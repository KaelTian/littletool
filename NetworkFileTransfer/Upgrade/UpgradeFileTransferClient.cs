using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NetworkFileTransfer.Upgrade
{
    public class UpgradeFileTransferClient : IAsyncDisposable
    {
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

        public async Task SendFileAsync(string filePath, CancellationToken ct = default)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected");

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token, ct);
            var token = linkedCts.Token;

            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;

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

                // 3. 分片发送文件数据
                await using var stream = File.OpenRead(filePath);
                const int chunkSize = 65536; // 64KB 分片
                var buffer = new byte[chunkSize];
                long totalSent = 0;
                long offset = 0;

                while (totalSent < fileInfo.Length)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), token);
                    if (read == 0) break;

                    var isLast = totalSent + read >= fileInfo.Length;
                    await _protocol.WriteAsync(
                        FileTransferProtocol.CreateFileData(buffer[..read], offset, isLast), token);

                    totalSent += read;
                    offset += read;
                    OnProgressChanged(fileName, totalSent, fileInfo.Length);

                    // 流控：每发送 1MB 等待一次确认（可选，防止服务器缓冲区溢出）
                    if (!isLast && totalSent % (1024 * 1024) == 0)
                    {
                        // 简单延迟，或实现窗口确认
                        await Task.Delay(1, token);
                    }
                }

                // 4. 等待完成确认
                var complete = await _protocol.ReadAsync(token);
                if (complete?.Type == FileTransferProtocol.MessageType.Complete)
                {
                    // 解析完成结果
                    var jsonString = Encoding.UTF8.GetString(complete.Payload);
                    var result = JsonSerializer.Deserialize<TransferComplete>(jsonString)!;
                    OnTransferCompleted(result.FileName, result.StoredPath);
                }
            }
            catch (Exception ex)
            {
                // 发送取消信号
                try { await _protocol!.WriteAsync(FileTransferProtocol.CreateError(ex.Message), default); }
                catch { }
                OnError(fileName, ex.Message);
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
        private void OnTransferCompleted(string file, string? path) =>
            TransferCompleted?.Invoke(this, new TransferEvent("", file, 0, path));
        private void OnError(string file, string error) =>
            ErrorOccurred?.Invoke(this, new TransferErrorEvent(file, error));
    }
}
