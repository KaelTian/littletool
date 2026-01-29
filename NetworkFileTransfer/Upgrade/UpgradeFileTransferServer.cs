using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace NetworkFileTransfer.Upgrade
{
    public class UpgradeFileTransferServer : IDisposable
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private SemaphoreSlim? _limiter;

        public string SaveDirectory { get; set; } = Path.GetTempPath();
        public int MaxConcurrent { get; set; } = 10;
        public int Port { get; set; } = 9000;

        // 事件
        public event EventHandler<TransferEvent>? ClientConnected;
        public event EventHandler<TransferEvent>? TransferStarted;
        public event EventHandler<TransferProgressEvent>? ProgressChanged;
        public event EventHandler<TransferEvent>? TransferCompleted;
        public event EventHandler<TransferErrorEvent>? ErrorOccurred;

        public async Task StartAsync(CancellationToken ct = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _limiter = new SemaphoreSlim(MaxConcurrent);
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();

            OnStatusChanged($"Server started on port {Port}");

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await _limiter.WaitAsync(_cts.Token);
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);

                    _ = HandleClientAsync(client, _cts.Token).ContinueWith(t =>
                    {
                        _limiter.Release();
                        client.Dispose();
                    }, TaskScheduler.Default);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            await using var protocol = new ProtocolReaderWriter(client.GetStream());
            try
            {
                // 1. 等待 Handshake
                var handshake = await protocol.ReadAsync(ct);
                if (handshake?.Type != FileTransferProtocol.MessageType.Handshake)
                    throw new ProtocolException("Expected handshake");

                // 发送确认，进入就绪状态
                await protocol.WriteAsync(FileTransferProtocol.CreateAck(true, "Ready"), ct);
                OnClientConnected(endpoint);

                // 2. 主循环：等待文件传输请求
                while (!ct.IsCancellationRequested)
                {
                    var msg = await protocol.ReadAsync(ct);
                    if (msg == null) break; // 客户端断开

                    switch (msg.Type)
                    {
                        case FileTransferProtocol.MessageType.FileHeader:
                            await HandleFileTransfer(protocol, msg.Payload, endpoint, ct);
                            break;

                        case FileTransferProtocol.MessageType.Cancel:
                            OnStatusChanged($"Client {endpoint} cancelled");
                            return;

                        default:
                            await protocol.WriteAsync(
                                FileTransferProtocol.CreateError($"Unexpected message: {msg.Type}"), ct);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(endpoint, ex.Message);
            }
        }

        private async Task HandleFileTransfer(ProtocolReaderWriter protocol, byte[] headerPayload,
            string endpoint, CancellationToken ct)
        {
            // 解析文件头
            var header = JsonSerializer.Deserialize<FileHeader>(headerPayload)!;
            var filePath = GetUniquePath(Path.Combine(SaveDirectory, header.FileName));

            OnTransferStarted(endpoint, header.FileName, header.FileSize);

            // 确认开始接收
            await protocol.WriteAsync(FileTransferProtocol.CreateAck(true, "Receiving"), ct);

            // 接收文件数据
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                FileShare.None, 81920, FileOptions.Asynchronous);

            long received = 0;
            var buffer = new List<byte>(); // 用于收集分片

            while (received < header.FileSize)
            {
                var chunk = await protocol.ReadAsync(ct);
                if (chunk?.Type != FileTransferProtocol.MessageType.FileData)
                    throw new ProtocolException("Expected file data chunk");

                var dataMsg = JsonSerializer.Deserialize<FileDataMessage>(chunk.Payload)!;
                var bytes = Convert.FromBase64String(dataMsg.Data);

                await fileStream.WriteAsync(bytes, ct);
                received += bytes.Length;

                OnProgressChanged(endpoint, header.FileName, received, header.FileSize);

                // 如果是最后一个分片，发送确认
                if (dataMsg.IsLast || received >= header.FileSize)
                {
                    await protocol.WriteAsync(
                        FileTransferProtocol.CreateComplete(header.FileName, received, filePath), ct);
                }
            }

            OnTransferCompleted(endpoint, header.FileName, filePath);
        }

        private string GetUniquePath(string path)
        {
            if (!File.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            return Path.Combine(dir!, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
        }

        // 事件触发方法...
        private void OnClientConnected(string endpoint) =>
            ClientConnected?.Invoke(this, new TransferEvent(endpoint));
        private void OnTransferStarted(string endpoint, string file, long size) =>
            TransferStarted?.Invoke(this, new TransferEvent(endpoint, file, size));
        private void OnProgressChanged(string endpoint, string file, long current, long total) =>
            ProgressChanged?.Invoke(this, new TransferProgressEvent(endpoint, file, current, total));
        private void OnTransferCompleted(string endpoint, string file, string path) =>
            TransferCompleted?.Invoke(this, new TransferEvent(endpoint, file, 0, path));
        private void OnError(string endpoint, string error) =>
            ErrorOccurred?.Invoke(this, new TransferErrorEvent(endpoint, error));
        private void OnStatusChanged(string msg) =>
            Console.WriteLine($"[Server] {msg}");

        public void Dispose()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _limiter?.Dispose();
        }
    }

    // DTOs
    public record FileHeader(string FileName, long FileSize, string? Checksum);
    public record FileDataMessage(long Offset, bool IsLast, string Data);
}
