namespace _005Tools
{
    public partial class Form2 : Form
    {
        private VerticalGaugeOri gauge;
        private TextBox txtMin, txtMax, txtWarn, txtErr, txtCurr;
        private TrackBar trackBar;
        private System.Windows.Forms.Timer simTimer;
        private Random rand = new Random();

        public Form2()
        {
            Text = "VerticalGauge 测试";
            Size = new Size(500, 450);
            StartPosition = FormStartPosition.CenterScreen;

            // ---------- 仪表控件 ----------
            gauge = new VerticalGaugeOri
            {
                Location = new Point(40, 30),
                Size = new Size(100, 320),
                MinValue = 0,
                MaxValue = 20,
                WarnValue = 12,
                ErrorValue = 18,
                Unit = "A",
                CurrentValue = 10,
                DecimalPlaces = 0
            };
            Controls.Add(gauge);

            // ---------- 右侧输入区 ----------
            int x = 200, y = 30, dy = 35;
            AddLabel("MinValue", x, y); txtMin = AddTextBox("0", x + 90, y); y += dy;
            AddLabel("MaxValue", x, y); txtMax = AddTextBox("20", x + 90, y); y += dy;
            AddLabel("WarnValue", x, y); txtWarn = AddTextBox("12", x + 90, y); y += dy;
            AddLabel("ErrorValue", x, y); txtErr = AddTextBox("18", x + 90, y); y += dy;
            AddLabel("CurrentValue", x, y); txtCurr = AddTextBox("10", x + 90, y); y += dy;

            // 应用按钮
            var btnApply = new Button { Text = "应用阈值", Location = new Point(x, y), Width = 180 };
            btnApply.Click += (s, e) => ApplySettings();
            Controls.Add(btnApply);
            y += dy + 10;

            // TrackBar 拖动实时看效果
            AddLabel("拖动调节 CurrentValue", x, y);
            y += 25;
            trackBar = new TrackBar
            {
                Location = new Point(x, y),
                Width = 180,
                Minimum = 0,
                Maximum = 200, // 放大 10 倍，内部除 10
                Value = 100,
                TickFrequency = 20
            };
            trackBar.Scroll += (s, e) =>
            {
                gauge.CurrentValue = trackBar.Value / 10f;
                txtCurr.Text = gauge.CurrentValue.ToString("F1");
            };
            Controls.Add(trackBar);
            y += 60;

            // 模拟按钮（随机波动）
            var btnSim = new Button { Text = "启动模拟", Location = new Point(x, y), Width = 180 };
            btnSim.Click += (s, e) =>
            {
                if (simTimer == null)
                {
                    simTimer = new System.Windows.Forms.Timer { Interval = 800 };
                    simTimer.Tick += (ss, ee) =>
                    {
                        float v = rand.Next(0, 220) / 10f; // 0~22
                        gauge.CurrentValue = v;
                        txtCurr.Text = v.ToString("F1");
                        trackBar.Value = Math.Min(trackBar.Maximum, (int)(v * 10));
                    };
                }
                simTimer.Enabled = !simTimer.Enabled;
                btnSim.Text = simTimer.Enabled ? "停止模拟" : "启动模拟";
            };
            Controls.Add(btnSim);

            // 绑定文本框回车
            txtCurr.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) gauge.CurrentValue = Parse(txtCurr.Text); };
        }

        private void ApplySettings()
        {
            gauge.MinValue = Parse(txtMin.Text);
            gauge.MaxValue = Parse(txtMax.Text);
            gauge.WarnValue = Parse(txtWarn.Text);
            gauge.ErrorValue = Parse(txtErr.Text);
            gauge.CurrentValue = Parse(txtCurr.Text);

            // 同步 TrackBar 范围
            trackBar.Minimum = (int)(gauge.MinValue * 10);
            trackBar.Maximum = (int)(gauge.MaxValue * 10);
            trackBar.Value = Math.Min(trackBar.Maximum, (int)(gauge.CurrentValue * 10));
        }

        private float Parse(string s) => float.TryParse(s, out float v) ? v : 0f;

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true });
        }
        private TextBox AddTextBox(string text, int x, int y)
        {
            var t = new TextBox { Text = text, Location = new Point(x, y), Width = 80 };
            Controls.Add(t);
            return t;
        }
    }
}
