using System.Text.Json;

namespace NetworkFileTransfer.Upgrade
{
    /// <summary>
    /// Protocol.cs - Client 和 Server 共享
    /// </summary>
    public static class FileTransferProtocol
    {
        public const ushort Magic = 0xA55A; // 魔数，用于快速识别有效包
        public const int HeaderSize = 8;
        // 新增：FileData 消息的 Payload 二进制头部大小（固定结构，方便解析）
        // offset(long:8字节) + isLast(bool:1字节) + 预留(3字节，对齐到12字节，提升解析效率)
        public const int FileDataPayloadHeaderSize = 8 + 1 + 3;
        public enum MessageType : byte
        {
            // 控制消息
            Handshake = 0x01,      // C→S: 文件传输请求
            Ack = 0x02,            // S→C: 确认/拒绝
            Cancel = 0x03,         // C→S: 客户端取消
            Complete = 0x04,       // S→C: 传输完成确认

            // 数据消息
            FileHeader = 0x10,     // C→S: 文件元数据（文件名、大小、哈希）
            FileData = 0x11,       // C→S: 文件数据分片
            FileChecksum = 0x12,   // C→S: 文件校验和

            // 错误
            Error = 0xFF           // 双向: 错误报告
        }

        // 消息结构
        public record Message(MessageType Type, byte[] Payload);

        // 便捷创建方法
        public static Message CreateHandshake(string clientId) =>
            CreateJsonMessage(MessageType.Handshake, new { clientId, version = "1.0" });

        public static Message CreateFileHeader(string fileName, long fileSize, string? checksum = null) =>
            CreateJsonMessage(MessageType.FileHeader, new { fileName, fileSize, checksum });

        public static Message CreateAck(bool accepted, string? reason = null) =>
            CreateJsonMessage(MessageType.Ack, new { accepted, reason });

        // ************************ 核心修改：重构 CreateFileData ************************
        // 不再使用 JSON + Base64，直接构建二进制 Payload
        public static Message CreateFileData(byte[] data, long offset, bool isLast)
        {
            // 1. 计算 Payload 总长度：二进制头部 + 原始文件数据长度
            int totalPayloadLength = FileDataPayloadHeaderSize + data.Length;
            byte[] payload = new byte[totalPayloadLength];

            // 2. 写入二进制头部（按固定结构填充）
            // 写入 offset（long，8字节，小端序，和协议整体保持一致）
            BitConverter.GetBytes(offset).CopyTo(payload, 0);
            // 写入 isLast（bool，1字节，第8位）
            payload[8] = isLast ? (byte)1 : (byte)0;
            // 写入预留字节（3字节，第9-11位，填充0，用于对齐和后续扩展）
            // payload[9] = 0;
            // payload[10] = 0;
            // payload[11] = 0;（数组初始化时默认是0，无需额外赋值）

            // 3. 写入原始文件数据（跳过二进制头部，直接拷贝）
            Buffer.BlockCopy(data, 0, payload, FileDataPayloadHeaderSize, data.Length);

            // 4. 创建 FileData 消息并返回
            return new Message(MessageType.FileData, payload);
        }
        // *****************************************************************************

        public static Message CreateComplete(string fileName, long bytesReceived, string? storedPath = null) =>
            CreateJsonMessage(MessageType.Complete, new { fileName, bytesReceived, storedPath });

        public static Message CreateError(string error) =>
            CreateJsonMessage(MessageType.Error, new { error });

        private static Message CreateJsonMessage(MessageType type, object obj) =>
            new(type, JsonSerializer.SerializeToUtf8Bytes(obj));

        // ************************ 新增：FileData 消息的解析辅助方法（供接收端使用） ************************
        // 接收端专用：解析 FileData 类型的 Payload，提取分片信息和原始文件数据
        public static bool TryParseFileDataPayload(byte[] fileDataPayload, out long offset, out bool isLast, out byte[] fileData)
        {
            // 初始化返回值
            offset = 0;
            isLast = false;
            fileData = Array.Empty<byte>();

            // 1. 校验 Payload 长度是否合法（至少要包含二进制头部）
            if (fileDataPayload.Length < FileDataPayloadHeaderSize)
            {
                return false;
            }

            // 2. 解析二进制头部
            // 解析 offset（long，8字节）
            offset = BitConverter.ToInt64(fileDataPayload, 0);
            // 解析 isLast（bool，1字节）
            isLast = fileDataPayload[8] == 1;

            // 3. 提取原始文件数据（跳过二进制头部）
            int fileDataLength = fileDataPayload.Length - FileDataPayloadHeaderSize;
            fileData = new byte[fileDataLength];
            Buffer.BlockCopy(fileDataPayload, FileDataPayloadHeaderSize, fileData, 0, fileDataLength);

            return true;
        }
    }
}
