namespace _005Tools
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();

            // 示例 1：文字 + 图标（左图右文）
            var btn1 = new Button3D
            {
                Text = "自动模式",
                ToggleMode = true,
                Location = new Point(20, 20),
                Size = new Size(120, 50),
                Image = Image.FromFile("icons8-mongrol-50.png"),//SystemIcons.WinLogo.ToBitmap(), // 换成你的 icon
                LayoutMode = Button3DLayout.IconRightTextLeft,
                BevelWidth = 2,
                BackColor = Color.FromArgb(220, 220, 255)
            };
            this.Controls.Add(btn1);

            // 示例 2：上图下文（模拟你截图的 Maint. 按钮）
            var btn2 = new Button3D
            {
                Text = "Maint.",
                ToggleMode = true,
                Location = new Point(150, 20),
                Size = new Size(120, 50),
                Image = Image.FromFile("icons8-mongrol-50.png"),//SystemIcons.WinLogo.ToBitmap(), // 换成你的齿轮 icon
                LayoutMode = Button3DLayout.IconTopTextBottom,
                BackColor = Color.Gold,
                BevelWidth = 4
            };
            // 联动：根据按钮状态控制其他控件
            btn2.CheckedChanged += (s, e) =>
            {
                bool isOn = btn2.Checked;

                //MessageBox.Show($"Maint. 按钮 {(isOn ? "按下" : "抬起")}");
                Console.WriteLine($"Maint. 按钮 {(isOn ? "按下" : "抬起")}");

                //// 视觉上：其他按钮跟着变灰/可用
                //somePanel.Enabled = isOn;
                //anotherButton.BackColor = isOn ? Color.LightGreen : Color.LightGray;

                //// 业务上：写入 PLC 或发送指令
                //WritePlcBit("MES_HMI.bAutoMode", isOn);
            };
            this.Controls.Add(btn2);

            // 示例 3：仅图标
            var btn3 = new Button3D
            {
                Location = new Point(20, 80),
                Size = new Size(50, 50),
                Image = Image.FromFile("icons8-mongrol-50.png"),//SystemIcons.WinLogo.ToBitmap(),
                LayoutMode = Button3DLayout.IconOnly,
                BevelWidth = 2
            };
            this.Controls.Add(btn3);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            IndustrialButton btn = new IndustrialButton();

            btn.Text = "Maint.";
            btn.Size = new Size(100, 70);

            btn.Icon = Image.FromFile("icons8-mongrol-50.png");

            btn.IconPosition = IconPosition.Right;

            btn.Location = new Point(200, 200);

            Controls.Add(btn);
        }

        private void button3d1_CheckedChanged(object sender, EventArgs e)
        {
            //var btn = sender as Button3D;

            //bool isOn = btn.Checked;

            //MessageBox.Show($"button3d1. 按钮 {(isOn ? "按下" : "抬起")}");
        }

        private void button3d1_Click(object sender, EventArgs e)
        {
            textBox1.Text += "123";
        }
    }
}
