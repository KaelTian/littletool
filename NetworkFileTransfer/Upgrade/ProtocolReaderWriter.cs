using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace NetworkFileTransfer.Upgrade
{
    // ProtocolReaderWriter.cs
    public class ProtocolReaderWriter : IAsyncDisposable
    {
        //private readonly NetworkStream _stream;
        private readonly PipeReader _reader; // 使用 PipeReader 自动处理粘包
        private readonly PipeWriter _writer;

        public ProtocolReaderWriter(NetworkStream stream)
        {
            //_stream = stream;
            _reader = PipeReader.Create(stream);
            _writer = PipeWriter.Create(stream);
        }

        /// <summary>
        /// 发送消息（自动打包）
        /// </summary>
        public async ValueTask WriteAsync(FileTransferProtocol.Message message, CancellationToken ct = default)
        {
            var payloadLen = message.Payload.Length;

            // 构建头部: [Magic:2][Type:1][Reserved:1][Length:4]
            var header = new byte[FileTransferProtocol.HeaderSize];
            BitConverter.GetBytes(FileTransferProtocol.Magic).CopyTo(header, 0);
            header[2] = (byte)message.Type;
            header[3] = 0; // Reserved
            BitConverter.GetBytes(payloadLen).CopyTo(header, 4);

            // 原子写入（避免分包）
            _writer.Write(header);
            _writer.Write(message.Payload);
            await _writer.FlushAsync(ct);
        }

        /// <summary>
        /// 读取完整消息（自动处理粘包/拆包）
        /// </summary>
        public async ValueTask<FileTransferProtocol.Message?> ReadAsync(CancellationToken ct = default)
        {
            while (true)
            {
                var result = await _reader.ReadAsync(ct);
                var buffer = result.Buffer;

                if (TryParseMessage(ref buffer, out var message))
                {
                    _reader.AdvanceTo(buffer.Start); // 消费已解析的数据
                    return message;
                }

                if (result.IsCompleted)
                    return null; // 连接关闭

                // 数据不足，等待更多
                _reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }

        private bool TryParseMessage(ref ReadOnlySequence<byte> buffer,
            [NotNullWhen(true)] out FileTransferProtocol.Message? message)
        {
            message = null;
            var reader = new SequenceReader<byte>(buffer);

            // 至少要有头部
            if (buffer.Length < FileTransferProtocol.HeaderSize)
                return false;

            // 读取魔数
            if (!reader.TryReadLittleEndian(out short magic) || (ushort)magic != FileTransferProtocol.Magic)
                throw new ProtocolException($"Invalid magic number: 0x{magic:X4}");

            // 读取类型
            if (!reader.TryRead(out byte typeByte))
                return false;
            var type = (FileTransferProtocol.MessageType)typeByte;

            // 跳过保留字节
            reader.TryRead(out _);

            // 读取长度
            if (!reader.TryReadLittleEndian(out int payloadLen))
                return false;

            // 检查是否有完整载荷
            if (buffer.Length < FileTransferProtocol.HeaderSize + payloadLen)
                return false;

            // 提取载荷
            var payload = buffer.Slice(FileTransferProtocol.HeaderSize, payloadLen).ToArray();
            buffer = buffer.Slice(FileTransferProtocol.HeaderSize + payloadLen);

            message = new FileTransferProtocol.Message(type, payload);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            await _reader.CompleteAsync();
            await _writer.CompleteAsync();
        }
    }

    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message) { }
    }
}
