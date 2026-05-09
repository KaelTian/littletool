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
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button3d1);
            Name = "FrmMain";
            Text = "FrmMain";
            Load += FrmMain_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button3D button3d1;
    }
}