using ExcelToPlcJson;

try
{
    #region 手动输入
    //// 使用示例 - 请修改为你的实际路径
    //Console.WriteLine("=== Excel 转 PLC JSON 工具 ===\n");

    //Console.Write("请输入 Excel 文件路径: ");
    //string excelPath = Console.ReadLine()?.Trim('"') ?? @"C:\temp\points.xlsx";

    //Console.Write("请输入 Sheet 名称(留空取第一个): ");
    //string sheetName = Console.ReadLine();

    //Console.Write("请输入输出 JSON 路径: ");
    //string outputPath = Console.ReadLine()?.Trim('"') ?? @"C:\temp\output.json";
    #endregion

    #region 批量处理
    {
        string excelBasePath = @"D:\Downloads\层压机数采数据明细_202602.xlsx";
        //// 测试环境输出路径
        //string jsonOutputDir = @"D:\works\005\PlcController";
        // 生产环境输出路径
        string jsonOutputDir = @"D:\works\005\PlcController\现场配置";
        List<(string, string, string, ExcelProcessor)> values = new List<(string, string, string, ExcelProcessor)>
        {
            // ("Excel文件路径", "Sheet名称", "输出JSON路径", ExcelProcessor实例)
            //(excelBasePath, "DB1", Path.Combine(jsonOutputDir,"DB1.json"),
            //    new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB2", Path.Combine(jsonOutputDir,"DB2.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB4", Path.Combine(jsonOutputDir,"DB4.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB5", Path.Combine(jsonOutputDir,"DB5.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB6", Path.Combine(jsonOutputDir,"DB6.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB7", Path.Combine(jsonOutputDir,"DB7.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB10", Path.Combine(jsonOutputDir,"DB10.json"),
                new ExcelProcessor(new ParserConfig(){
                     StartRow=8 // DB10表格起始行不同
                })),
            (excelBasePath, "DB11", Path.Combine(jsonOutputDir,"DB11.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB14", Path.Combine(jsonOutputDir,"DB14.json"),
                new ExcelProcessor(new ParserConfig())),
            (excelBasePath, "DB21", Path.Combine(jsonOutputDir,"DB21.json"),
                new ExcelProcessor(new ParserConfig()
                {
                    StartRow=8 // DB21表格起始行不同
                })),
            // //测试环境M区地址偏移量为1000，正式环境不需要
            //(excelBasePath, "MArea_NoAlarm", Path.Combine(jsonOutputDir,"MArea_NoAlarm.json"),
            //    new ExcelProcessor(new ParserConfig()
            //    {MAreaBaseOffset=1000})),
            //(excelBasePath, "MArea_Alarm", Path.Combine(jsonOutputDir,"MArea_Alarm.json"),
            //    new ExcelProcessor(new ParserConfig()
            //    {MAreaBaseOffset=1000})),
            // 正式环境M区地址不需要偏移
            (excelBasePath, "MArea_NoAlarm", Path.Combine(jsonOutputDir,"MArea_NoAlarm.json"),
                new ExcelProcessor(new ParserConfig()
                )),
            (excelBasePath, "MArea_Alarm", Path.Combine(jsonOutputDir,"MArea_Alarm.json"),
                new ExcelProcessor(new ParserConfig()
                )),
        };
        foreach ((var excelPath, var sheetName, var outputPath, var processor) in values)
        {
            processor.Process(excelPath, sheetName, outputPath);
        }
    }
    #endregion
    #region 单个处理
    {

        //// 配置参数
        //var config = new ParserConfig
        //{
        //    //StartRow = 29,           // 从第29行开始（第1行是表头）
        //    NameColumnIndex = 1,    // A列：数据名称
        //    AddressColumnIndex = 4, // D列：点位名（DBD120等）
        //    MAreaBaseOffset = 1000  // M区基准偏移量
        //};
        //var processor = new ExcelProcessor(config);
        //string excelPath = @"D:\Downloads\层压机数采数据明细_202602.xlsx";
        //string sheetName = "M区报警";
        //string outputPath = @"D:\works\005\PlcController\M区报警.json";
        //processor.Process(excelPath, sheetName, outputPath);

    }
    #endregion
    Console.WriteLine("\n按任意键退出...");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine($"程序错误: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadKey();
}