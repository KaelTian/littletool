using System.ComponentModel;

namespace TaskSchedulerManager.Models
{
    public class AppStartupConfig
    {
        [DisplayName("名称")]
        public string? Name { get; set; }

        [DisplayName("程序路径")]
        public string? ExePath { get; set; }

        [DisplayName("启动参数")]
        public string? Arguments { get; set; }

        [DisplayName("工作目录")]
        public string? WorkingDirectory { get; set; }

        [DisplayName("启动顺序")]
        public int Order { get; set; }

        [DisplayName("启动后等待(秒)")]
        public int DelayAfterStart { get; set; } = 2;

        [DisplayName("健康检查URL")]
        public string? HealthCheckUrl { get; set; } // 如 http://localhost:8091/health

        [DisplayName("自动重启")]
        public bool AutoRestart { get; set; } = false;

        [DisplayName("最大重启次数")]
        public int MaxRestarts { get; set; } = 3;

        //[DisplayName("日志目录")]
        //public string? LogDirectory { get; set; }

        [Browsable(false)]
        public string Id => Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public class SchedulerProfile
    {
        public string ProfileName { get; set; } = "DefaultProfile";
        public List<AppStartupConfig> Apps { get; set; } = new List<AppStartupConfig>();
        public bool RunWhetherUserLoggedOn { get; set; } = true;
        public bool RunWithHighestPrivileges { get; set; } = true;
        public int BootDelaySeconds { get; set; } = 30;
        public string TaskNamePrefix { get; set; } = "MyAppLauncher_";
    }
}