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
                DecimalPlaces = 0
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
            gauge.MinValue = Parse(txtMin.Text);
            gauge.MaxValue = Parse(txtMax.Text);
            gauge.WarnValue = Parse(txtWarn.Text);
            gauge.ErrorValue = Parse(txtErr.Text);
        }

        private float Parse(string s) => float.TryParse(s, out float v) ? v : 0f;

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

