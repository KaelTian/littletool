using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace _005Tools
{
    [DefaultEvent("ValueChanged")]
    [Description("垂直仪表/温度计控件，支持 Min/Max/Warn/Error 四段阈值")]
    public class VerticalGaugeOri : UserControl
    {
        #region 字段
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private float _warnValue = 70f;
        private float _errorValue = 90f;
        private float _currentValue = 0f;
        private string _unit = "";
        private int _decimalPlaces = 0;

        private Color _normalColor = Color.FromArgb(76, 175, 80);   // 正常：绿
        private Color _warnColor = Color.FromArgb(255, 193, 7);   // 警告：琥珀黄
        private Color _errorColor = Color.FromArgb(244, 67, 54);   // 报警：红
        private Color _errorLineColor = Color.FromArgb(244, 67, 54); // 红线
        private Color _warnZoneBack = Color.FromArgb(50, 255, 235, 59); // Warn~Error 之间很淡的黄底
        private Color _scaleColor = Color.FromArgb(120, 120, 120);
        private Color _borderColor = Color.FromArgb(180, 180, 180);

        private float _scaleWidth = 44f;   // 左侧刻度区宽度
        private int _majorTicks = 5;     // 主刻度数（含 Min/Max）
        #endregion

        #region 事件
        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
        #endregion

        #region 属性（设计器可见）

        [Category("数据"), Description("量程下限（刻度底部）"), DefaultValue(0f)]
        public float MinValue
        {
            get => _minValue;
            set { _minValue = value; Invalidate(); }
        }

        [Category("数据"), Description("量程上限（刻度顶部）"), DefaultValue(100f)]
        public float MaxValue
        {
            get => _maxValue;
            set { _maxValue = value; Invalidate(); }
        }

        [Category("数据"), Description("Warning 阈值，超过后条带变黄"), DefaultValue(70f)]
        public float WarnValue
        {
            get => _warnValue;
            set { _warnValue = value; Invalidate(); }
        }

        [Category("数据"), Description("Error 阈值，超过后条带变红，并绘制红线"), DefaultValue(90f)]
        public float ErrorValue
        {
            get => _errorValue;
            set { _errorValue = value; Invalidate(); }
        }

        [Category("数据"), Description("当前值（决定填充高度与颜色）"), DefaultValue(0f)]
        public float CurrentValue
        {
            get => _currentValue;
            set
            {
                float v = Math.Max(_minValue, Math.Min(_maxValue, value));
                if (Math.Abs(v - _currentValue) > 0.0001f)
                {
                    _currentValue = v;
                    Invalidate();
                    OnValueChanged();
                }
            }
        }

        [Category("外观"), Description("单位，显示在刻度顶部，如 A / °C / %"), DefaultValue("")]
        public string Unit
        {
            get => _unit;
            set { _unit = value; Invalidate(); }
        }

        [Category("外观"), Description("刻度小数位"), DefaultValue(0)]
        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set { _decimalPlaces = value; Invalidate(); }
        }

        [Category("外观"), Description("正常状态条带颜色"), DefaultValue(typeof(Color), "76, 175, 80")]
        public Color NormalColor { get => _normalColor; set { _normalColor = value; Invalidate(); } }

        [Category("外观"), Description("Warning 状态条带颜色"), DefaultValue(typeof(Color), "255, 193, 7")]
        public Color WarnColor { get => _warnColor; set { _warnColor = value; Invalidate(); } }

        [Category("外观"), Description("Error 状态条带颜色"), DefaultValue(typeof(Color), "244, 67, 54")]
        public Color ErrorColor { get => _errorColor; set { _errorColor = value; Invalidate(); } }

        [Category("外观"), Description("Error 红线颜色"), DefaultValue(typeof(Color), "244, 67, 54")]
        public Color ErrorLineColor { get => _errorLineColor; set { _errorLineColor = value; Invalidate(); } }
        #endregion

        public VerticalGaugeOri()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Size = new Size(90, 280);
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle cli = ClientRectangle;
            if (cli.Width < 60 || cli.Height < 60) return;

            // 布局：左侧刻度 + 右侧仪表
            float padTop = 18; // 顶部留空给单位
            RectangleF scaleRect = new RectangleF(2, padTop, _scaleWidth, cli.Height - padTop - 4);
            RectangleF gaugeRect = new RectangleF(
                scaleRect.Right + 2,
                padTop,
                cli.Width - scaleRect.Width - 6,
                scaleRect.Height);

            DrawScale(g, cli, scaleRect, gaugeRect);  // ← 把 cli 传进去
            DrawGauge(g, gaugeRect);
        }

        /// <summary>
        /// 绘制左侧刻度轴
        /// </summary>
        private void DrawScale(Graphics g, Rectangle cli, RectangleF scaleRect, RectangleF gaugeRect)
        {
            using (Pen pen = new Pen(_scaleColor, 1f))
            using (Font font = new Font(Font.FontFamily, 8f))
            using (SolidBrush brush = new SolidBrush(_scaleColor))
            {
                float axisX = gaugeRect.Left - 1;
                g.DrawLine(pen, axisX, gaugeRect.Top, axisX, gaugeRect.Bottom);

                // 自动计算需要显示的刻度值（Min、Max、Warn、Error + 均分）
                var vals = new System.Collections.Generic.List<float>();
                vals.Add(_minValue);
                vals.Add(_maxValue);

                if (_warnValue > _minValue && _warnValue < _maxValue) vals.Add(_warnValue);
                if (_errorValue > _minValue && _errorValue < _maxValue &&
                    Math.Abs(_errorValue - _warnValue) > (_maxValue - _minValue) * 0.05f)
                    vals.Add(_errorValue);

                // 均分刻度（约每 45px 一个）
                int seg = Math.Max(2, (int)(gaugeRect.Height / 45f));
                for (int i = 1; i < seg; i++)
                {
                    float v = _minValue + (_maxValue - _minValue) * i / seg;
                    bool dup = false;
                    foreach (var ex in vals)
                    {
                        if (Math.Abs(v - ex) < (_maxValue - _minValue) * 0.06f) { dup = true; break; }
                    }
                    if (!dup) vals.Add(v);
                }
                vals.Sort();

                foreach (float v in vals)
                {
                    float y = ValueToY(v, gaugeRect);
                    bool isMajor = (v == _minValue || v == _maxValue);
                    float tickLen = isMajor ? 6 : 4;

                    g.DrawLine(pen, axisX - tickLen, y, axisX, y);

                    string fmt = _decimalPlaces > 0 ? $"F{_decimalPlaces}" : "F0";
                    string txt = v.ToString(fmt);
                    if (isMajor && v == _maxValue && !string.IsNullOrEmpty(_unit))
                        txt += $" [{_unit}]";

                    SizeF sz = g.MeasureString(txt, font);
                    float tx = axisX - tickLen - 2 - sz.Width;
                    float ty = y - sz.Height / 2f;
                    // 用 cli.Height 限制，确保文字不画出控件底边
                    ty = Math.Max(2, Math.Min(cli.Height - sz.Height - 2, ty));

                    g.DrawString(txt, font, brush, tx, ty);
                }
            }
        }

        /// <summary>
        /// 绘制右侧仪表主体
        /// </summary>
        private void DrawGauge(Graphics g, RectangleF rect)
        {
            // 1. 背景槽
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(245, 245, 245)))
            using (Pen border = new Pen(_borderColor, 1f))
            {
                g.FillRectangle(bg, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }

            // 2. Warning Zone 淡黄背景（Warn ~ Error 区间）
            if (_warnValue < _errorValue)
            {
                float yWarn = ValueToY(_warnValue, rect);
                float yErr = ValueToY(_errorValue, rect);
                if (Math.Abs(yWarn - yErr) > 1f)
                {
                    var zone = new RectangleF(rect.X + 1, yErr, rect.Width - 2, yWarn - yErr);
                    using (var b = new SolidBrush(_warnZoneBack))
                        g.FillRectangle(b, zone);
                }
            }

            // 3. Error 红线（稍微长出两侧，像截图那样）
            if (_errorValue >= _minValue && _errorValue <= _maxValue)
            {
                float y = ValueToY(_errorValue, rect);
                using (var pen = new Pen(_errorLineColor, 2.5f))
                    g.DrawLine(pen, rect.X - 5, y, rect.Right + 4, y);

                using (var pen2 = new Pen(Color.FromArgb(120, _errorLineColor), 1f))
                    g.DrawLine(pen2, rect.X - 5, y + 1, rect.Right + 4, y + 1);
            }

            // 4. 当前值条带
            if (_currentValue > _minValue)
            {
                float yCurr = ValueToY(_currentValue, rect);
                var bar = new RectangleF(rect.X + 3, yCurr, rect.Width - 6, rect.Bottom - yCurr);
                if (bar.Height > 0)
                {
                    Color c = GetBarColor();
                    // 纵向渐变：顶部微亮，底部微暗，增加一点立体感
                    using (var lgb = new LinearGradientBrush(
                        bar,
                        ControlPaint.Light(c, 0.85f),
                        ControlPaint.Dark(c, 0.9f),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(lgb, bar);
                    }

                    // 顶部高光线
                    using (var hp = new Pen(Color.FromArgb(200, 255, 255, 255), 1f))
                        g.DrawLine(hp, bar.Left, bar.Top, bar.Right, bar.Top);
                }
            }
        }

        /// <summary>
        /// 数值 -> Y 坐标（底部 Min，顶部 Max）
        /// </summary>
        private float ValueToY(float value, RectangleF rect)
        {
            float ratio = (value - _minValue) / (_maxValue - _minValue);
            ratio = Math.Max(0f, Math.Min(1f, ratio));
            return rect.Bottom - ratio * rect.Height;
        }

        private Color GetBarColor()
        {
            if (_currentValue >= _errorValue) return _errorColor;
            if (_currentValue >= _warnValue) return _warnColor;
            return _normalColor;
        }
    }
}