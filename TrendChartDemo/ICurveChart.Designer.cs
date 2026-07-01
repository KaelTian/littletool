using System.Windows.Forms;
using System.Xml.Linq;

namespace TrendChartDemo
{
    partial class ICurveChart
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
            if (!_disposed)
            {
                if (disposing)
                {
                    if (components != null)
                        components.Dispose();

                    if (_dataTimer != null)
                    {
                        _dataTimer.Stop();
                        _dataTimer.Dispose();
                    }
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ICurveChart));
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            groupBox2 = new GroupBox();
            z2TrendChart = new TrendChart();
            groupBox1 = new GroupBox();
            z1TrendChart = new TrendChart();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Location = new Point(1737, 680);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(129, 248);
            tableLayoutPanel1.TabIndex = 6;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel2.Controls.Add(groupBox2, 0, 1);
            tableLayoutPanel2.Controls.Add(groupBox1, 0, 0);
            tableLayoutPanel2.Location = new Point(5, 46);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(1718, 900);
            tableLayoutPanel2.TabIndex = 7;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(z2TrendChart);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("Microsoft YaHei UI", 12F);
            groupBox2.Location = new Point(3, 453);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1712, 444);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Z2";
            // 
            // z2TrendChart
            // 
            z2TrendChart.BackColor = Color.Silver;
            z2TrendChart.Dock = DockStyle.Fill;
            z2TrendChart.Location = new Point(3, 24);
            z2TrendChart.Name = "z2TrendChart";
            z2TrendChart.Size = new Size(1706, 417);
            z2TrendChart.TabIndex = 1;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(z1TrendChart);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Microsoft YaHei UI", 12F);
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(1712, 444);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Z1";
            // 
            // z1TrendChart
            // 
            z1TrendChart.BackColor = Color.Silver;
            z1TrendChart.Dock = DockStyle.Fill;
            z1TrendChart.Location = new Point(3, 24);
            z1TrendChart.Name = "z1TrendChart";
            z1TrendChart.Size = new Size(1706, 417);
            z1TrendChart.TabIndex = 0;
            // 
            // ICurveChart
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            BackColor = Color.Silver;
            Controls.Add(tableLayoutPanel2);
            Controls.Add(tableLayoutPanel1);
            Name = "ICurveChart";
            Size = new Size(1896, 948);
            Controls.SetChildIndex(tableLayoutPanel1, 0);
            Controls.SetChildIndex(tableLayoutPanel2, 0);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private GroupBox groupBox2;
        private GroupBox groupBox1;
        private TrendChart z1TrendChart;
        private TrendChart z2TrendChart;
    }
}
