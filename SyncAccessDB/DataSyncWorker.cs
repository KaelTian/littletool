using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace SyncAccessDB
{
    public class DataSyncWorker : BackgroundService
    {
        private readonly ILogger<DataSyncWorker> _logger;
        private readonly SyncStateService _state;
        private readonly IConfiguration _config;

        // 连接字符串
        private readonly string _accessConn;
        private readonly string _mysqlConn;

        public DataSyncWorker(
            ILogger<DataSyncWorker> logger,
            SyncStateService state,
            IConfiguration config)
        {
            _logger = logger;
            _state = state;
            _config = config;

            _accessConn = _config.GetConnectionString("AccessDB")!;
            _mysqlConn = _config.GetConnectionString("MySQL")!;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 这些日志会同时输出到 Console 和 File
            _logger.LogInformation("同步服务启动，上次同步位置：{LastId}", _state.LastSyncedId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var count = await SyncOnceAsync();
                    if (count > 0)
                    {
                        _logger.LogInformation("成功同步 {Count} 条数据，最新序号 {Seq}",
                            count, _state.LastSyncedId);
                    }
                    else
                    {
                        _logger.LogDebug("本次无新数据");  // Debug 级别只写控制台，不写文件
                    }
                }
                catch (Exception ex)
                {
                    // 错误会写入文件，包含堆栈
                    _logger.LogError(ex, "同步失败，等待下次重试");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogWarning("收到取消信号，服务正在停止...");
        }

        private async Task<int> SyncOnceAsync()
        {
            // 1. 从Access读取增量数据（严格大于上次序号）
            List<AccessRecord> newRecords;
            using (var accessDb = CreateAccessClient())
            {
                newRecords = await accessDb.Queryable<AccessRecord>()
                    .Where(r => r.序号 > _state.LastSyncedId)  // 关键：只取新数据
                    .Where(r => !string.IsNullOrWhiteSpace(r.玻璃ID))
                    .OrderBy(r => r.序号)
                    .ToListAsync();

                if (newRecords.Count == 0) return 0;

                _logger.LogDebug("AccessDB返回 {Count} 条新记录", newRecords.Count);
            }

            // 2. 转换并批量插入MySQL（无需查重，直接插入）
            var mesRecords = newRecords.Select(r => new MesRecord
            {
                SourceSeq = r.序号,
                CreateTime = ParseDateTime(r.日期, r.时间),
                Quantity = r.数量,
                CarrierPlate = r.载板,
                GlassId = r.玻璃ID,
                Recipe = r.配方,
                // SyncTime由数据库自动填充或这里赋值
                SyncTime = DateTime.Now
            }).ToList();

            using (var mysqlDb = CreateMysqlClient())
            {
                // 简单直接插入（因为SourceSeq唯一且递增，不可能重复）
                await mysqlDb.Insertable(mesRecords).ExecuteCommandAsync();

                // 3. 更新状态：取本次最大序号
                var maxSeq = newRecords.Max(r => r.序号);
                await _state.UpdateLastSyncedIdAsync(maxSeq);

                return newRecords.Count;
            }
        }

        private SqlSugarClient CreateAccessClient() => new(new ConnectionConfig
        {
            DbType = DbType.Access,
            //InitKeyType = InitKeyType.Attribute,
            ConnectionString = _accessConn,
            IsAutoCloseConnection = true
        });

        private SqlSugarClient CreateMysqlClient() => new(new ConnectionConfig
        {
            DbType = DbType.MySql,
            ConnectionString = _mysqlConn,
            IsAutoCloseConnection = true
        });

        private DateTime ParseDateTime(string? date, string? time)
        {
            var dt = $"{date} {time}";
            return DateTime.TryParse(dt, out var result)
                ? result
                : DateTime.MinValue;
        }
    }
}
