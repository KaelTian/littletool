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
                    // 解析成功：此时buffer已被TryParseMessage修改，buffer.Start指向已解析消息的下一个位置
                    // 释放buffer.Start之前的所有数据（即已解析的完整消息），后续无需再处理
                    _reader.AdvanceTo(buffer.Start);
                    return message;
                }

                if (result.IsCompleted)
                    return null; // 连接已关闭，无更多数据可读取，返回null

                // 解析失败（数据不足）：此时buffer未被修改，buffer.Start仍为原始起始位置
                // 保留buffer.Start到buffer.End的所有数据，下一次ReadAsync会将新数据追加到该数据尾部，拼接后再解析
                _reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }

        private bool TryParseMessage(ref ReadOnlySequence<byte> buffer,
            [NotNullWhen(true)] out FileTransferProtocol.Message? message)
        {
            message = null;
            var reader = new SequenceReader<byte>(buffer);

            // 1. 先校验：当前缓冲区数据长度是否≥固定头部长度，不足则无法解析，直接返回false
            if (buffer.Length < FileTransferProtocol.HeaderSize)
                return false;

            // 2. 读取并校验魔数（小端序），魔数错误直接抛出协议异常
            if (!reader.TryReadLittleEndian(out short magic) || (ushort)magic != FileTransferProtocol.Magic)
                throw new ProtocolException($"Invalid magic number: 0x{magic:X4}");

            // 3. 读取消息类型字节，并转换为定义的消息类型枚举
            if (!reader.TryRead(out byte typeByte))
                return false;
            var type = (FileTransferProtocol.MessageType)typeByte;

            // 4. 跳过保留字节（预留字段，当前无业务意义，仅占位满足协议格式）
            reader.TryRead(out _);

            // 5. 读取载荷长度（小端序），后续需要用该长度校验是否有完整载荷数据
            if (!reader.TryReadLittleEndian(out int payloadLen))
                return false;

            // 6. 校验：当前缓冲区数据长度是否≥「头部长度+载荷长度」，不足则无法解析完整消息，返回false
            if (buffer.Length < FileTransferProtocol.HeaderSize + payloadLen)
                return false;

            // ==========================================================================
            // 以下为「解析成功」的后续操作，执行到这里说明数据足够，能组成完整消息
            // ==========================================================================

            // 7. 提取载荷数据：从头部结束位置开始，截取指定载荷长度的字节数组
            //    buffer.Slice(起始偏移量, 截取长度)：这里起始偏移量是头部长度，跳过头部直接取载荷
            var payload = buffer.Slice(FileTransferProtocol.HeaderSize, payloadLen).ToArray();

            // 8. 关键：更新引用传递的buffer，跳过当前已解析的「完整消息」（头部+载荷）
            //    执行后，buffer.Start 会向后移动到「当前完整消息的下一个字节位置」（指向剩余未解析数据）
            //    若当前缓冲区只有这一条消息，Slice后buffer会变为空，buffer.Start指向缓冲区末尾
            buffer = buffer.Slice(FileTransferProtocol.HeaderSize + payloadLen);

            // 9. 封装完整消息对象，返回true标识解析成功
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
