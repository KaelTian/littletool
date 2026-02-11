using Newtonsoft.Json;
using OfficeOpenXml;

namespace ExcelToPlcJson
{
    /// <summary>
    /// Excel 处理器
    /// </summary>
    public class ExcelProcessor
    {
        private readonly ParserConfig _config;
        private readonly PlcAddressParser _parser;

        public ExcelProcessor(ParserConfig? config = null)
        {
            _config = config ?? new ParserConfig();
            _parser = new PlcAddressParser(_config);
        }

        /// <summary>
        /// 处理 Excel 文件并生成 JSON
        /// </summary>
        /// <param name="excelPath">Excel文件路径</param>
        /// <param name="sheetName">Sheet名称，为空则取第一个</param>
        /// <param name="outputJsonPath">输出JSON路径</param>
        public void Process(string excelPath, string sheetName, string outputJsonPath)
        {
            if (!File.Exists(excelPath))
                throw new FileNotFoundException("Excel文件不存在", excelPath);

            var points = new List<PlcPoint>();

            //// 设置 EPPlus 许可证（非商业用途）
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage.License.SetNonCommercialPersonal("Kael.Tian");

            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                // 获取指定 Sheet 或第一个
                var worksheet = !string.IsNullOrEmpty(sheetName)
                    ? package.Workbook.Worksheets[sheetName]
                    : package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    throw new Exception($"未找到 Sheet: {sheetName ?? "第一个Sheet"}");

                Console.WriteLine($"正在处理 Sheet: {worksheet.Name}");
                Console.WriteLine($"数据起始行: {_config.StartRow}");
                Console.WriteLine($"M区基准偏移: {_config.MAreaBaseOffset}");

                int rowCount = worksheet.Dimension?.Rows ?? 0;
                int successCount = 0;
                int errorCount = 0;

                // 从起始行开始遍历
                for (int row = _config.StartRow; row <= rowCount; row++)
                {
                    try
                    {
                        // 读取数据名称（第A列）
                        string? name = worksheet.Cells[row, _config.NameColumnIndex].Text?.Trim();

                        // 读取点位地址（第D列，点位名）
                        string? address = worksheet.Cells[row, _config.AddressColumnIndex].Text?.Trim();

                        // 跳过空行
                        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(address))
                            continue;

                        if (string.IsNullOrEmpty(address))
                        {
                            Console.WriteLine($"警告: 第 {row} 行地址为空，跳过");
                            continue;
                        }

                        // 解析地址
                        var (offset, type) = _parser.Parse(address);

                        points.Add(new PlcPoint
                        {
                            Name = name ?? $"未命名_{row}",
                            Offset = offset,
                            Type = type
                        });

                        successCount++;

                        // 调试输出
                        if (row <= _config.StartRow + 5 || row == rowCount)
                        {
                            Console.WriteLine($"行 {row}: {name} | {address} -> Offset:{offset}, Type:{type}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"错误: 第 {row} 行解析失败 - {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine($"\n解析完成: 成功 {successCount} 条, 失败 {errorCount} 条");
            }

            // 生成 JSON
            string json = JsonConvert.SerializeObject(points, Formatting.Indented);
            File.WriteAllText(outputJsonPath, json);

            Console.WriteLine($"JSON 已保存至: {outputJsonPath}");
            Console.WriteLine($"总计生成 {points.Count} 个点位");
        }
    }

}
