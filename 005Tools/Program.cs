// See https://aka.ms/new-console-template for more information
using _005Tools;
using NModbus;
using System.Drawing.Text;
using System.Net.Sockets;
using System.Text;
using Tool.Service;


//FileCleaner fileCleaner = new FileCleaner(
//    targetFolder: @"\\192.168.0.189\FilesBackup",
//    expireDays: 3,
//    useLastAccessTime: false);

//fileCleaner.StartCleaning();

//TestJiGuangOperationModel();

//await TestPeriodicTimerBehavior();

//SyncFolderFiles();

//RawStringLiteralsDemo.LiteralsDemo();

//await FileDownloaderDemo();

//TestReadJsonFile("""D:\xwechat_files\wxid_ktv024u2o2l341_d47f\msg\file\2025-12\Config.Json""");

//TestJiGuangOperationModel();

//TestRandomDataGenerator();

//TestRefAndOutMethod();

//TestDictionaryBehavior();

//ContravariantDemo.TestContravariance();

//EventDemo.Main(new string[] { });

await ModbusClientTest();

async Task ModbusClientTest()
{
    string ServerIp = "192.168.0.189";
    int Port = 502;

    // 从站地址（Slave ID）
    byte SlaveId = 1;

    using var tcp = new TcpClient();
    await tcp.ConnectAsync(ServerIp, Port);
    var factory = new ModbusFactory();
    IModbusMaster master = factory.CreateMaster(tcp);

    Console.WriteLine("已连接到虚拟服务器 …");

    // 1. 写单个线圈（bool）→ 地址 0001
    bool coiValue = true;
    await master.WriteSingleCoilAsync(SlaveId, 1, coiValue);

    // 2. 写单个保持寄存器（16-bit int）→ 地址 4001
    ushort intValue = 12345;
    await master.WriteSingleRegisterAsync(SlaveId, 0, intValue);

    // 寄存器地址=4001-4000-1
    Console.WriteLine($"写寄存器 4001 = {intValue}");

    // 3. 写字符串 → 地址 4010 开始，长度 10 个寄存器（20 字节）
    string str = "Hello Modbus!";
    ushort startReg = 9; // 4010 对应的偏移 = 4010-4000-1
    byte[] bytes = Encoding.ASCII.GetBytes(str.PadRight(20, '\0')); // 定长 20
    ushort[] registers = BytesToUshorts(bytes);
    await master.WriteMultipleRegistersAsync(SlaveId, startReg, registers);
    Console.WriteLine($"写字符串 4010-4019 : {str}");

    Console.WriteLine("全部写入完成，按任意键退出 …");
    Console.ReadKey();

    ushort[] BytesToUshorts(byte[] bytes)
    {
        if (bytes.Length % 2 != 0)
            Array.Resize(ref bytes, bytes.Length + 1);
        var result = new ushort[bytes.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = (ushort)((bytes[2 * i] << 8) | bytes[2 * i + 1]);
        }
        return result;
    }
}

void TestDictionaryBehavior()
{
    var list1 = new List<object>();
    list1.Add(new Dictionary<string, object> { { "Key1", true } });
    list1.Add(new Dictionary<string, object> { { "Key2", 123 } });
    list1.Add(new Dictionary<string, object> { { "Key3", "KaelTian" } });
    var dicList = new List<Dictionary<string, object>>();
    foreach (var item in list1)
    {
        if (item is Dictionary<string, object> dic)
        {
            dicList.Add(dic);
        }
    }
    ChargeObjectInfos(new Dictionary<string, object>
    {
        { "List Object",list1 }
    });
    ChargeObjectInfos(new Dictionary<string, object>
    {
        { "List Dictionary",dicList }
    });
    void ChargeObjectInfos(Dictionary<string, object> content)
    {
        foreach (KeyValuePair<string, object> item in content)
        {
            // 原代码
            if (item.Value is List<object> list)
            {
                foreach (IDictionary<string, object> propertys in list)
                {
                    foreach (var property in propertys)
                    {
                        Console.WriteLine($"List object Key: {property.Key},Value: {property.Value}");
                    }
                }
            }

            // 改成
            if (item.Value is IEnumerable<object> rows)
            {
                foreach (IDictionary<string, object> propertys in rows)
                {
                    foreach (var property in propertys)
                    {
                        Console.WriteLine($"IEnumerable object Key: {property.Key},Value: {property.Value}");
                    }
                }
            }
        }
    }

    void ChargeObjectInfos1(Dictionary<string, Dictionary<string, object>> content)
    {
        foreach (var item in content)
        {
            foreach (var property in item.Value)
            {
                Console.WriteLine($"IEnumerable object Key: {property.Key},Value: {property.Value}");
            }
        }
    }
}

void TestRefAndOutMethod()
{
    RefAndOutExample.Run();
}

void TestRandomDataGenerator()
{
    try
    {
        var tbMachineProcessDataDto = TestGenericDataGenetor<TBMachineProcessDataDto>.GenerateTestData((message) =>
        {
            Console.WriteLine(message);
        });
        var tbMachineAxisDataDto = TestGenericDataGenetor<TBMachineAxisDataDto>.GenerateTestData((message) =>
        {
            Console.WriteLine(message);
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}

async Task FileDownloaderDemo()
{
    await FileDownloader.DownloadFileAsync("http://192.168.0.189:9527/JJ_Alarms_Config.json",
        """D:\works\005\005Tools\005Tools\bin\Debug\net8.0\JJ_Alarms_Config.json""");
}


void TestConversion()
{
    // 测试不同大小的文件
    TestFileSizeFormatting();

    // 模拟转换结果测试
    TestConversionResults();

    try
    {
        var sourceBMPFile = "\\\\192.168.0.189\\SharedImages\\ZNJ1202511030016\\ZNJ1202511030016_202511031607487311.bmp";
        var singleResult = BmpToJpgConverter.ConvertSingleFile(sourceBMPFile,
            BmpToJpgConverter.GenerateTargetJpgPath(sourceBMPFile, "\\\\192.168.0.189\\SharedImages", "\\\\192.168.0.209\\destimages"));

        Console.WriteLine($"单文件转换结果: {singleResult.GetSizeInfo()}");

        var batchResult = BmpToJpgConverter.ConvertDirectory("\\\\192.168.0.189\\SharedImages", "\\\\192.168.0.209\\destimages");
        batchResult.PrintSummary();
        batchResult.PrintStatistics();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error:" + ex.ToString());
    }
}
void TestFileSizeFormatting()
{
    Console.WriteLine("=== 文件大小格式化测试 ===");

    long[] testSizes = {
            123,                    // 123B
            1234,                   // 1.2KB
            12345,                  // 12.1KB
            123456,                 // 120.6KB
            1234567,                // 1.2MB
            12345678,               // 11.8MB
            123456789,              // 117.7MB
            1234567890,             // 1.1GB
            12345678901,            // 11.5GB
            123456789012            // 115.0GB
        };

    foreach (var size in testSizes)
    {
        Console.WriteLine($"{size} bytes = {BatchConversionResult.FormatFileSize(size)}");
    }
}

void TestConversionResults()
{
    Console.WriteLine("\n=== 转换结果测试 ===");

    // 模拟不同大小的转换结果
    var results = new[]
    {
            new ConversionResult {
                Success = true,
                Message = "small_image.bmp",
                OriginalSize = 1024,         // 1KB
                NewSize = 512,               // 0.5KB
                CompressionRatio = 0.5
            },
            new ConversionResult {
                Success = true,
                Message = "medium_image.bmp",
                OriginalSize = 5 * 1024 * 1024,  // 5MB
                NewSize = 800 * 1024,            // 800KB
                CompressionRatio = 0.156
            },
            new ConversionResult {
                Success = true,
                Message = "large_image.bmp",
                OriginalSize = 150 * 1024 * 1024, // 150MB
                NewSize = 15 * 1024 * 1024,       // 15MB
                CompressionRatio = 0.1
            },
            new ConversionResult {
                Success = false,
                Message = "corrupted.bmp",
                OriginalSize = 0,
                NewSize = 0,
                CompressionRatio = 0
            }
        };

    foreach (var result in results)
    {
        var status = result.Success ? "✓" : "✗";
        Console.WriteLine($"{status} {result.Message}");
        if (result.Success)
        {
            Console.WriteLine($"  标准显示: {result.GetSizeInfo()}");
            Console.WriteLine($"  详细显示: {result.GetDetailedSizeInfo()}");
            Console.WriteLine($"  简化显示: {result.GetSimpleSizeInfo()}");
        }
        Console.WriteLine();
    }

    // 测试批量结果
    var batchResult = new BatchConversionResult
    {
        TotalFiles = 100,
        SuccessfulConversions = 95,
        FailedConversions = 3,
        SkippedFiles = 2,
        TotalOriginalSize = 10L * 1024 * 1024 * 1024, // 10GB
        TotalNewSize = 1L * 1024 * 1024 * 1024,       // 1GB
        IndividualResults = results.ToList()
    };

    batchResult.PrintSummary();
    batchResult.PrintStatistics();
}

void TestReadJsonFile(string filePath)
{
    string jsonString = File.ReadAllText(filePath);
    var deserializedModel = JiGuangOperationModel.DeserializeFromJson(jsonString);
    Console.WriteLine($"\n反序列化成功!");
    Console.WriteLine($"类型: {deserializedModel?.Type}");
    Console.WriteLine($"脉宽: {deserializedModel?.GetDataValue<double>("MES_QRLaserPulseWidth")}");
    Console.WriteLine($"频率: {deserializedModel?.GetDataValue<double>("MES_QRLaserFrequency")}");
    Console.WriteLine($"模板: {deserializedModel?.GetDataValue<string>("MES_QRActiveRecipe")}");
    Console.WriteLine($"玻璃型号: {deserializedModel?.GetDataValue<string>("MES_QRGlassModel")}");
    Console.WriteLine($"编辑序列号: {deserializedModel?.GetDataValue<string>("MES_QRID")}");
}

//测试方法
void TestJiGuangOperationModel()
{
    // 方法1：直接创建并设置数据
    var model1 = new JiGuangOperationModel();
    model1.Data["MES_QRLaserPulseWidth"] = 12.2;
    model1.Data["MES_QRLaserFrequency"] = 1.3;
    model1.Data["MES_QRActiveRecipe"] = "模板A";
    model1.Data["MES_QRGlassModel"] = "玻璃B";
    model1.Data["MES_QRID"] = "SN123456";

    // 方法2：使用便捷方法设置
    var model2 = new JiGuangOperationModel();
    model2.SetDataValue("MES_QRLaserPulseWidth", 15.5);
    model2.SetDataValue("MES_QRLaserFrequency", 2.0);
    model2.SetDataValue("MES_QRActiveRecipe", "模板B");

    // 方法3：使用示例数据
    var model3 = JiGuangOperationModel.CreateExample();

    // 序列化为JSON
    string json = model3.SerializeToJson();
    Console.WriteLine("序列化结果:");
    Console.WriteLine(json);
    File.WriteAllText("JiGuangOperationModel_Example.json", json);

    // 反序列化示例
    string jsonString = @"{
            ""type"": ""LASERQR_Change"",
            ""data"": {
                ""MES_QRLaserPulseWidth"": 12.2,
                ""MES_QRLaserFrequency"": 1.3,
                ""MES_QRActiveRecipe"": ""当前加工模板"",
                ""MES_QRGlassModel"": ""玻璃型号"",
                ""MES_QRID"": ""编辑序列号"",
                ""MES_ISProcessMode"": true
            }
        }";

    var deserializedModel = JiGuangOperationModel.DeserializeFromJson(jsonString);
    Console.WriteLine($"\n反序列化成功!");
    Console.WriteLine($"类型: {deserializedModel?.Type}");
    Console.WriteLine($"脉宽: {deserializedModel?.GetDataValue<double>("MES_QRLaserPulseWidth")}");
    Console.WriteLine($"频率: {deserializedModel?.GetDataValue<double>("MES_QRLaserFrequency")}");
    Console.WriteLine($"模板: {deserializedModel?.GetDataValue<string>("MES_QRActiveRecipe")}");
    Console.WriteLine($"加工模式: {deserializedModel?.GetDataValue<bool>("MES_ISProcessMode")}");

    // 遍历所有数据
    Console.WriteLine("\n所有数据:");
    foreach (var item in deserializedModel!.Data)
    {
        Console.WriteLine($"{item.Key}: {item.Value} ({item.Value.GetType().Name})");
    }
}

async Task TestPeriodicTimerBehavior()
{
    using var cts = new CancellationTokenSource();
    int executionCount = 0;
    var startTime = DateTime.UtcNow;

    Console.WriteLine($"测试开始时间: {startTime:HH:mm:ss}");
    Console.WriteLine("=" + new string('=', 50));

    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
    try
    {

        while (await timer.WaitForNextTickAsync(cts.Token))
        {
            var cycleStart = DateTime.UtcNow;
            executionCount++;

            Console.WriteLine($"[{executionCount}] 周期开始: {cycleStart:HH:mm:ss}");
            Console.WriteLine($"[{executionCount}] 距离开始时间: {(cycleStart - startTime).TotalSeconds:F1}s");

            // 模拟不稳定的执行时间：1-7秒
            var workTime = TimeSpan.FromSeconds(1 + (executionCount % 7));
            Console.WriteLine($"[{executionCount}] 模拟工作时间: {workTime.TotalSeconds}s");

            await Task.Delay(workTime, cts.Token);

            var cycleEnd = DateTime.UtcNow;
            var actualInterval = cycleEnd - cycleStart;
            var expectedNextTick = cycleStart.AddSeconds(3);

            Console.WriteLine($"[{executionCount}] 周期结束: {cycleEnd:HH:mm:ss}");
            Console.WriteLine($"[{executionCount}] 实际耗时: {actualInterval.TotalSeconds:F1}s");
            Console.WriteLine($"[{executionCount}] 预期下次tick: {expectedNextTick:HH:mm:ss}");

            if (executionCount >= 40)
            {
                cts.Cancel();
                Console.WriteLine("已达到10次执行，取消计时器");
            }

            Console.WriteLine("---");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }

    var totalTime = DateTime.UtcNow - startTime;
    Console.WriteLine("=" + new string('=', 50));
    Console.WriteLine($"测试总结:");
    Console.WriteLine($"总执行次数: {executionCount}");
    Console.WriteLine($"总耗时: {totalTime.TotalSeconds:F1}s");
    Console.WriteLine($"平均每次间隔: {totalTime.TotalSeconds / executionCount:F1}s");
}

void SyncFolderFiles()
{
    // 配置参数
    string sourceRoot = @"D:\works\005\GOOD";     // 源目录
    string targetRoot = @"D:\works\005\TargetImages";     // 目标目录

    // 创建同步器
    var synchronizer = new RandomFileSynchronizer(sourceRoot, targetRoot)
    {
        SyncIntervalSeconds = 60,           // 每60秒同步一次
        MinFilesPerCycle = 2,              // 每周期最少2个文件
        MaxFilesPerCycle = 8,              // 每周期最多8个文件
        CreateDirectories = true,          // 创建目录结构
        OverwriteExisting = false,         // 不覆盖已存在文件
        FilePattern = "*.bmp"              // 同步BMP文件
    };

    // 启动同步
    synchronizer.Start();

    // 保持程序运行
    Console.WriteLine("按任意键停止同步...");
    Console.ReadKey();

    // 停止同步
    synchronizer.Stop();

    // 打印统计信息
    synchronizer.PrintStatistics();
}

internal class TemperatureSensor
{
    // 使用事件保护委托
    private EventHandler<TemperatureChangedEventArgs>? _temperatureChanged;

    public event EventHandler<TemperatureChangedEventArgs> TemperatureChanged
    {
        add
        {
            _temperatureChanged += value;
        }
        remove
        {
            _temperatureChanged -= value;
        }
    }

    // 事件只能在类内部触发
    private void OnTemperatureChanged(double newTemp)
    {
        _temperatureChanged?.Invoke(this, new TemperatureChangedEventArgs(newTemp));
    }
}

public class TemperatureChangedEventArgs : EventArgs
{
    public double Temperature { get; private set; }

    public TemperatureChangedEventArgs(double temperature)
    {
        Temperature = temperature;
    }
}