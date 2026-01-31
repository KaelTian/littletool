using Microsoft.Win32.TaskScheduler;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using TaskSchedulerManager.Models;

namespace TaskSchedulerManager.Core
{
    public class TaskSchedulerHelper
    {

        public static bool RegisterTasksSafe(SchedulerProfile profile, out string message)
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    // 确保使用当前用户权限，避免 SYSTEM 账户的复杂性
                    string userId = WindowsIdentity.GetCurrent().Name;
                    bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                        .IsInRole(WindowsBuiltInRole.Administrator);

                    if (!isAdmin)
                    {
                        message = "必须以管理员身份运行此程序！";
                        return false;
                    }

                    // 创建/获取文件夹
                    TaskFolder folder;
                    try
                    {
                        folder = ts.RootFolder.CreateFolder("MyAppLaunchers", null, false);
                    }
                    catch
                    {
                        folder = ts.RootFolder.SubFolders["MyAppLaunchers"];
                    }

                    // 删除旧任务（如果存在）
                    string taskName = profile.TaskNamePrefix + "Main";
                    try
                    {
                        folder.DeleteTask(taskName);
                    }
                    catch { /* 可能不存在，忽略 */ }

                    // 生成脚本
                    string scriptDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "TaskSchedulerManager",
                        profile.ProfileName);
                    Directory.CreateDirectory(scriptDir);

                    string scriptPath = Path.Combine(scriptDir, "startup.ps1");
                    string logPath = Path.Combine(scriptDir, "logs");
                    Directory.CreateDirectory(logPath);

                    string psScript = ScriptGenerator.GenerateStartupScript(profile.Apps, logPath);
                    File.WriteAllText(scriptPath, psScript, new UTF8Encoding(true));

                    // 创建任务定义 - 使用最简单可靠的配置
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = $"Launch {profile.Apps.Count} apps";
                    td.Settings.Enabled = true;
                    td.Settings.AllowHardTerminate = true;
                    td.Settings.DisallowStartIfOnBatteries = false;
                    td.Settings.StopIfGoingOnBatteries = false;
                    td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                    // 触发器：使用 Logon 而非 Boot（兼容性更好）
                    // Boot 触发器在某些系统上需要特殊权限
                    LogonTrigger logonTrigger = new LogonTrigger();
                    logonTrigger.Delay = TimeSpan.FromSeconds(profile.BootDelaySeconds);
                    logonTrigger.UserId = userId; // 指定当前用户
                    td.Triggers.Add(logonTrigger);

                    // 备选：开机触发器（可选，如果上面不行就用这个）
                    // BootTrigger bootTrigger = new BootTrigger();
                    // bootTrigger.Delay = TimeSpan.FromSeconds(30);
                    // td.Triggers.Add(bootTrigger);

                    // 操作 - 关键修复：正确处理引号
                    string powerShellPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
                    // 注意：参数中的引号需要小心处理
                    string arguments = $"-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File \"{scriptPath}\"";

                    if (profile.Apps.Any(a => a.AutoRestart))
                    {
                        arguments += " -MonitorMode";
                    }

                    td.Actions.Add(new ExecAction(powerShellPath, arguments, scriptDir));

                    // Principal - 使用当前用户而非 SYSTEM（避免权限问题）
                    td.Principal.UserId = userId;
                    td.Principal.LogonType = TaskLogonType.InteractiveToken;
                    td.Principal.RunLevel = TaskRunLevel.Highest;

                    // 注册 - 关键修改：明确指定用户
                    folder.RegisterTaskDefinition(
                        taskName,
                        td,
                        TaskCreation.CreateOrUpdate,
                        userId,
                        null,
                        TaskLogonType.InteractiveToken);

                    message = $"注册成功！\n任务名: {taskName}\n用户: {userId}\n脚本: {scriptPath}\n\n注意：此任务将在用户登录时启动（如需开机启动，需使用 Boot 触发器并确保以 SYSTEM 运行）";
                    return true;
                }
            }
            catch (COMException comEx)
            {
                message = $"COM 错误: {comEx.Message}\n\n可能原因：\n1. 未以管理员身份运行\n2. 任务名称冲突\n3. 路径包含特殊字符\n\n建议：右键→以管理员身份运行";
                return false;
            }
            catch (Exception ex)
            {
                message = $"错误: {ex.Message}\n\n堆栈: {ex.StackTrace}";
                return false;
            }
        }

        public static bool UnregisterTasks(string taskNamePrefix, out string message)
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    var folder = ts.RootFolder.SubFolders.FirstOrDefault(f => f.Name == "MyAppLaunchers");
                    if (folder != null)
                    {
                        var tasksToDelete = folder.GetTasks()
                            .Where(t => t.Name.StartsWith(taskNamePrefix))
                            .ToList();

                        foreach (var task in tasksToDelete)
                        {
                            folder.DeleteTask(task.Name);
                        }

                        message = $"已删除 {tasksToDelete.Count} 个任务";
                    }
                    else
                    {
                        message = "没有找到任务";
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = $"取消注册失败: {ex.Message}";
                return false;
            }
        }

        public static List<string> GetRegisteredTaskStatus(string taskNamePrefix)
        {
            var status = new List<string>();
            try
            {
                using (TaskService ts = new TaskService())
                {
                    var folder = ts.RootFolder.SubFolders.FirstOrDefault(f => f.Name == "MyAppLaunchers");
                    if (folder != null)
                    {
                        foreach (var task in folder.GetTasks().Where(t => t.Name.StartsWith(taskNamePrefix)))
                        {
                            var state = task.State == TaskState.Running ? "运行中" :
                                       task.State == TaskState.Ready ? "就绪" : "停止";
                            status.Add($"{task.Name}: {state} (上次运行: {task.LastRunTime})");
                        }
                    }
                }
            }
            catch { }
            return status;
        }
    }
}