namespace _005Tools
{
    partial class Form2
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
            sx = new VerticalGauge();
            SuspendLayout();
            // 
            // sx
            // 
            sx.BackColor = Color.White;
            sx.Font = new Font("Microsoft YaHei", 9F);
            sx.Location = new Point(858, 87);
            sx.Name = "sx";
            sx.ScaleBackColor = Color.FromArgb(128, 0, 128);
            sx.Size = new Size(177, 448);
            sx.TabIndex = 1;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1166, 658);
            Controls.Add(sx);
            Name = "Form2";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private VerticalGauge sx;
    }
}