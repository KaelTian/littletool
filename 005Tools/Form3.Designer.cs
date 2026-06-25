namespace _005Tools
{
    partial class Form3
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
            navigationArrow1 = new NavigationArrow();
            SuspendLayout();
            // 
            // navigationArrow1
            // 
            navigationArrow1.BackColor = Color.Transparent;
            navigationArrow1.LineColor = Color.FromArgb(255, 215, 0);
            navigationArrow1.LineLength = 229;
            navigationArrow1.LineThickness = 4;
            navigationArrow1.Location = new Point(364, 343);
            navigationArrow1.MarkerFill = false;
            navigationArrow1.MarkerSize = 12;
            navigationArrow1.Name = "navigationArrow1";
            navigationArrow1.Size = new Size(245, 16);
            navigationArrow1.TabIndex = 0;
            navigationArrow1.Text = "navigationArrow1";
            // 
            // Form3
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1058, 527);
            Controls.Add(navigationArrow1);
            Name = "Form3";
            Text = "Form3";
            ResumeLayout(false);
        }

        #endregion

        private NavigationArrow navigationArrow1;
    }
}