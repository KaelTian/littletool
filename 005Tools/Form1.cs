namespace _005Tools
{
    public partial class Form1 : Form
    {
        private VerticalGauge gauge;
        private TextBox txtMin, txtMax, txtWarn, txtErr;

        public Form1()
        {
            Text = "VerticalGauge 阈值配置";
            Size = new Size(400, 480);
            StartPosition = FormStartPosition.CenterScreen;

            gauge = new VerticalGauge
            {
                Location = new Point(40, 30),
                Size = new Size(100, 340),
                MinValue = 0,
                MaxValue = 20,
                WarnValue = 10,
                ErrorValue = 15,
                Unit = "A",
                // 保留小数点 正整数就是保留位数，但如果是整数则不显示小数点，-1就是自己去进行判断
                // 默认- 1
                DecimalPlaces = -1
            };
            Controls.Add(gauge);

            int x = 200, y = 50, dy = 55;
            AddLabel("MinValue", x, y); txtMin = AddTextBox("0", x + 90, y); y += dy;
            AddLabel("MaxValue", x, y); txtMax = AddTextBox("20", x + 90, y); y += dy;
            AddLabel("WarnValue", x, y); txtWarn = AddTextBox("10", x + 90, y); y += dy;
            AddLabel("ErrorValue", x, y); txtErr = AddTextBox("15", x + 90, y); y += dy + 15;

            var btn = new Button
            {
                Text = "应用阈值",
                Location = new Point(x, y),
                Width = 180,
                Height = 36,
                Font = new Font("微软雅黑", 10f)
            };
            btn.Click += (s, e) => Apply();
            Controls.Add(btn);

            Apply();
        }

        private void Apply()
        {
            // 先全部更新数值，最后统一强制重绘，避免异步 Paint 抓到中间态
            gauge.MinValue = Parse(txtMin.Text);
            gauge.MaxValue = Parse(txtMax.Text);
            gauge.WarnValue = Parse(txtWarn.Text);
            gauge.ErrorValue = Parse(txtErr.Text);

            // ── 关键：强制立即同步重绘，不要等消息队列 ──
            gauge.Refresh();
        }

        // ── 关键：用 InvariantCulture 确保 "." 小数点稳定解析 ──
        private float Parse(string s)
        {
            return float.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float v) ? v : 0f;
        }


        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, Location = new Point(x, y + 4), AutoSize = true });
        }

        private TextBox AddTextBox(string text, int x, int y)
        {
            var t = new TextBox { Text = text, Location = new Point(x, y), Width = 80, Font = new Font("Consolas", 10f) };
            Controls.Add(t);
            return t;
        }
    }
}

