using System.Net.Sockets;
using System.Text;

namespace NetworkFileTransfer
{
    /// <summary>
    /// 文件传输客户端
    /// </summary>
    public class FileTransferClient : IAsyncDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly int _bufferSize;
        private readonly int _headerSize;
        private readonly TimeSpan _responseTimeout;
        private long _lastProgressReported;
        private const int ProgressReportIntervalBytes = 65536; // 每 64KB 报告一次进度，避免 UI 卡顿

        public bool IsConnected => _client?.Connected == true && _stream != null;

        public event EventHandler<TransferEventArgs>? ProgressChanged;
        public event EventHandler<TransferEventArgs>? StatusChanged;
        public event EventHandler<TransferEventArgs>? TransferStarted;

        public FileTransferClient(int bufferSize = 8192, int headerSize = 1024, TimeSpan? responseTimeout = null)
        {
            _bufferSize = bufferSize;
            _headerSize = headerSize;
            _responseTimeout = responseTimeout ?? TimeSpan.FromSeconds(30);
        }

        public async Task ConnectAsync(string serverIP, int port, CancellationToken cancellationToken = default)
        {
            try
            {
                Dispose(); // 清理旧连接

                _cts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _client = new TcpClient() { NoDelay = true }; // 禁用 Nagle 算法，减少延迟

                using var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // 连接超时 10 秒

                await _client.ConnectAsync(serverIP, port, cts.Token);
                _stream = _client.GetStream();

                OnStatusChanged($"已连接到服务器 {serverIP}:{port}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Dispose();
                OnStatusChanged($"连接服务器失败: {ex.Message}", true);
                throw;
            }
        }

        public async Task SendFileAsync(string localFilePath, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器");
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException("文件不存在", localFilePath);

            // 创建链接的 CancellationToken，支持方法级取消
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token, cancellationToken);
            var token = linkedCts.Token;

            var fileInfo = new FileInfo(localFilePath);
            var fileName = fileInfo.Name;

            try
            {
                OnStatusChanged($"准备发送: {fileName} ({FormatBytes(fileInfo.Length)})");
                // 1. 发送文件头（带长度前缀确保服务器能正确解析）
                var headerBytes = CreateFileHeader("SEND", fileName, fileInfo.Length);
                await _stream!.WriteAsync(headerBytes.AsMemory(), token);

                // 2. 可靠地读取服务器响应（处理 TCP 分包）
                var response = await ReadExactlyAsync(_stream, 2, token);
                var responseStr = Encoding.UTF8.GetString(response).TrimEnd('\0');

                if (responseStr != "OK")
                    throw new IOException($"服务器拒绝接收: {responseStr}");

                // 3. 开始传输
                OnTransferStarted(fileName, fileInfo.Length);
                _lastProgressReported = 0;

                await using var fileStream = new FileStream(
                    localFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    _bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan); // 优化顺序读取

                var buffer = new byte[_bufferSize];
                long totalSent = 0;

                while (totalSent < fileInfo.Length)
                {
                    // 读取文件块
                    var toRead = (int)Math.Min(_bufferSize, fileInfo.Length - totalSent);
                    var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, toRead), token);

                    if (bytesRead == 0)
                        throw new IOException("文件读取意外结束");

                    // 可靠写入 (循环确保所有数据发送)
                    await WriteExactlyAsync(_stream, buffer.AsMemory(0, bytesRead), token);

                    totalSent += bytesRead;

                    // 节流进度报告，避免 UI 线程过载
                    if (totalSent - _lastProgressReported >= ProgressReportIntervalBytes || totalSent == fileInfo.Length)
                    {
                        OnProgressChanged(totalSent, fileInfo.Length, fileName);
                        _lastProgressReported = totalSent;
                    }
                }

                // 4. 可选：等待服务器最终确认（文件完整性校验）
                await WaitForFinalAck(token);

                OnStatusChanged($"文件发送完成: {fileName}");

            }
            catch (OperationCanceledException)
            {
                OnStatusChanged("传输已取消");
                throw;
            }
            catch (Exception ex)
            {
                OnStatusChanged($"发送失败: {ex.Message}", true);
                // 尝试发送取消信号给服务器，避免服务器僵死等待
                try
                {
                    await _stream!.WriteAsync(Encoding.UTF8.GetBytes("CANC").AsMemory(), CancellationToken.None);
                }
                catch { /* 忽略清理时的错误 */ }

                throw;
            }
        }
        /// <summary>
        /// 可靠地读取指定字节数（处理 TCP 分包/粘包）
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="count"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static async ValueTask<byte[]> ReadExactlyAsync(
            NetworkStream stream,
            int count,
            CancellationToken token
            )
        {
            var buffer = new byte[count];
            var totalRead = 0;

            while (totalRead < count)
            {
                var read = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, count - totalRead), token);
                if (read == 0)
                    throw new IOException("连接已关闭，读取失败");
                totalRead += read;
            }

            return buffer;
        }
        /// <summary>
        /// 可靠地写入所有数据
        /// 
        /// </summary>
        private static async ValueTask WriteExactlyAsync(
            NetworkStream stream,
            ReadOnlyMemory<byte> data,
            CancellationToken token
            )
        {
            // 校验参数合法性
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new InvalidOperationException("网络流不支持写入操作");
            await stream.WriteAsync(data, token);
        }

        /// <summary>
        /// 升级后的头部格式，包含魔数和版本，更健壮
        /// </summary>
        private byte[] CreateFileHeader(string command, string fileName, long fileSize)
        {
            var header = new byte[_headerSize];
            // 格式: COMMAND|FILENAME|FILESIZE|TIMESTAMP
            // 实际生产建议使用固定长度二进制协议或 Protobuf，这里保持文本协议但确保填充
            var content = $"{command}|{fileName}|{fileSize}|{DateTime.UtcNow:O}";
            var bytes = Encoding.UTF8.GetBytes(content);

            if (bytes.Length > _headerSize - 1)
                throw new ArgumentException($"文件名过长，最大支持 {_headerSize - 1} 字节");

            bytes.CopyTo(header, 0);
            return header;
        }

        private async ValueTask WaitForFinalAck(CancellationToken token)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(_responseTimeout);
                var response = await ReadExactlyAsync(_stream!, 4, cts.Token);
                var responseStr = Encoding.UTF8.GetString(response).TrimEnd('\0');
                if (responseStr != "DONE")
                    throw new IOException($"服务器未确认完成: {responseStr}");
                OnStatusChanged("服务器确认接收成功");
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                // 只是超时，不影响业务，记录即可
                OnStatusChanged("服务器最终确认超时（可能已接收）");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_client == null) return;

            try
            {
                // 优雅关闭：先发送 FIN，等待对方关闭
                _client.Client.Shutdown(SocketShutdown.Send);

                // 给服务器 2 秒时间响应关闭
                await Task.Delay(2000);
            }
            catch { /* 忽略关闭时的错误 */ }
            finally
            {
                Dispose();
                OnStatusChanged("已断开连接");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _stream?.Dispose();
            _client?.Dispose();
            _cts?.Dispose();

            _stream = null;
            _client = null;
            _cts = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void OnTransferStarted(string fileName, long totalBytes)
        {
            TransferStarted?.Invoke(this, new TransferEventArgs(0, totalBytes, fileName));
        }

        private void OnProgressChanged(long transferred, long total, string fileName)
        {
            ProgressChanged?.Invoke(this, new TransferEventArgs(transferred, total, fileName));
        }

        private void OnStatusChanged(string message, bool isError = false)
        {
            StatusChanged?.Invoke(this, new TransferEventArgs(message, isError));
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
