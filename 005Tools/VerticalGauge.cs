using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace _005Tools
{
    [Description("垂直阈值仪表，左侧紫色刻度区，右侧黄/红分区")]
    public class VerticalGauge : UserControl
    {
        private float _minValue = 0f;
        private float _maxValue = 20f;
        private float _warnValue = 10f;
        private float _errorValue = 15f;
        private string _unit = "A";
        private int _decimalPlaces = 0;

        private Color _scaleBackColor = Color.FromArgb(128, 0, 128);   // 左侧刻度区：紫
        private Color _scaleTextColor = Color.White;                   // 刻度文字：白
        private Color _warnZoneColor = Color.FromArgb(255, 235, 59); // Min~Warn：亮黄
        private Color _errorLineColor = Color.FromArgb(244, 67, 54);   // Error 红线：红
        private Color _gaugeBorderColor = Color.FromArgb(180, 180, 180);

        public VerticalGauge()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Size = new Size(100, 320);
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9f);
        }

        [Category("数据"), DefaultValue(0f)]
        public float MinValue { get => _minValue; set { _minValue = value; Invalidate(); } }

        [Category("数据"), DefaultValue(20f)]
        public float MaxValue { get => _maxValue; set { _maxValue = value; Invalidate(); } }

        [Category("数据"), DefaultValue(10f)]
        public float WarnValue { get => _warnValue; set { _warnValue = value; Invalidate(); } }

        [Category("数据"), DefaultValue(15f)]
        public float ErrorValue { get => _errorValue; set { _errorValue = value; Invalidate(); } }

        [Category("外观"), DefaultValue("A")]
        public string Unit { get => _unit; set { _unit = value; Invalidate(); } }

        [Category("外观"), DefaultValue(0)]
        public int DecimalPlaces { get => _decimalPlaces; set { _decimalPlaces = value; Invalidate(); } }

        [Category("外观"), Description("左侧刻度区背景色")]
        public Color ScaleBackColor { get => _scaleBackColor; set { _scaleBackColor = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle cli = ClientRectangle;
            if (cli.Width < 50 || cli.Height < 80) return;

            // ── 改动点 2：顶部/底部边距改为 0，消除 Max 值上方的白色间隙 ──
            float padTop = 0, padBottom = 0;
            float scaleW = 46f;

            // 左侧紫色刻度区
            RectangleF scaleRect = new RectangleF(0, padTop, scaleW, cli.Height - padTop - padBottom);
            // 右侧仪表区
            RectangleF gaugeRect = new RectangleF(scaleW + 2, padTop, cli.Width - scaleW - 4, cli.Height - padTop - padBottom);

            DrawScale(g, scaleRect, gaugeRect);
            DrawGauge(g, gaugeRect);
        }

        private void DrawScale(Graphics g, RectangleF scaleRect, RectangleF gaugeRect)
        {
            // 1. 左侧整体紫色背景
            using (SolidBrush purple = new SolidBrush(_scaleBackColor))
                g.FillRectangle(purple, scaleRect);

            // 2. 收集刻度：Min、Max、Mid，外加 Warn、Error（去重）
            var vals = new System.Collections.Generic.List<float> { _minValue, _maxValue };
            float mid = (_minValue + _maxValue) / 2f;
            if (Math.Abs(mid - _minValue) > 0.001f && Math.Abs(mid - _maxValue) > 0.001f)
                vals.Add(mid);

            if (_warnValue > _minValue && _warnValue < _maxValue && !vals.Any(v => Math.Abs(v - _warnValue) < 0.001f))
                vals.Add(_warnValue);
            if (_errorValue > _minValue && _errorValue < _maxValue && !vals.Any(v => Math.Abs(v - _errorValue) < 0.001f))
                vals.Add(_errorValue);
            vals.Sort();

            // 轴线（靠右边缘，半透明白）
            float axisX = scaleRect.Right - 2;
            using (Pen axisPen = new Pen(Color.FromArgb(180, 255, 255, 255), 1f))
                g.DrawLine(axisPen, axisX, scaleRect.Top + 2, axisX, scaleRect.Bottom - 2);

            using (Font font = new Font(Font.FontFamily, 8f))
            using (SolidBrush txtBrush = new SolidBrush(_scaleTextColor))
            {
                foreach (float v in vals)
                {
                    float y = ValueToY(v, gaugeRect);
                    bool isMajor = Math.Abs(v - _minValue) < 0.001f || Math.Abs(v - _maxValue) < 0.001f;
                    float tickLen = isMajor ? 8 : 5;

                    // Warn刻度线黄色，Error刻度线红色，其余白色
                    Color tickColor = _scaleTextColor;
                    if (Math.Abs(v - _warnValue) < 0.001f) tickColor = _warnZoneColor;
                    else if (Math.Abs(v - _errorValue) < 0.001f) tickColor = _errorLineColor;

                    using (Pen p = new Pen(tickColor, 1.5f))
                        g.DrawLine(p, axisX - tickLen, y, axisX, y);

                    // 文字
                    string fmt = _decimalPlaces > 0 ? $"F{_decimalPlaces}" : "F0";
                    string txt = v.ToString(fmt);
                    if (isMajor && Math.Abs(v - _maxValue) < 0.001f && !string.IsNullOrEmpty(_unit))
                        txt += $" [{_unit}]";

                    SizeF sz = g.MeasureString(txt, font);
                    float tx = axisX - tickLen - 4 - sz.Width;
                    float ty = y - sz.Height / 2f;

                    // ── 同步放宽文字限制，允许贴顶/贴底，彻底消除留白 ──
                    ty = Math.Max(scaleRect.Top, Math.Min(scaleRect.Bottom - sz.Height, ty));
                    tx = Math.Max(scaleRect.Left, tx);

                    g.DrawString(txt, font, txtBrush, tx, ty);
                }
            }
        }

        private void DrawGauge(Graphics g, RectangleF rect)
        {
            // 背景槽
            using (SolidBrush bg = new SolidBrush(Color.White))
            using (Pen border = new Pen(_gaugeBorderColor, 1f))
            {
                g.FillRectangle(bg, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }

            // Min ~ Warn 亮黄区域
            if (_warnValue > _minValue)
            {
                float yWarn = ValueToY(_warnValue, rect);
                float yMin = ValueToY(_minValue, rect);
                var zone = new RectangleF(rect.X + 1, yWarn, rect.Width - 2, yMin - yWarn);
                using (var b = new SolidBrush(_warnZoneColor))
                    g.FillRectangle(b, zone);
            }

            // ── 改动点 1：删掉 Error ~ Max 的淡红背景填充，不再显示粉色区域 ──
            // （原代码已移除）

            // Warn 细黄线（贯穿左右）
            if (_warnValue > _minValue && _warnValue < _maxValue)
            {
                float y = ValueToY(_warnValue, rect);
                using (var pen = new Pen(Color.FromArgb(255, 160, 0), 2f))
                    g.DrawLine(pen, rect.X - 3, y, rect.Right + 2, y);
            }

            // Error 粗红线（4px，长出边缘，最显眼）
            if (_errorValue >= _minValue && _errorValue <= _maxValue)
            {
                float y = ValueToY(_errorValue, rect);
                using (var pen = new Pen(_errorLineColor, 4f))
                    g.DrawLine(pen, rect.X - 6, y, rect.Right + 5, y);

                // 底部阴影，增加立体感
                using (var pen2 = new Pen(Color.FromArgb(80, _errorLineColor), 1f))
                    g.DrawLine(pen2, rect.X - 6, y + 2, rect.Right + 5, y + 2);
            }
        }

        private float ValueToY(float value, RectangleF rect)
        {
            float ratio = (value - _minValue) / (_maxValue - _minValue);
            ratio = Math.Max(0f, Math.Min(1f, ratio));
            return rect.Bottom - ratio * rect.Height;
        }
    }
}
