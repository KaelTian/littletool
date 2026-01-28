using System.Net.Sockets;
using System.Text;

namespace NetworkFileTransfer
{
    /// <summary>
    /// 文件传输服务器
    /// </summary>
    public class FileTransferServer
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private SemaphoreSlim? _concurrencyLimiter; // 并发限流器

        // 属性配置
        public string SaveDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public int MaxConcurrentConnections { get; set; } = 10; // 最大并发连接数
        private const int BufferSize = 8192;
        private const int HeaderSize = 1024;
        public event EventHandler<TransferEventArgs>? ProgressChanged;
        public event EventHandler<TransferEventArgs>? StatusChanged;
        public event EventHandler<TransferEventArgs>? ClientConnected;
        public event EventHandler<TransferEventArgs>? TransferStarted;

        
        public async Task StartListening(int port)
        {
            _cts = new CancellationTokenSource();
            _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentConnections);

            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _listener.Start();

            OnStatusChanged($"服务器启动，监听端口 {port}，最大并发: {MaxConcurrentConnections}");

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    // 等待有空余槽位（限流）
                    await _concurrencyLimiter.WaitAsync(_cts.Token);

                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);

                    // Fire-and-forget 处理客户端,不阻塞主监听循环
                    _ = ProcessClientAsync(client);
                }
            }
            catch (OperationCanceledException)
            {
                OnStatusChanged("服务器正在关闭...");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"服务器错误: {ex.Message}", true);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _concurrencyLimiter?.Dispose();
            OnStatusChanged("服务器已停止");
        }

        private async Task ProcessClientAsync(TcpClient client)
        {
            // 确保无论成功失败都释放并发槽位
            try
            {
                await HandleClientAsync(client);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"客户端处理错误: {ex.Message}", true);
            }
            finally
            {
                client.Dispose();
                _concurrencyLimiter?.Release();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            // 使用局部变量,避免线程安全问题
            await using var stream = client.GetStream();
            var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知客户端";

            OnStatusChanged($"客户端已连接: {endpoint}");
            ClientConnected?.Invoke(this, new TransferEventArgs($"客户端 {endpoint} 已连接"));

            try
            {
                while (_cts != null && !_cts.Token.IsCancellationRequested && client.Connected)
                {
                    // 读取文件头 (使用局部 buffer)
                    var headerBuffer = new byte[HeaderSize];
                    var totalRead = 0;

                    while (totalRead < HeaderSize)
                    {
                        var read = await stream.ReadAsync(
                            headerBuffer.AsMemory(totalRead, HeaderSize - totalRead), _cts.Token);

                        if (read == 0)
                        {
                            // 连接关闭
                            OnStatusChanged($"客户端 {endpoint} 断开连接");
                            return;
                        }
                        totalRead += read;
                    }

                    var header = ParseFileHeader(headerBuffer);

                    if (header.Command == "SEND")
                    {
                        await ReceiveFileAsync(stream, header, endpoint);
                    }
                    else if (header.Command == "CANC")
                    {
                        OnStatusChanged($"客户端 {endpoint} 请求断开连接");
                        //await SendResponseAsync(stream, "BYE");
                    }
                    else
                    {
                        OnStatusChanged($"未知命令: {header.Command}", true);
                        //await SendResponseAsync(stream, "ERR");
                    }
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                OnStatusChanged($"客户端 {endpoint} 网络异常断开");
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
        }

        private async Task ReceiveFileAsync(
            NetworkStream stream,
            (string Command, string FileName, long FileSize, DateTime Timestamp) header,
            string endpoint)
        {
            try
            {
                // 构造保存路径 (自动处理重名)
                var filePath = GetUniqueFilePath(Path.Combine(SaveDirectory, header.FileName));

                OnStatusChanged($"[{endpoint}] 开始接收: {header.FileName} ({FormatBytes(header.FileSize)})");
                OnTransferStarted(header.FileName, header.FileSize);

                // 发送确认
                await SendResponseAsync(stream, "OK");

                // 接收文件内容
                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    FileOptions.Asynchronous);

                var buffer = new byte[BufferSize];
                long received = 0;

                while (received < header.FileSize)
                {
                    var toRead = (int)Math.Min(BufferSize, header.FileSize - received);
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, toRead), _cts!.Token);

                    if (bytesRead == 0)
                        throw new IOException("连接意外关闭");

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), _cts.Token);
                    received += bytesRead;
                    OnProgressChanged(received, header.FileSize, header.FileName);
                }

                OnStatusChanged($"文件接收完成: {Path.GetFileName(filePath)}");
                // 发送完成响应
                await SendResponseAsync(stream, "DONE");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"接收文件失败: {ex.Message}", true);
                await SendResponseAsync(stream, "ERR");
            }
        }
        /// <summary>
        /// 获取唯一的文件路径，避免覆盖已有文件
        /// </summary>
        /// <param name="basePath"></param>
        /// <returns></returns>
        private string GetUniqueFilePath(string basePath)
        {
            if (!File.Exists(basePath)) { return basePath; }

            var dir = Path.GetDirectoryName(basePath) ?? "";
            var name = Path.GetFileNameWithoutExtension(basePath);
            var ext = Path.GetExtension(basePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return Path.Combine(dir, $"{name}_{timestamp}{ext}");
        }

        private async Task SendResponseAsync(NetworkStream stream, string message)
        {
            var responseBytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }

        private (string Command, string FileName, long FileSize, DateTime Timestamp) ParseFileHeader(byte[] header)
        {
            var headerString = Encoding.UTF8.GetString(header).TrimEnd('\0');
            var parts = headerString.Split('|');

            if (parts.Length >= 4)
            {
                return (
                    Command: parts[0],
                    FileName: parts[1],
                    FileSize: long.TryParse(parts[2], out var size) ? size : 0,
                    Timestamp: DateTime.TryParse(parts[3], out var time) ? time : DateTime.Now
                );
            }
            else if (parts.Length == 1)
            {
                return (Command: parts[0], FileName: "", FileSize: 0, Timestamp: DateTime.Now);
            }
            return ("UNKNOWN", "", 0, DateTime.Now);
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

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
