using System.Text;
using TaskSchedulerManager.Models;

namespace TaskSchedulerManager.Core
{
    public class ScriptGenerator
    {
        public static string GenerateStartupScript(List<AppStartupConfig> apps, string logBasePath)
        {
            // 前置校验：避免空列表或无效日志路径导致生成无用脚本
            if (apps == null || !apps.Any())
            {
                throw new ArgumentException("应用配置列表不能为空且必须包含至少一个应用", nameof(apps));
            }
            if (string.IsNullOrEmpty(logBasePath))
            {
                throw new ArgumentException("日志基础路径不能为空", nameof(logBasePath));
            }

            var sb = new StringBuilder();
            // 脚本头
            sb.AppendLine("# 自动生成的启动脚本 - 由 TaskSchedulerManager 创建");
            sb.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"); // 格式化时间，提升可读性
            sb.AppendLine("param(");
            sb.AppendLine("    [switch]$MonitorMode = $false  # 监控模式：保持运行并监控进程");
            sb.AppendLine(")");
            sb.AppendLine();
            sb.AppendLine("$ErrorActionPreference = 'Stop'"); // 改为Stop，更易捕获致命错误
            sb.AppendLine("$jobs = @{}");
            // 优化路径转义：使用PowerShell的单引号包裹，简化C#转义逻辑
            string psLogBasePath = logBasePath.Replace("\\", "\\\\").Replace("'", "''");
            sb.AppendLine($"$logDir = '{psLogBasePath}'");
            sb.AppendLine("if (-not (Test-Path $logDir)) {");
            sb.AppendLine("    New-Item -ItemType Directory -Path $logDir -Force | Out-Null");
            sb.AppendLine("    Write-Host \"创建日志目录: $logDir\"");
            sb.AppendLine("}");
            sb.AppendLine();

            // 日志函数（保留原有功能，优化换行格式）
            sb.AppendLine("function Write-Log {");
            sb.AppendLine("    param([string]$Message, [string]$Level = 'INFO')");
            sb.AppendLine("    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'");
            sb.AppendLine("    $logEntry = \"[$timestamp] [$Level] $Message\"");
            sb.AppendLine("    Write-Host $logEntry");
            sb.AppendLine("    $logFile = Join-Path -Path $logDir -ChildPath 'launcher.log'");
            sb.AppendLine("    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8"); // 指定UTF8，避免中文乱码
            sb.AppendLine("}");
            sb.AppendLine();

            // 优化启动函数：改为接收单个应用配置，实现复用（关键修复）
            sb.AppendLine("function Start-Application {");
            sb.AppendLine("    param([PSObject]$AppConfig)"); // 使用PSObject，兼容PowerShell内的配置传递
            sb.AppendLine();
            sb.AppendLine("    # 校验配置必填项");
            sb.AppendLine("    if (-not $AppConfig.Name -or -not $AppConfig.ExePath) {");
            sb.AppendLine("        Write-Log \"错误：应用配置缺少必填项（名称/可执行文件路径）\" 'ERROR'");
            sb.AppendLine("        return");
            sb.AppendLine("    }");
            sb.AppendLine();

            // 在 Start-Application 函数中，添加“校验配置必填项”之后，“定义日志路径”之前
            sb.AppendLine("    # 前置清理：终止同名旧进程（避免多个进程并存）");
            sb.AppendLine("    $processName = [System.IO.Path]::GetFileNameWithoutExtension($AppConfig.ExePath)");
            sb.AppendLine("    $oldProcesses = Get-Process -Name $processName -ErrorAction SilentlyContinue");
            sb.AppendLine("    if ($oldProcesses) {");
            sb.AppendLine("        Write-Log \"发现 $($oldProcesses.Count) 个旧进程，正在终止...\" 'WARN'");
            sb.AppendLine("        foreach ($oldProc in $oldProcesses) {");
            sb.AppendLine("            try {");
            sb.AppendLine("                $oldProc.Kill();");
            sb.AppendLine("                $oldProc.WaitForExit(5000); # 等待5秒，让进程正常退出");
            sb.AppendLine("                Write-Log \"已终止旧进程 $($oldProc.Id)（$processName）\"");
            sb.AppendLine("            } catch {");
            sb.AppendLine("                Write-Log \"终止旧进程 $($oldProc.Id) 失败：$($_.Exception.Message)\" 'ERROR'");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        # 终止后延迟1秒，避免资源未释放");
            sb.AppendLine("        Start-Sleep -Seconds 1");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    $safeName = $AppConfig.Name.Replace(' ', '_')");
            sb.AppendLine("    $stdLogPath = Join-Path -Path $logDir -ChildPath \"$safeName.log\"");
            sb.AppendLine("    $errorLogPath = Join-Path -Path $logDir -ChildPath \"$safeName`_error.log\"");
            sb.AppendLine();

            sb.AppendLine("    # --- 启动 $($AppConfig.Name) ---");
            sb.AppendLine("    Write-Log \"正在启动 $($AppConfig.Name)...\"");
            sb.AppendLine("    try {");
            sb.AppendLine("        $exePath = $AppConfig.ExePath");
            sb.AppendLine("        if (-not (Test-Path $exePath -PathType Leaf)) {"); // 明确校验文件类型，避免目录误判
            sb.AppendLine("            Write-Log \"错误: 找不到可执行文件 $exePath\" 'ERROR'");
            sb.AppendLine("            return");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 构建进程启动信息
            sb.AppendLine("        $startInfo = New-Object System.Diagnostics.ProcessStartInfo");
            sb.AppendLine("        $startInfo.FileName = $exePath");
            sb.AppendLine("        $startInfo.Arguments = if ($AppConfig.Arguments) { $AppConfig.Arguments } else { '' }");
            sb.AppendLine("        # 确定工作目录");
            sb.AppendLine("        $workingDir = if (-not [string]::IsNullOrEmpty($AppConfig.WorkingDirectory)) {");
            sb.AppendLine("            $AppConfig.WorkingDirectory");
            sb.AppendLine("        } else {");
            sb.AppendLine("            Split-Path -Path $exePath -Parent");
            sb.AppendLine("        }");
            sb.AppendLine("        $startInfo.WorkingDirectory = $workingDir");
            sb.AppendLine("        $startInfo.RedirectStandardOutput = $true");
            sb.AppendLine("        $startInfo.RedirectStandardError = $true");
            sb.AppendLine("        $startInfo.UseShellExecute = $false");
            sb.AppendLine("        $startInfo.CreateNoWindow = $true");
            sb.AppendLine("        $startInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8"); // 解决输出日志中文乱码
            sb.AppendLine("        $startInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8");
            sb.AppendLine();

            // 启动进程并捕获信息
            sb.AppendLine("        $process = [System.Diagnostics.Process]::Start($startInfo)");
            sb.AppendLine("        if (-not $process) {");
            sb.AppendLine("            Write-Log \"错误: 无法启动进程 $($AppConfig.Name)\" 'ERROR'");
            sb.AppendLine("            return");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 替换原有“异步读取标准输出/错误”的代码块
            sb.AppendLine("        # 后台作业读取标准输出并写入日志（兼容PowerShell 5.1）");
            sb.AppendLine("        Start-Job -ScriptBlock {");
            sb.AppendLine("            param($process, $logPath)");
            sb.AppendLine("            $output = $process.StandardOutput.ReadToEnd()");
            sb.AppendLine("            if (-not [string]::IsNullOrEmpty($output)) {");
            sb.AppendLine("                Add-Content -Path $logPath -Value $output -Encoding UTF8");
            sb.AppendLine("            }");
            sb.AppendLine("        } -ArgumentList $process, $stdLogPath | Out-Null");
            sb.AppendLine();
            sb.AppendLine("        # 后台作业读取标准错误并写入日志（兼容PowerShell 5.1）");
            sb.AppendLine("        Start-Job -ScriptBlock {");
            sb.AppendLine("            param($process, $logPath)");
            sb.AppendLine("            $errorOutput = $process.StandardError.ReadToEnd()");
            sb.AppendLine("            if (-not [string]::IsNullOrEmpty($errorOutput)) {");
            sb.AppendLine("                Add-Content -Path $logPath -Value $errorOutput -Encoding UTF8");
            sb.AppendLine("            }");
            sb.AppendLine("        } -ArgumentList $process, $errorLogPath | Out-Null");

            // 更新Job缓存（支持重启时覆盖旧记录，避免冗余）
            sb.AppendLine("        # 更新进程缓存");
            sb.AppendLine("        $jobs[$safeName] = @{");
            sb.AppendLine("            Process = $process");
            sb.AppendLine("            Config = $AppConfig");
            sb.AppendLine("            RestartCount = if ($jobs.ContainsKey($safeName)) { $jobs[$safeName].RestartCount + 1 } else { 0 }");
            sb.AppendLine("            StartTime = Get-Date");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 健康检查逻辑（保留原有功能，优化变量命名）
            sb.AppendLine("        # 健康检查或启动后延迟");
            sb.AppendLine("        if (-not [string]::IsNullOrEmpty($AppConfig.HealthCheckUrl)) {");
            sb.AppendLine("            $healthUrl = $AppConfig.HealthCheckUrl");
            sb.AppendLine("            $maxWaitSeconds = 30");
            sb.AppendLine("            $waitedSeconds = 0");
            sb.AppendLine("            $healthCheckPassed = $false");
            sb.AppendLine();
            sb.AppendLine("            while ($waitedSeconds -lt $maxWaitSeconds) {");
            sb.AppendLine("                try {");
            sb.AppendLine("                    $response = Invoke-WebRequest -Uri $healthUrl -TimeoutSec 2 -ErrorAction Stop");
            sb.AppendLine("                    if ($response.StatusCode -eq 200) {");
            sb.AppendLine("                        $healthCheckPassed = $true");
            sb.AppendLine("                        break;");
            sb.AppendLine("                    }");
            sb.AppendLine("                } catch {");
            sb.AppendLine("                    # 忽略健康检查失败，继续等待");
            sb.AppendLine("                }");
            sb.AppendLine("                Start-Sleep -Milliseconds 500");
            sb.AppendLine("                $waitedSeconds += 0.5");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            if ($healthCheckPassed) {");
            sb.AppendLine("                Write-Log \"$($AppConfig.Name) 健康检查通过\"");
            sb.AppendLine("            } else {");
            sb.AppendLine("                Write-Log \"$($AppConfig.Name) 健康检查超时（等待$maxWaitSeconds秒）\" 'WARN'");
            sb.AppendLine("            }");
            sb.AppendLine("        } else {");
            sb.AppendLine("            # 无健康检查，执行启动后延迟（默认1秒，避免配置为空报错）");
            sb.AppendLine("            $delaySeconds = if ($AppConfig.DelayAfterStart -gt 0) { $AppConfig.DelayAfterStart } else { 1 }");
            sb.AppendLine("            Start-Sleep -Seconds $delaySeconds");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        Write-Log \"$($AppConfig.Name) 已启动 (PID: $($process.Id))\"");
            sb.AppendLine("    } catch {");
            sb.AppendLine("        Write-Log \"启动 $($AppConfig.Name) 失败: $($_.Exception.Message)\" 'ERROR'");
            sb.AppendLine("    }");
            sb.AppendLine("}"); // end Start-Application
            sb.AppendLine();

            // 关键补充：定义应用配置列表，并主动调用启动函数（核心修复）
            sb.AppendLine("# 构建应用配置列表");
            sb.AppendLine("$appConfigs = @()");
            var orderedApps = apps.OrderBy(a => a.Order).ToList();
            foreach (var app in orderedApps)
            {
                // 转义特殊字符，避免PowerShell语法错误
                string safeName = app.Name?.Replace("'", "''") ?? "UnnamedApp";
                string safeExePath = app.ExePath?.Replace("'", "''") ?? string.Empty;
                string safeArguments = app.Arguments?.Replace("'", "''") ?? string.Empty;
                string safeWorkingDir = app.WorkingDirectory?.Replace("'", "''") ?? string.Empty;
                string safeHealthUrl = app.HealthCheckUrl?.Replace("'", "''") ?? string.Empty;

                sb.AppendLine("$appConfigs += @{");
                sb.AppendLine($"    Name = '{safeName}'");
                sb.AppendLine($"    ExePath = '{safeExePath}'");
                sb.AppendLine($"    Arguments = '{safeArguments}'");
                sb.AppendLine($"    WorkingDirectory = '{safeWorkingDir}'");
                sb.AppendLine($"    HealthCheckUrl = '{safeHealthUrl}'");
                sb.AppendLine($"    DelayAfterStart = {app.DelayAfterStart}");
                sb.AppendLine($"    AutoRestart = ${app.AutoRestart.ToString().ToLower()}"); // PowerShell布尔值小写
                sb.AppendLine($"    MaxRestarts = {app.MaxRestarts}");
                sb.AppendLine("}");
            }
            sb.AppendLine();

            // 主动遍历配置，启动所有应用（核心：触发实际启动逻辑）
            sb.AppendLine("# 批量启动所有应用");
            sb.AppendLine("foreach ($appConfig in $appConfigs) {");
            sb.AppendLine("    Start-Application -AppConfig $appConfig");
            sb.AppendLine("}");
            sb.AppendLine();

            // 监控循环（优化重启逻辑，修复Job更新问题）
            sb.AppendLine("# 监控模式：保持运行并自动重启异常退出的应用");
            sb.AppendLine("if ($MonitorMode) {");
            sb.AppendLine("    Write-Log '进入监控模式...（按Ctrl+C退出）'");
            sb.AppendLine("    try {");
            sb.AppendLine("        while ($true) {");
            sb.AppendLine("            Start-Sleep -Seconds 5");
            sb.AppendLine("            # 遍历Job的副本，避免遍历过程中修改集合导致错误");
            sb.AppendLine("            foreach ($name in @($jobs.Keys)) {");
            sb.AppendLine("                if (-not $jobs.ContainsKey($name)) { continue }");
            sb.AppendLine("                $job = $jobs[$name]");
            sb.AppendLine("                if ($job.Process.HasExited) {");
            sb.AppendLine("                    $exitCode = $job.Process.ExitCode");
            sb.AppendLine("                    Write-Log \"进程 $name 已退出 (ExitCode: $exitCode)\" 'WARN'");
            sb.AppendLine("                    $appConfig = $job.Config");
            sb.AppendLine();
            sb.AppendLine("                    # 判断是否需要自动重启");
            sb.AppendLine("                    if ($appConfig.AutoRestart -and $job.RestartCount -lt $appConfig.MaxRestarts) {");
            sb.AppendLine("                        Write-Log \"准备重启 $name（当前重启次数：$($job.RestartCount)/$($appConfig.MaxRestarts)）\"");
            sb.AppendLine("                        # 调用启动函数，复用重启逻辑");
            sb.AppendLine("                        Start-Application -AppConfig $appConfig");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        Write-Log \"达到最大重启次数或禁用自动重启，放弃重启 $name\" 'ERROR'");
            sb.AppendLine("                        $jobs.Remove($name)"); // 移除无效Job，清理缓存
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    } catch {");
            sb.AppendLine("        Write-Log \"监控模式异常退出: $($_.Exception.Message)\" 'ERROR'");
            sb.AppendLine("    }");
            sb.AppendLine("} else {");
            sb.AppendLine("    Write-Log '非监控模式，脚本执行完成（进程将继续后台运行）'");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}