using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            var jsonString = Encoding.UTF8.GetString(headerPayload);
            var header = JsonSerializer.Deserialize<FileHeader>(jsonString)!;
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

                // 1. 调用辅助方法解析 FileData 二进制 Payload（无需 JSON/Base64）
                if (!FileTransferProtocol.TryParseFileDataPayload(chunk.Payload, out long offset, out bool isLast, out byte[] fileData))
                {
                    OnError(endpoint, "解析 FileData 失败：无效的二进制 Payload");
                    // 可发送 Error 消息给客户端
                    await protocol.WriteAsync(FileTransferProtocol.CreateError("无效的文件分片数据"), ct);
                    return;
                }

                // 2. 直接写入文件流（无额外转换，效率拉满）
                // 可选：验证 offset 是否正确（避免分片乱序）
                if (offset != fileStream.Position)
                {
                    OnError(endpoint, $"警告：分片偏移量不匹配，预期 {fileStream.Position}，实际 {offset}");
                    // 如需严格保证顺序，可在这里缓存乱序分片，后续重新排序
                }

                await fileStream.WriteAsync(fileData, 0, fileData.Length, ct);
                received += fileData.Length;

                OnProgressChanged(endpoint, header.FileName, received, header.FileSize);

                // 3. 处理最后一片分片
                if (isLast || received >= header.FileSize)
                {
                    await fileStream.FlushAsync(ct);
                    // 后续：校验文件哈希、发送 Complete 消息给客户端等逻辑
                    await protocol.WriteAsync(FileTransferProtocol.CreateComplete(
                        Path.GetFileName(fileStream.Name), fileStream.Length, filePath), ct);
                    OnTransferCompleted(endpoint, header.FileName, filePath, fileStream.Length);
                }
            }
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
        private void OnTransferCompleted(string endpoint, string file, string path, long total) =>
            TransferCompleted?.Invoke(this, new TransferEvent(endpoint, file, total, path));
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
    public record FileHeader(
       [property: JsonPropertyName("fileName")] string FileName,
       [property: JsonPropertyName("fileSize")] long FileSize,
       [property: JsonPropertyName("checksum")] string? Checksum
        );
}
