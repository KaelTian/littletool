using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _005Tools
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();

            // 示例1：指向 Grinding 1 的指示线（匹配你图片样式）
            var arrow1 = new NavigationArrow
            {
                Location = new Point(150, 50),
                LineLength = 80,
                MarkerSize = 8,
                MarkerShape = MarkerShape.Square,
                MarkerFill = true,
                LineColor = Color.Gold
            };
            this.Controls.Add(arrow1);

            // 示例2：指向 Turntable 的指示线（标记在左侧）
            var arrow2 = new NavigationArrow
            {
                Location = new Point(150, 80),
                LineLength = 80,
                MarkerSize = 10,
                MarkerShape = MarkerShape.Circle,
                MarkerFill = false,           // 空心
                MarkerBorderThickness = 2,    // 边框粗细
                LineColor = Color.Gold
            };
            this.Controls.Add(arrow2);

            // 示例3：圆圈样式的反向指示线
            var arrow3 = new NavigationArrow
            {
                Location = new Point(150, 110),
                LineLength = 80,
                MarkerSize = 10,
                MarkerShape = MarkerShape.Square,
                MarkerFill = false,
                MarkerBorderThickness = 3,
                LineColor = Color.Orange,
                Direction = ArrowDirection.Left
            };
            this.Controls.Add(arrow3);
        }

        private void navigationArrow1_Click(object sender, EventArgs e)
        {

        }
    }
}
