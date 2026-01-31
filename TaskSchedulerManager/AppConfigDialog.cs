using TaskSchedulerManager.Models;

namespace TaskSchedulerManager
{
    public class AppConfigDialog : Form
    {
        private AppStartupConfig _config;
        public AppStartupConfig Config => _config;

        private TextBox txtName, txtPath, txtArgs, txtWd, txtHealth;
        private NumericUpDown numOrder, numDelay, numRestarts;
        private CheckBox chkAutoRestart;

        public AppConfigDialog(AppStartupConfig? editConfig = null)
        {
            _config = editConfig ?? new AppStartupConfig();
            InitializeComponents();
            if (editConfig != null) LoadData();
        }

        private void InitializeComponents()
        {
            this.Text = "应用配置";
            this.Size = new System.Drawing.Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(550, 450);

            // 主布局面板 - 3列：标签列、内容列、按钮列
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 10, // 行数
                Padding = new Padding(10)
            };

            // 设置列宽
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // 标签列
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // 内容列
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // 按钮列（浏览按钮）

            // 设置行高
            for (int i = 0; i < mainLayout.RowCount - 1; i++)
            {
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            }
            // 最后一行用于按钮，高度稍大
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            int row = 0;

            // 名称
            mainLayout.Controls.Add(CreateLabel("名称:"), 0, row);
            txtName = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 5, 0),
                Width = 300
            };
            mainLayout.Controls.Add(txtName, 1, row);
            mainLayout.SetColumnSpan(txtName, 2); // 占用第1和第2列
            row++;

            // 路径
            mainLayout.Controls.Add(CreateLabel("程序路径:"), 0, row);
            txtPath = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 5, 0)
            };
            mainLayout.Controls.Add(txtPath, 1, row);

            var btnBrowse = new Button
            {
                Text = "浏览...",
                Anchor = AnchorStyles.None,
                Margin = new Padding(5, 8, 0, 0),
                Width = 70,
                Height = 25
            };
            btnBrowse.Click += (s, e) => {
                var ofd = new OpenFileDialog
                {
                    Filter = "可执行文件|*.exe;*.bat;*.cmd",
                    FileName = txtPath.Text
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                    txtPath.Text = ofd.FileName;
            };
            mainLayout.Controls.Add(btnBrowse, 2, row);
            row++;

            // 参数
            mainLayout.Controls.Add(CreateLabel("启动参数:"), 0, row);
            txtArgs = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 5, 0)
            };
            mainLayout.Controls.Add(txtArgs, 1, row);
            mainLayout.SetColumnSpan(txtArgs, 2); // 占用第1和第2列
            row++;

            // 工作目录
            mainLayout.Controls.Add(CreateLabel("工作目录:"), 0, row);
            txtWd = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 5, 0)
            };
            mainLayout.Controls.Add(txtWd, 1, row);
            mainLayout.SetColumnSpan(txtWd, 2); // 占用第1和第2列
            row++;

            // 启动顺序
            mainLayout.Controls.Add(CreateLabel("启动顺序:"), 0, row);
            numOrder = new NumericUpDown
            {
                Margin = new Padding(0, 8, 5, 0),
                Width = 120,
                Minimum = 0,
                Maximum = 999
            };
            mainLayout.Controls.Add(numOrder, 1, row);
            // 第2列留空
            row++;

            // 延迟
            mainLayout.Controls.Add(CreateLabel("启动后等待:"), 0, row);

            // 创建一个Panel来包含numDelay和标签
            var delayPanel = new Panel
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Height = 30,
                AutoSize = true
            };

            numDelay = new NumericUpDown
            {
                Margin = new Padding(0, 8, 0, 0),
                Width = 80,
                Minimum = 0,
                Maximum = 300,
                Value = 2,
                Location = new Point(0, 5)
            };

            var lblSeconds = new Label
            {
                Text = "秒",
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(numDelay.Right + 5, 8),
                AutoSize = true
            };

            delayPanel.Controls.Add(numDelay);
            delayPanel.Controls.Add(lblSeconds);
            delayPanel.Width = numDelay.Width + lblSeconds.Width + 10;

            mainLayout.Controls.Add(delayPanel, 1, row);
            row++;

            // 健康检查URL
            mainLayout.Controls.Add(CreateLabel("健康检查URL:"), 0, row);
            txtHealth = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 5, 0),
                PlaceholderText = "http://localhost:8080/health"
            };
            mainLayout.Controls.Add(txtHealth, 1, row);
            mainLayout.SetColumnSpan(txtHealth, 2); // 占用第1和第2列
            row++;

            // 自动重启
            mainLayout.Controls.Add(CreateLabel("自动重启:"), 0, row);
            chkAutoRestart = new CheckBox
            {
                Text = "启用",
                Margin = new Padding(0, 10, 5, 0),
                Checked = false
            };
            mainLayout.Controls.Add(chkAutoRestart, 1, row);
            // 第2列留空
            row++;

            // 最大重启次数
            mainLayout.Controls.Add(CreateLabel("最大重启次数:"), 0, row);
            numRestarts = new NumericUpDown
            {
                Margin = new Padding(0, 8, 5, 0),
                Width = 120,
                Minimum = 0,
                Maximum = 10,
                Value = 3
            };
            mainLayout.Controls.Add(numRestarts, 1, row);
            // 第2列留空
            row++;

            // 按钮行 - 跨3列
            var btnPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40
            };

            var btnOk = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Width = 100,
                Height = 30
            };
            btnOk.Click += (s, e) => SaveData();

            var btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 30,
                Left = 120
            };

            // 计算按钮位置使其居中
            btnOk.Left = (btnPanel.Width - 220) / 2;
            btnCancel.Left = btnOk.Left + 110;

            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            // 将按钮面板放在最后一行的第一列，并跨越三列
            mainLayout.SetColumnSpan(btnPanel, 3);
            mainLayout.Controls.Add(btnPanel, 0, row);

            // 注册布局事件，在窗体大小改变时重新计算按钮位置
            btnPanel.SizeChanged += (s, e) =>
            {
                btnOk.Left = (btnPanel.Width - 220) / 2;
                btnCancel.Left = btnOk.Left + 110;
            };

            this.Controls.Add(mainLayout);

            // 设置窗体接受按钮
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        // 辅助方法：创建标签
        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 10, 5, 0)
            };
        }

        private void LoadData()
        {
            txtName.Text = _config.Name;
            txtPath.Text = _config.ExePath;
            txtArgs.Text = _config.Arguments;
            txtWd.Text = _config.WorkingDirectory;
            numOrder.Value = _config.Order;
            numDelay.Value = _config.DelayAfterStart;
            txtHealth.Text = _config.HealthCheckUrl;
            chkAutoRestart.Checked = _config.AutoRestart;
            numRestarts.Value = _config.MaxRestarts;
            //txtLogDir.Text = _config.LogDirectory;
        }

        private void SaveData()
        {
            _config.Name = txtName.Text;
            _config.ExePath = txtPath.Text;
            _config.Arguments = txtArgs.Text;
            _config.WorkingDirectory = string.IsNullOrEmpty(txtWd.Text) ? Path.GetDirectoryName(txtPath.Text) : txtWd.Text;
            _config.Order = (int)numOrder.Value;
            _config.DelayAfterStart = (int)numDelay.Value;
            _config.HealthCheckUrl = txtHealth.Text;
            _config.AutoRestart = chkAutoRestart.Checked;
            _config.MaxRestarts = (int)numRestarts.Value;
            //_config.LogDirectory = txtLogDir.Text;
        }
    }
}