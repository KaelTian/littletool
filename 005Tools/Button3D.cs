

using System.ComponentModel;

namespace _005Tools
{
    public enum Button3DLayout
    {
        IconLeftTextRight,
        IconRightTextLeft,
        IconTopTextBottom,
        IconBottomTextTop,
        IconOnly,
        TextOnly,
        Overlay
    }

    public class Button3D : Button
    {
        private bool _isPressed;   // 鼠标当前是否正按着（临时）
        private bool _isHovered;
        private bool _checked;     // 保持按下状态（Toggle）

        // ========== 新增：Toggle 相关 ==========

        [Browsable(true), Category("行为"), DefaultValue(false)]
        [Description("是否启用切换模式。true=点击后保持按下状态，再次点击抬起")]
        public bool ToggleMode { get; set; } = false;

        [Browsable(true), Category("行为"), DefaultValue(false)]
        [Description("当前是否处于保持按下的状态（仅 ToggleMode=true 时有效）")]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    Invalidate();                 // 立即重绘
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Browsable(true), Category("行为")]
        [Description("Checked 状态改变时触发")]
        public event EventHandler? CheckedChanged;

        // ========== 原有外观属性 ==========

        [Browsable(true), Category("外观"), DefaultValue(typeof(Button3DLayout), "IconLeftTextRight")]
        public Button3DLayout LayoutMode { get; set; } = Button3DLayout.IconLeftTextRight;

        [Browsable(true), Category("外观"), DefaultValue(4)]
        public int IconTextSpacing { get; set; } = 4;

        [Browsable(true), Category("外观"), DefaultValue(2)]
        public int BevelWidth { get; set; } = 2;

        public Button3D()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.FromArgb(240, 240, 240);
            ForeColor = Color.Black;
            Font = new Font("Microsoft YaHei", 9F);
            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer, true);
        }

        // ========== 鼠标/键盘状态 ==========

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _isPressed = true;
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _isPressed = false;
                Invalidate();
            }
            base.OnKeyUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        // ========== 关键：点击时翻转 Toggle 状态 ==========

        protected override void OnClick(EventArgs e)
        {
            if (ToggleMode)
            {
                Checked = !Checked;   // 翻转状态
            }
            base.OnClick(e);
        }

        // ========== 绘制 ==========

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = ClientRectangle;
            int bw = BevelWidth;

            // 是否显示“凹陷/按下”效果：Checked 保持 或 鼠标正按着
            bool isDepressed = Checked || _isPressed;

            // 1. 背景
            Color bg = BackColor;
            if (!Enabled) bg = Color.FromArgb(220, 220, 220);
            else if (isDepressed) bg = ControlPaint.Dark(BackColor, 0.12f);  // 按下状态略深
            else if (_isHovered) bg = ControlPaint.Light(BackColor, 0.08f);

            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, rect);

            // 2. 3D 边框（凸起 vs 凹陷）
            Draw3DBorder(g, rect, raised: !isDepressed);

            // 3. 内容区域：凹陷时整体偏移 1px
            int off = isDepressed ? 1 : 0;
            Rectangle content = new Rectangle(
                rect.X + bw + off,
                rect.Y + bw + off,
                rect.Width - bw * 2,
                rect.Height - bw * 2);

            // 4. 绘制 Icon + Text
            DrawContent(g, content);
        }

        private void Draw3DBorder(Graphics g, Rectangle rect, bool raised)
        {
            int bw = BevelWidth;
            if (bw <= 0) return;

            Color light = ControlPaint.LightLight(BackColor);
            Color dark = ControlPaint.DarkDark(BackColor);
            Color shadow = ControlPaint.Dark(BackColor, 0.3f);

            using (Pen penLight = new Pen(light),
                      penDark = new Pen(dark),
                      penShadow = new Pen(shadow))
            {
                for (int i = 0; i < bw; i++)
                {
                    Rectangle r = new Rectangle(
                        rect.X + i, rect.Y + i,
                        rect.Width - 1 - i * 2, rect.Height - 1 - i * 2);

                    if (raised)
                    {
                        // 左上亮，右下暗 → 凸起
                        g.DrawLine(penLight, r.X, r.Bottom, r.X, r.Y);
                        g.DrawLine(penLight, r.X, r.Y, r.Right, r.Y);
                        g.DrawLine(penShadow, r.Right, r.Y, r.Right, r.Bottom);
                        g.DrawLine(penShadow, r.Right, r.Bottom, r.X, r.Bottom);
                    }
                    else
                    {
                        // 左上暗，右下亮 → 凹陷/按下
                        g.DrawLine(penDark, r.X, r.Bottom, r.X, r.Y);
                        g.DrawLine(penDark, r.X, r.Y, r.Right, r.Y);
                        g.DrawLine(penLight, r.Right, r.Y, r.Right, r.Bottom);
                        g.DrawLine(penLight, r.Right, r.Bottom, r.X, r.Bottom);
                    }
                }
            }
        }

        private void DrawContent(Graphics g, Rectangle rect)
        {
            Image icon = Image;
            string text = Text;
            bool hasIcon = icon != null && LayoutMode != Button3DLayout.TextOnly;
            bool hasText = !string.IsNullOrEmpty(text) && LayoutMode != Button3DLayout.IconOnly;

            if (!hasIcon && !hasText) return;

            SizeF textSize = hasText ? g.MeasureString(text, Font) : SizeF.Empty;
            Size iconSize = hasIcon ? new Size(24, 24) : Size.Empty;
            if (hasIcon)
            {
                iconSize = new Size(
                    Math.Min(icon.Width, 24),
                    Math.Min(icon.Height, 24));
            }

            int sp = IconTextSpacing;
            int totalW = 0, totalH = 0;

            switch (LayoutMode)
            {
                case Button3DLayout.IconLeftTextRight:
                case Button3DLayout.IconRightTextLeft:
                    totalW = (hasIcon ? iconSize.Width : 0) + (hasText ? (int)textSize.Width : 0)
                           + (hasIcon && hasText ? sp : 0);
                    totalH = Math.Max(hasIcon ? iconSize.Height : 0, hasText ? (int)textSize.Height : 0);
                    break;
                case Button3DLayout.IconTopTextBottom:
                case Button3DLayout.IconBottomTextTop:
                    totalW = Math.Max(hasIcon ? iconSize.Width : 0, hasText ? (int)textSize.Width : 0);
                    totalH = (hasIcon ? iconSize.Height : 0) + (hasText ? (int)textSize.Height : 0)
                           + (hasIcon && hasText ? sp : 0);
                    break;
                case Button3DLayout.IconOnly:
                    totalW = iconSize.Width; totalH = iconSize.Height;
                    break;
                case Button3DLayout.TextOnly:
                    totalW = (int)textSize.Width; totalH = (int)textSize.Height;
                    break;
                case Button3DLayout.Overlay:
                    totalW = Math.Max(iconSize.Width, (int)textSize.Width);
                    totalH = Math.Max(iconSize.Height, (int)textSize.Height);
                    break;
            }

            int startX = rect.X + (rect.Width - totalW) / 2;
            int startY = rect.Y + (rect.Height - totalH) / 2;

            Point iconPt = Point.Empty;
            Point textPt = Point.Empty;

            switch (LayoutMode)
            {
                case Button3DLayout.IconLeftTextRight:
                    iconPt = new Point(startX, startY + (totalH - iconSize.Height) / 2);
                    textPt = new Point(startX + iconSize.Width + sp, startY + (totalH - (int)textSize.Height) / 2);
                    break;
                case Button3DLayout.IconRightTextLeft:
                    textPt = new Point(startX, startY + (totalH - (int)textSize.Height) / 2);
                    iconPt = new Point(startX + (int)textSize.Width + sp, startY + (totalH - iconSize.Height) / 2);
                    break;
                case Button3DLayout.IconTopTextBottom:
                    iconPt = new Point(startX + (totalW - iconSize.Width) / 2, startY);
                    textPt = new Point(startX + (totalW - (int)textSize.Width) / 2, startY + iconSize.Height + sp);
                    break;
                case Button3DLayout.IconBottomTextTop:
                    textPt = new Point(startX + (totalW - (int)textSize.Width) / 2, startY);
                    iconPt = new Point(startX + (totalW - iconSize.Width) / 2, startY + (int)textSize.Height + sp);
                    break;
                case Button3DLayout.IconOnly:
                    iconPt = new Point(startX, startY);
                    break;
                case Button3DLayout.TextOnly:
                    textPt = new Point(startX, startY);
                    break;
                case Button3DLayout.Overlay:
                    iconPt = new Point(rect.X + (rect.Width - iconSize.Width) / 2,
                                      rect.Y + (rect.Height - iconSize.Height) / 2);
                    textPt = new Point(rect.X + (rect.Width - (int)textSize.Width) / 2,
                                      rect.Y + (rect.Height - (int)textSize.Height) / 2);
                    break;
            }

            // 图标
            if (hasIcon)
            {
                Rectangle iconRect = new Rectangle(iconPt, iconSize);
                if (!Enabled)
                    ControlPaint.DrawImageDisabled(g, icon, iconRect.X, iconRect.Y, BackColor);
                else
                    g.DrawImage(icon, iconRect);
            }

            // 文字
            if (hasText)
            {
                using (Brush brush = new SolidBrush(Enabled ? ForeColor : Color.Gray))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                    if (LayoutMode != Button3DLayout.Overlay)
                    {
                        g.DrawString(text, Font, brush,
                            new Rectangle(textPt, Size.Ceiling(textSize)), sf);
                    }
                    else
                    {
                        g.DrawString(text, Font, brush, rect, sf);
                    }
                }
            }
        }
    }
}
