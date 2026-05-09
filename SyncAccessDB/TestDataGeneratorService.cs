using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace SyncAccessDB
{
    public class TestDataGeneratorService : BackgroundService
    {
        private readonly ILogger<TestDataGeneratorService> _logger;
        private readonly IConfiguration _config;
        private readonly string _accessConn;
        private readonly Random _random = new();

        // 模拟产线配置
        private readonly string[] _recipes = { "M20+R5-725-6", "M30+L2-718-8", "A15+T1-801-3", "B40-X9-915-2" };
        private readonly string[] _carrierPrefixes = { "1101", "1102", "2201", "2202", "3301" };
        private int _sequenceCounter = 0;

        public TestDataGeneratorService(ILogger<TestDataGeneratorService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _accessConn = config.GetConnectionString("AccessDB")!;

            // 从配置读取起始序号，避免重复（如果Access已存在数据）
            _sequenceCounter = config.GetValue<int>("TestData:StartSequence", 1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 检查是否启用测试模式
            if (!_config.GetValue<bool>("TestData:Enabled", false))
            {
                _logger.LogInformation("测试数据生成服务已禁用（配置 TestData:Enabled = false）");
                return;
            }

            _logger.LogWarning("⚠️ 测试数据生成服务已启动！每 {Interval} 秒写入一条模拟数据到 AccessDB",
                _config.GetValue<int>("TestData:IntervalSeconds", 10));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await InsertTestRecordAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "写入测试数据失败");
                }

                var interval = TimeSpan.FromSeconds(_config.GetValue<int>("TestData:IntervalSeconds", 10));
                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task InsertTestRecordAsync()
        {
            var now = DateTime.Now;
            var record = new AccessRecord
            {
                // 序号是自增，Access会自动处理，这里给0或不赋值都行
                // 但为了日志显示，我们模拟一个
                序号 = 0,

                日期 = now.ToString("yyyy-MM-dd"),
                时间 = now.ToString("HH:mm:ss"),
                数量 = (short)_random.Next(1, 5),  // 随机 1-4
                载板 = GenerateCarrierPlate(),
                玻璃ID = GenerateGlassId(now),
                配方 = _recipes[_random.Next(_recipes.Length)]
            };

            using var db = new SqlSugarClient(new ConnectionConfig
            {
                DbType = DbType.Access,
                ConnectionString = _accessConn,
                IsAutoCloseConnection = true
            });

            // 插入数据（忽略序号，让Access自增）
            var id = await db.Insertable(record)
                .IgnoreColumns(it => new { it.序号 })  // 忽略自增列
                .ExecuteReturnIdentityAsync();  // 返回生成的序号

            _sequenceCounter++;

            _logger.LogInformation("✅ 已写入测试数据 #{Id}: 玻璃ID={GlassId}, 载板={Carrier}, 配方={Recipe}, 时间={Time}",
                id, record.玻璃ID, record.载板, record.配方, record.时间);
        }

        /// <summary>
        /// 生成载板编号：类似 11011111
        /// </summary>
        private string GenerateCarrierPlate()
        {
            var prefix = _carrierPrefixes[_random.Next(_carrierPrefixes.Length)];
            var suffix = _random.Next(1000, 9999);
            return $"{prefix}{suffix}";
        }

        /// <summary>
        /// 生成玻璃ID：格式 ZNJ1202507250034（前缀+日期+4位流水）
        /// 模拟真实MES中的玻璃追溯码
        /// </summary>
        private string GenerateGlassId(DateTime date)
        {
            var prefix = "ZNJ1";  // 产线代码
            var dateStr = date.ToString("yyyyMMdd");
            var seq = _sequenceCounter.ToString("D4");  // 4位流水，如 0001
            return $"{prefix}{dateStr}{seq}";
        }
    }
}
