namespace TrendChartDemo
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            iCurveChart1 = new ICurveChart();
            SuspendLayout();
            // 
            // iCurveChart1
            // 
            iCurveChart1.Location = new Point(12, 12);
            iCurveChart1.Name = "iCurveChart1";
            iCurveChart1.Size = new Size(946, 948);
            iCurveChart1.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1904, 1161);
            Controls.Add(iCurveChart1);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            Shown += Form1_Shown;
            ResumeLayout(false);
        }

        #endregion

        private ICurveChart iCurveChart1;
    }
}
