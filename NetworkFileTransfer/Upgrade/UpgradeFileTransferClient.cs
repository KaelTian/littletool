using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NetworkFileTransfer.Upgrade
{
    public class UpgradeFileTransferClient : IAsyncDisposable
    {
        /// <summary>
        /// 文件传输状态（用于断点续传/暂停恢复）
        /// </summary>
        private class FileTransferState
        {
            /// <summary>
            /// 源文件路径
            /// </summary>
            public string FilePath { get; set; } = string.Empty;

            /// <summary>
            /// 文件名
            /// </summary>
            public string FileName { get; set; } = string.Empty;

            /// <summary>
            /// 文件总大小
            /// </summary>
            public long TotalFileSize { get; set; }

            /// <summary>
            /// 已发送字节数（当前offset）
            /// </summary>
            public long TotalSent { get; set; }

            /// <summary>
            /// 文件校验和
            /// </summary>
            public string Checksum { get; set; } = string.Empty;

            /// <summary>
            /// 文件读取流（需保持存活，不随方法释放）
            /// </summary>
            public FileStream? FileStream { get; set; }

            /// <summary>
            /// 是否处于暂停状态
            /// </summary>
            public bool IsPaused { get; set; }

            /// <summary>
            /// 清理资源
            /// </summary>
            public void Dispose()
            {
                FileStream?.Dispose();
            }
        }

        // 客户端类内定义共享字段，保存当前正在传输的状态（仅支持单文件传输，多文件可改为字典）
        private FileTransferState? _currentTransferState;

        private TcpClient? _client;
        private ProtocolReaderWriter? _protocol;
        private CancellationTokenSource? _cts;

        public bool IsConnected => _client?.Connected ?? false;

        public event EventHandler<TransferEvent>? Connected;
        public event EventHandler<TransferEvent>? TransferStarted;
        public event EventHandler<TransferProgressEvent>? ProgressChanged;
        public event EventHandler<TransferEvent>? TransferCompleted;
        public event EventHandler<TransferErrorEvent>? ErrorOccurred;

        public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, ct);
            _protocol = new ProtocolReaderWriter(_client.GetStream());
            _cts = new CancellationTokenSource();

            // 1. 发送握手
            await _protocol.WriteAsync(FileTransferProtocol.CreateHandshake(Guid.NewGuid().ToString()), ct);

            // 2. 等待服务器就绪
            var ack = await _protocol.ReadAsync(ct);
            if (ack?.Type != FileTransferProtocol.MessageType.Ack)
                throw new ProtocolException("Handshake failed");

            OnConnected();
        }

        /// <summary>
        /// 暂停当前文件传输（同一次连接内优雅暂停）
        /// </summary>
        public Task PauseTransferAsync()
        {
            if (_currentTransferState == null)
                throw new InvalidOperationException("No ongoing file transfer to pause");

            _currentTransferState.IsPaused = true;
            OnProgressChanged(_currentTransferState.FileName, _currentTransferState.TotalSent, _currentTransferState.TotalFileSize);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 恢复当前文件传输（从暂停的offset继续发送）
        /// </summary>
        public async Task ResumeTransferAsync(CancellationToken ct = default)
        {
            if (_currentTransferState == null)
                throw new InvalidOperationException("No paused file transfer to resume");

            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            if (!_currentTransferState.IsPaused)
                return; // 未暂停，无需恢复

            // 重置暂停标志位
            _currentTransferState.IsPaused = false;
            var state = _currentTransferState;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token, ct);
            var token = linkedCts.Token;

            try
            {
                const int chunkSize = 65536; // 64KB 分片
                var buffer = new byte[chunkSize];

                // 从当前已发送的offset继续读取和发送
                while (state.TotalSent < state.TotalFileSize && !state.IsPaused)
                {
                    // 响应取消令牌
                    token.ThrowIfCancellationRequested();

                    // 从文件流当前位置（已自动定位到offset）读取数据
                    var read = await state.FileStream!.ReadAsync(buffer.AsMemory(0, chunkSize), token);
                    if (read == 0) break;

                    var isLast = state.TotalSent + read >= state.TotalFileSize;
                    await _protocol!.WriteAsync(
                        FileTransferProtocol.CreateFileData(buffer[..read], state.TotalSent, isLast), token);

                    // 更新状态
                    state.TotalSent += read;
                    OnProgressChanged(state.FileName, state.TotalSent, state.TotalFileSize);

                    // 流控：每发送1MB等待一次（可选）
                    if (!isLast && state.TotalSent % (1024 * 1024) == 0)
                    {
                        await Task.Delay(1, token);
                    }
                }

                // 只有传输完成（未暂停、已发送全部数据），才等待服务端完成确认
                if (!state.IsPaused && state.TotalSent >= state.TotalFileSize)
                {
                    var complete = await _protocol!.ReadAsync(token);
                    if (complete?.Type == FileTransferProtocol.MessageType.Complete)
                    {
                        var jsonString = Encoding.UTF8.GetString(complete.Payload);
                        var result = JsonSerializer.Deserialize<TransferComplete>(jsonString)!;
                        OnTransferCompleted(result.FileName, result.StoredPath, result.BytesReceived);

                        // 传输完成，清理当前状态
                        state.Dispose();
                        _currentTransferState = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // 发送取消信号
                try { await _protocol!.WriteAsync(FileTransferProtocol.CreateError(ex.Message), default); }
                catch { }

                OnError(state.FileName, ex.Message);
                // 清理异常状态
                state.Dispose();
                _currentTransferState = null;
                throw;
            }
        }


        public async Task SendFileAsync(string filePath, CancellationToken ct = default)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected");
            if (_currentTransferState != null)
                throw new InvalidOperationException("Another file transfer is already in progress");

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token, ct);
            var token = linkedCts.Token;

            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            var transferState = new FileTransferState
            {
                FilePath = filePath,
                FileName = fileName,
                TotalFileSize = fileInfo.Length,
                IsPaused = false
            };

            // 初始化当前传输状态（共享字段，用于Pause/Resume）
            _currentTransferState = transferState;


            try
            {
                // 1. 发送文件头
                var checksum = await CalculateChecksumAsync(filePath, token);
                await _protocol!.WriteAsync(
                    FileTransferProtocol.CreateFileHeader(fileName, fileInfo.Length, checksum), token);

                // 2. 等待服务器确认
                var ack = await _protocol.ReadAsync(token);
                if (ack?.Type != FileTransferProtocol.MessageType.Ack)
                    throw new IOException("Server rejected file");

                OnTransferStarted(fileName, fileInfo.Length);

                // 3. 打开文件流（不使用using，避免方法内释放，暂停时保持流存活）
                transferState.FileStream = File.OpenRead(filePath);
                const int chunkSize = 65536; // 64KB 分片
                var buffer = new byte[chunkSize];

                // 4. 分片发送文件数据（支持暂停判断）
                while (transferState.TotalSent < transferState.TotalFileSize && !transferState.IsPaused)
                {
                    // 响应取消令牌
                    token.ThrowIfCancellationRequested();

                    // 从文件流当前位置读取数据（首次是0，恢复时是已发送的offset）
                    var read = await transferState.FileStream.ReadAsync(buffer.AsMemory(0, chunkSize), token);
                    if (read == 0) break;

                    var isLast = transferState.TotalSent + read >= transferState.TotalFileSize;
                    await _protocol.WriteAsync(
                        FileTransferProtocol.CreateFileData(buffer[..read], transferState.TotalSent, isLast), token);

                    // 更新传输状态
                    transferState.TotalSent += read;
                    OnProgressChanged(fileName, transferState.TotalSent, fileInfo.Length);

                    // 流控：每发送 1MB 等待一次确认（可选，防止服务器缓冲区溢出）
                    if (!isLast && transferState.TotalSent % (1024 * 1024) == 0)
                    {
                        // 简单延迟，或实现窗口确认
                        await Task.Delay(1, token);
                    }
                }

                // 5. 只有传输完成（未暂停、已发送全部数据），才等待服务端完成确认
                if (!transferState.IsPaused && transferState.TotalSent >= transferState.TotalFileSize)
                {
                    var complete = await _protocol.ReadAsync(token);
                    if (complete?.Type == FileTransferProtocol.MessageType.Complete)
                    {
                        var jsonString = Encoding.UTF8.GetString(complete.Payload);
                        var result = JsonSerializer.Deserialize<TransferComplete>(jsonString)!;
                        OnTransferCompleted(result.FileName, result.StoredPath, result.BytesReceived);

                        // 传输完成，清理资源
                        transferState.Dispose();
                        _currentTransferState = null;
                    }
                    else if (complete?.Type == FileTransferProtocol.MessageType.Error)
                    {
                        var errorMsg = Encoding.UTF8.GetString(complete.Payload);
                        OnError(fileName, $"Server reported error: {errorMsg}");
                        // 清理异常状态
                        transferState.Dispose();
                        _currentTransferState = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // 发送取消信号
                try { await _protocol!.WriteAsync(FileTransferProtocol.CreateError(ex.Message), default); }
                catch { }

                OnError(fileName, ex.Message);
                // 清理异常状态
                transferState.Dispose();
                _currentTransferState = null;
                throw;
            }
        }

        private async Task<string> CalculateChecksumAsync(string path, CancellationToken ct)
        {
            await using var stream = File.OpenRead(path);
            var hash = await SHA256.HashDataAsync(stream, ct);
            return Convert.ToHexString(hash);
        }

        public async ValueTask DisposeAsync()
        {
            _cts?.Cancel();
            await _protocol!.DisposeAsync();
            _client?.Dispose();
        }

        // 事件触发...
        private void OnConnected() => Connected?.Invoke(this, new TransferEvent("Connected"));
        private void OnTransferStarted(string file, long size) =>
            TransferStarted?.Invoke(this, new TransferEvent("", file, size));
        private void OnProgressChanged(string file, long current, long total) =>
            ProgressChanged?.Invoke(this, new TransferProgressEvent("", file, current, total));
        private void OnTransferCompleted(string file, string? path, long total) =>
            TransferCompleted?.Invoke(this, new TransferEvent("", file, total, path));
        private void OnError(string file, string error) =>
            ErrorOccurred?.Invoke(this, new TransferErrorEvent(file, error));
    }
}
