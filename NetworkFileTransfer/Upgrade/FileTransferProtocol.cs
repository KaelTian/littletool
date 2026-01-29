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

        public static Message CreateFileData(byte[] data, long offset, bool isLast) =>
            CreateJsonMessage(MessageType.FileData, new { offset, isLast, data = Convert.ToBase64String(data) });

        public static Message CreateComplete(string fileName, long bytesReceived, string? storedPath = null) =>
            CreateJsonMessage(MessageType.Complete, new { fileName, bytesReceived, storedPath });

        public static Message CreateError(string error) =>
            CreateJsonMessage(MessageType.Error, new { error });

        private static Message CreateJsonMessage(MessageType type, object obj) =>
            new(type, JsonSerializer.SerializeToUtf8Bytes(obj));
    }
}
