using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;



string inputDir = @"D:\works\005\005_2.0\trunk\03.开发\Collect\PlcPoint\Models\TransmissionLine";
string outputDir = @"D:\works\005\Refactored";

if (!Directory.Exists(outputDir))
    Directory.CreateDirectory(outputDir);

var files = Directory.GetFiles(inputDir, "SiemensTransmissionLineA*.cs")
                     .OrderBy(f => f);

foreach (var file in files)
{
    ProcessFile(file, outputDir);
}

Console.WriteLine("全部处理完成。");




void ProcessFile(string inputPath, string outputDir)
{
    string fileName = Path.GetFileNameWithoutExtension(inputPath);
    string content = File.ReadAllText(inputPath, Encoding.UTF8);
    var mappings = new List< MappingItem > ();

    // 正则从 [JsonPropertyName] 开始匹配，前面的 // ==================== 分隔注释自然保留，不受影响
    string pattern = @"^[ \t]*\[JsonPropertyName\(""([^""]+)""\)\][ \t]*\r?\n" +
                     @"([ \t]*)\[PlcOffset\(""([^""]+)""\)\][ \t]*\r?\n" +
                     @"([ \t]*)(public\s+(\w+)\s+(\w+)\s*\{[^}]+\}[^;]*;)";

    var regex = new Regex(pattern, RegexOptions.Multiline);

    string newContent = regex.Replace(content, m =>
    {
        string rawLabel = m.Groups[1].Value;   // 原中文，如 A_1_1进片状态
        string indent = m.Groups[2].Value;   // PlcOffset 行缩进
        string offset = m.Groups[3].Value;
        string propDec = m.Groups[4].Value;   // 属性声明行缩进
        string fullProp = m.Groups[5].Value;   // 完整属性声明
        string propName = m.Groups[7].Value;   // 属性名，如 A11InletStatus

        string displayId = rawLabel.Replace("_", "-");

        mappings.Add(new MappingItem
        {
            PlcTag = propName,
            Id = displayId,
            Offset = offset
        });

        var sb = new StringBuilder();

        // summary 注释
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {displayId}");
        sb.AppendLine($"{indent}/// </summary>");

        // 保留 JsonPropertyName，但值改为属性名
        sb.AppendLine($"{indent}[JsonPropertyName(\"{propName}\")]");

        // PlcOffset
        sb.AppendLine($"{indent}[PlcOffset(\"{offset}\")]");

        // 原属性声明
        sb.Append($"{propDec}{fullProp}");

        return sb.ToString();
    });

    string outClassPath = Path.Combine(outputDir, Path.GetFileName(inputPath));
    File.WriteAllText(outClassPath, newContent, Encoding.UTF8);

    string jsonFileName = $"PlcDataMapping_{fileName}.json";
    string jsonPath = Path.Combine(outputDir, jsonFileName);
    var json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    File.WriteAllText(jsonPath, json, Encoding.UTF8);

    Console.WriteLine($"[{fileName}] 已处理，点位数：{mappings.Count}");
}



class MappingItem
{
    public string? PlcTag { get; set; }   // 原 key
    public string? Id { get; set; }        // 原 label（中文释义）
    public string? Offset { get; set; }
}