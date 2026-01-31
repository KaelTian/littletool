using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using TaskSchedulerManager.Core;
using TaskSchedulerManager.Models;

namespace TaskSchedulerManager
{
    public partial class MainForm : Form
    {

        private SchedulerProfile _currentProfile;
        private string _configPath;
        private BindingSource _bindingSource;
        public MainForm()
        {
            InitializeComponent();
            InitializeData();
            SetupUI();

        }

        private void InitializeData()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaskSchedulerManager",
                "config.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));

            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _currentProfile = JsonConvert.DeserializeObject<SchedulerProfile>(json) ?? new SchedulerProfile();
            }
            else
            {
                _currentProfile = new SchedulerProfile();
            }

            _bindingSource = new BindingSource { DataSource = _currentProfile.Apps };
        }

        private void SetupUI()
        {
            var fileMenu = new ToolStripMenuItem("文件");
            fileMenu.DropDownItems.Add("保存配置", null, (s, e) => SaveConfig());
            fileMenu.DropDownItems.Add("导出脚本", null, (s, e) => ExportScriptOnly());
            menuStrip.Items.Add(fileMenu);
            // 日志框
            var txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.LimeGreen,
                Font = new System.Drawing.Font("Consolas", 10)
            };
            bottomPanel.Controls.Add(txtLog);
            txtLog.AppendText($"[{DateTime.Now}] 配置文件路径: {_configPath}\r\n");
            // 操作按钮面板
            var btnPanel = new Panel { Dock = DockStyle.Right, Width = 200 };
            var btnRegister = new Button { Text = "注册到任务计划", Dock = DockStyle.Top, Height = 40 };
            btnRegister.Click += (s, e) => RegisterToScheduler(txtLog);

            var btnUnregister = new Button { Text = "取消注册", Dock = DockStyle.Top, Height = 40 };
            btnUnregister.Click += (s, e) => UnregisterFromScheduler(txtLog);

            var btnRunNow = new Button { Text = "立即执行测试", Dock = DockStyle.Top, Height = 40 };
            btnRunNow.Click += (s, e) => RunNow(txtLog);

            var btnViewStatus = new Button { Text = "查看任务状态", Dock = DockStyle.Top, Height = 40 };
            btnViewStatus.Click += (s, e) => ViewStatus(txtLog);

            btnPanel.Controls.AddRange(new Control[] { btnViewStatus, btnRunNow, btnUnregister, btnRegister });
            bottomPanel.Controls.Add(btnPanel);
            // GataGridView
            dgv.AutoGenerateColumns = false;
            // 固定宽度的列
            var colOrder = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Order",
                HeaderText = "启动顺序",
                Width = 80,  // 像素
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None  // 固定
            };

            // 按比例填充的列（类似百分比）
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "名称",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 10  // 占比权重
            };

            var colPath = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ExePath",
                HeaderText = "程序路径",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 35  // 占比权重（30:70 分配剩余空间）
            };

            var colArguments = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Arguments",
                HeaderText = "启动参数",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20  // 占比权重
            };

            var colWorkingDirectory = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "WorkingDirectory",
                HeaderText = "工作目录",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 35  // 占比权重（30:70 分配剩余空间）
            };
            dgv.Columns.AddRange(colOrder, colName, colPath, colArguments, colWorkingDirectory);
            dgv.DataSource = _bindingSource;
            dgv.RowPostPaint += dgv_RowPostPaint;
        }

        private void dgv_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            // 行号 = 索引 + 1
            string rowNum = (e.RowIndex + 1).ToString();

            // 计算居中位置
            var size = e.Graphics.MeasureString(rowNum, this.Font);
            float x = e.RowBounds.Left + (dgv.RowHeadersWidth - size.Width) / 2;
            float y = e.RowBounds.Top + (e.RowBounds.Height - size.Height) / 2;

            // 画上
            e.Graphics.DrawString(rowNum, this.Font, Brushes.Black, x, y);
        }


        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var dialog = new AppConfigDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var config = dialog.Config;
                config.Order = _currentProfile.Apps.Count;
                _currentProfile.Apps.Add(config);
                _bindingSource.ResetBindings(false);
                SaveConfig();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            EditCurrentRow();
        }

        private void EditCurrentRow()
        {
            if (_bindingSource.Current is AppStartupConfig current)
            {
                var dialog = new AppConfigDialog(current);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _bindingSource.ResetBindings(false);
                    SaveConfig();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_bindingSource.Current is AppStartupConfig current)
            {
                if (MessageBox.Show($"确认删除 {current.Name}?", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _currentProfile.Apps.Remove(current);
                    ReorderItems();
                    _bindingSource.ResetBindings(false);
                    SaveConfig();
                }
            }
        }

        private void MoveItem(int direction)
        {
            var index = _bindingSource.Position;
            if (index < 0) return;

            var newIndex = index + direction;
            if (newIndex < 0 || newIndex >= _currentProfile.Apps.Count) return;

            var item = _currentProfile.Apps[index];
            _currentProfile.Apps.RemoveAt(index);
            _currentProfile.Apps.Insert(newIndex, item);

            ReorderItems();
            _bindingSource.ResetBindings(false);
            _bindingSource.Position = newIndex;
        }

        private void ReorderItems()
        {
            for (int i = 0; i < _currentProfile.Apps.Count; i++)
            {
                _currentProfile.Apps[i].Order = i;
            }
        }

        private void RegisterToScheduler(TextBox logBox)
        {
            SaveConfig();
            logBox.AppendText($"[{DateTime.Now}] 开始注册任务...\r\n");

            bool success = TaskSchedulerHelper.RegisterTasksSafe(_currentProfile, out string message);

            logBox.AppendText($"[{DateTime.Now}] {message}\r\n");
            if (success)
            {
                MessageBox.Show("注册成功！系统将开机自动按顺序启动这些应用。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UnregisterFromScheduler(TextBox logBox)
        {
            if (MessageBox.Show("确认取消注册所有相关任务？", "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            bool success = TaskSchedulerHelper.UnregisterTasks(_currentProfile.TaskNamePrefix, out string message);
            logBox.AppendText($"[{DateTime.Now}] {message}\r\n");
        }

        private void RunNow(TextBox logBox)
        {
            SaveConfig();
            logBox.AppendText($"[{DateTime.Now}] 生成临时脚本并执行测试...\r\n");

            try
            {
                string scriptDir = Path.Combine(Path.GetTempPath(), "TaskSchedulerManager");
                Directory.CreateDirectory(scriptDir);
                string scriptPath = Path.Combine(scriptDir, "test_startup.ps1");
                string logPath = Path.Combine(scriptDir, "logs");

                string script = ScriptGenerator.GenerateStartupScript(_currentProfile.Apps, logPath);
                File.WriteAllText(scriptPath, script, new UTF8Encoding(true));

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false,
                    WorkingDirectory = scriptDir
                };

                var process = Process.Start(psi);
                logBox.AppendText($"[{DateTime.Now}] 测试进程已启动 (PID: {process.Id})\r\n");
                logBox.AppendText($"[{DateTime.Now}] 脚本位置: {scriptPath}\r\n");
                logBox.AppendText($"[{DateTime.Now}] 执行日志目录: {logPath}\r\n");
            }
            catch (Exception ex)
            {
                logBox.AppendText($"[{DateTime.Now}] 错误: {ex.Message}\r\n");
            }
        }

        private void ViewStatus(TextBox logBox)
        {
            var status = TaskSchedulerHelper.GetRegisteredTaskStatus(_currentProfile.TaskNamePrefix);
            logBox.AppendText($"[{DateTime.Now}] === 任务状态 ===\r\n");
            foreach (var s in status)
            {
                logBox.AppendText($"[{DateTime.Now}] {s}\r\n");
            }
            if (status.Count == 0)
                logBox.AppendText($"[{DateTime.Now}] 未找到已注册的任务\r\n");
        }

        private void ExportScriptOnly()
        {
            var sfd = new SaveFileDialog { Filter = "PowerShell脚本|*.ps1", FileName = "startup.ps1" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string script = ScriptGenerator.GenerateStartupScript(_currentProfile.Apps,
                    Path.Combine(Path.GetDirectoryName(sfd.FileName), "logs"));
                File.WriteAllText(sfd.FileName, script, new UTF8Encoding(true));
                MessageBox.Show("脚本已导出，可手动添加到任务计划程序", "完成");
            }
        }

        private void SaveConfig()
        {
            var json = JsonConvert.SerializeObject(_currentProfile, Formatting.Indented);
            File.WriteAllText(_configPath, json, new UTF8Encoding(true));
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            MoveItem(-1);
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            MoveItem(1);
        }

        private void BtnEdit_Click(object sender, DataGridViewCellEventArgs e)
        {
            EditCurrentRow();
        }
    }
}
