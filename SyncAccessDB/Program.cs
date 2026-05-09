using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SyncAccessDB;

// ==================== 1. 配置 Serilog（在 Builder 之前）====================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // 捕获所有级别
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)  // 微软框架的日志只记 Warning 以上
    .Enrich.FromLogContext()
    //.Enrich.WithThreadId()  // 记录线程ID，方便排查并发问题
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Debug)
    .WriteTo.Async(a => a.File(
        path: "logs/sync-service-.log",  // 路径：会自动创建 logs 文件夹
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,     // 按天滚动：sync-service-20240417.log
        rollOnFileSizeLimit: true,                // 单文件超过限制也滚动
        fileSizeLimitBytes: 10 * 1024 * 1024,     // 10MB
        retainedFileCountLimit: 30,               // 保留最近30天（防止磁盘撑爆）
        restrictedToMinimumLevel: LogEventLevel.Information,
        encoding: System.Text.Encoding.UTF8))
    .CreateLogger();

try
{
    Log.Information("正在启动 MES 数据同步服务...");

    var builder = Host.CreateApplicationBuilder(args);

    // 使用 Serilog 替代默认日志
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    // 配置（支持热重载）
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    // 🆕 测试数据生成服务（根据配置自动启用）
    if (builder.Configuration.GetValue<bool>("TestData:Enabled", false))
    {
        builder.Services.AddHostedService<TestDataGeneratorService>();
        Log.Warning("⚠️ 注意：测试数据生成服务已注册（仅用于开发测试）");
    }

    // 注册服务
    builder.Services.AddSingleton<SyncStateService>();
    builder.Services.AddHostedService<DataSyncWorker>();

    var host = builder.Build();

    Log.Information("服务启动成功");
    await host.RunAsync();
}
catch (Exception ex)
{
    // 启动失败时确保写入日志（文件或控制台）
    Log.Fatal(ex, "服务启动失败");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();  // 确保所有日志落盘
}