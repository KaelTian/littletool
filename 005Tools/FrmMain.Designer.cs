namespace _005Tools
{
    partial class FrmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button3d1 = new Button3D();
            button3d2 = new Button3D();
            textBox1 = new TextBox();
            SuspendLayout();
            // 
            // button3d1
            // 
            button3d1.BackColor = Color.FromArgb(128, 255, 128);
            button3d1.BevelWidth = 5;
            button3d1.FlatAppearance.BorderSize = 0;
            button3d1.FlatStyle = FlatStyle.Flat;
            button3d1.Font = new Font("Microsoft YaHei", 9F);
            button3d1.ForeColor = Color.Black;
            button3d1.Image = Properties.Resources.icons8_mongrol_50;
            button3d1.LayoutMode = Button3DLayout.IconRightTextLeft;
            button3d1.Location = new Point(610, 60);
            button3d1.Name = "button3d1";
            button3d1.Size = new Size(144, 67);
            button3d1.TabIndex = 0;
            button3d1.Text = "3D按钮示例";
            button3d1.ToggleMode = true;
            button3d1.UseVisualStyleBackColor = false;
            button3d1.CheckedChanged += button3d1_CheckedChanged;
            button3d1.Click += button3d1_Click;
            // 
            // button3d2
            // 
            button3d2.BackColor = Color.FromArgb(240, 240, 240);
            button3d2.FlatAppearance.BorderSize = 0;
            button3d2.FlatStyle = FlatStyle.Flat;
            button3d2.Font = new Font("Microsoft YaHei", 9F);
            button3d2.ForeColor = Color.Black;
            button3d2.Location = new Point(191, 249);
            button3d2.Name = "button3d2";
            button3d2.Size = new Size(194, 122);
            button3d2.TabIndex = 1;
            button3d2.Text = "button3d2";
            button3d2.UseVisualStyleBackColor = false;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(496, 265);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(234, 27);
            textBox1.TabIndex = 2;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(textBox1);
            Controls.Add(button3d2);
            Controls.Add(button3d1);
            Name = "FrmMain";
            Text = "FrmMain";
            Load += FrmMain_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button3D button3d1;
        private Button3D button3d2;
        private TextBox textBox1;
    }
}