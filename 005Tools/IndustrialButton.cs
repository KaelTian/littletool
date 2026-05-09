using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace _005Tools
{
    public enum IconPosition
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public class IndustrialButton : Control
    {
        private bool _pressed;
        private bool _hover;

        private Image _icon;

        [Category("Appearance")]
        public Image Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        public IconPosition IconPosition { get; set; } = IconPosition.Top;

        [Category("Appearance")]
        public Color ButtonColor { get; set; } = Color.Gold;

        [Category("Appearance")]
        public Color HoverColor { get; set; } = Color.Khaki;

        public IndustrialButton()
        {
            DoubleBuffered = true;

            Size = new Size(120, 70);

            Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            ForeColor = Color.Black;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _pressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();

            OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;

            Color baseColor = _hover ? HoverColor : ButtonColor;

            using (SolidBrush brush = new SolidBrush(baseColor))
            {
                g.FillRectangle(brush, rect);
            }

            Draw3DBorder(g, rect);

            DrawContent(g, rect);
        }

        private void Draw3DBorder(Graphics g, Rectangle rect)
        {
            Color light = Color.White;
            Color dark = Color.FromArgb(80, 80, 80);

            if (_pressed)
            {
                // 凹陷效果
                using (Pen p = new Pen(dark, 2))
                {
                    g.DrawLine(p, 0, 0, rect.Width - 1, 0);
                    g.DrawLine(p, 0, 0, 0, rect.Height - 1);
                }

                using (Pen p = new Pen(light, 2))
                {
                    g.DrawLine(p,
                        rect.Width - 1,
                        0,
                        rect.Width - 1,
                        rect.Height - 1);

                    g.DrawLine(p,
                        0,
                        rect.Height - 1,
                        rect.Width - 1,
                        rect.Height - 1);
                }
            }
            else
            {
                // 凸起效果
                using (Pen p = new Pen(light, 2))
                {
                    g.DrawLine(p, 0, 0, rect.Width - 1, 0);
                    g.DrawLine(p, 0, 0, 0, rect.Height - 1);
                }

                using (Pen p = new Pen(dark, 2))
                {
                    g.DrawLine(p,
                        rect.Width - 1,
                        0,
                        rect.Width - 1,
                        rect.Height - 1);

                    g.DrawLine(p,
                        0,
                        rect.Height - 1,
                        rect.Width - 1,
                        rect.Height - 1);
                }
            }
        }

        private void DrawContent(Graphics g, Rectangle rect)
        {
            int offset = _pressed ? 2 : 0;

            SizeF textSize = g.MeasureString(Text, Font);

            int spacing = 4;

            Rectangle iconRect = Rectangle.Empty;
            Rectangle textRect = Rectangle.Empty;

            int iconSize = 24;

            switch (IconPosition)
            {
                case IconPosition.Top:

                    iconRect = new Rectangle(
                        (rect.Width - iconSize) / 2,
                        8 + offset,
                        iconSize,
                        iconSize);

                    textRect = new Rectangle(
                        0,
                        iconRect.Bottom + spacing,
                        rect.Width,
                        24);

                    break;

                case IconPosition.Bottom:

                    textRect = new Rectangle(
                        0,
                        8 + offset,
                        rect.Width,
                        24);

                    iconRect = new Rectangle(
                        (rect.Width - iconSize) / 2,
                        textRect.Bottom + spacing,
                        iconSize,
                        iconSize);

                    break;

                case IconPosition.Left:

                    iconRect = new Rectangle(
                        10 + offset,
                        (rect.Height - iconSize) / 2,
                        iconSize,
                        iconSize);

                    textRect = new Rectangle(
                        iconRect.Right + spacing,
                        0,
                        rect.Width - iconRect.Right,
                        rect.Height);

                    break;

                case IconPosition.Right:

                    textRect = new Rectangle(
                        0,
                        0,
                        rect.Width - iconSize - 10,
                        rect.Height);

                    iconRect = new Rectangle(
                        rect.Width - iconSize - 10 + offset,
                        (rect.Height - iconSize) / 2,
                        iconSize,
                        iconSize);

                    break;
            }

            if (Icon != null)
            {
                g.DrawImage(Icon, iconRect);
            }

            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (SolidBrush brush =
                       new SolidBrush(ForeColor))
                {
                    g.DrawString(
                        Text,
                        Font,
                        brush,
                        textRect,
                        sf);
                }
            }
        }
    }
}