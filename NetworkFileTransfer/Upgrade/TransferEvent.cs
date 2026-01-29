using System.Text.Json.Serialization;

namespace NetworkFileTransfer.Upgrade
{
    public class TransferEvent : EventArgs
    {
        public string Endpoint { get; }
        public string? FileName { get; }
        public long FileSize { get; }
        public string? StoredPath { get; }

        public TransferEvent(string endpoint, string? fileName = null, long fileSize = 0, string? path = null)
        {
            Endpoint = endpoint;
            FileName = fileName;
            FileSize = fileSize;
            StoredPath = path;
        }
    }

    public class TransferProgressEvent : TransferEvent
    {
        public long BytesTransferred { get; }
        public double ProgressPercent => FileSize > 0 ? (double)BytesTransferred / FileSize * 100 : 0;

        public TransferProgressEvent(string endpoint, string fileName, long current, long total)
            : base(endpoint, fileName, total)
        {
            BytesTransferred = current;
        }
    }

    public class TransferErrorEvent : EventArgs
    {
        public string Context { get; }
        public string Error { get; }
        public TransferErrorEvent(string context, string error) { Context = context; Error = error; }
    }

    public record TransferComplete(
        [property: JsonPropertyName("fileName")] string FileName,
        [property: JsonPropertyName("bytesReceived")] long BytesReceived,
        [property: JsonPropertyName("storedPath")] string? StoredPath
        );
}
