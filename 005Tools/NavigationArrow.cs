namespace _005Tools
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// 导航指示箭头控件
    /// 效果：一条横线，末端带标记（圆圈或方块），用于连接/指向目标控件
    /// </summary>
    public class NavigationArrow : Control
    {
        public NavigationArrow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.UserPaint, true);

            BackColor = Color.Transparent;
            Size = new Size(120, 20);

            _lineColor = Color.FromArgb(255, 215, 0);
            _lineLength = 100;
            _markerSize = 8;
            _lineThickness = 2;
            _markerShape = MarkerShape.Square;
            _markerFill = true;          // 默认实心
            _markerBorderThickness = 2;    // 空心时边框粗细
        }

        #region 属性

        private int _lineLength;
        [Category("外观"), Description("横线长度（像素）")]
        [DefaultValue(100)]
        public int LineLength
        {
            get => _lineLength;
            set { _lineLength = Math.Max(0, value); UpdateSize(); Invalidate(); }
        }

        private int _markerSize;
        [Category("外观"), Description("末端标记大小（像素）")]
        [DefaultValue(8)]
        public int MarkerSize
        {
            get => _markerSize;
            set { _markerSize = Math.Max(2, value); UpdateSize(); Invalidate(); }
        }

        private int _lineThickness;
        [Category("外观"), Description("线条粗细")]
        [DefaultValue(2)]
        public int LineThickness
        {
            get => _lineThickness;
            set { _lineThickness = Math.Max(1, value); Invalidate(); }
        }

        private Color _lineColor;
        [Category("外观"), Description("线条和标记的颜色")]
        [DefaultValue(typeof(Color), "255, 215, 0")]
        public Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; Invalidate(); }
        }

        private MarkerShape _markerShape;
        [Category("外观"), Description("末端标记形状：Circle=圆圈, Square=方块")]
        [DefaultValue(MarkerShape.Square)]
        public MarkerShape MarkerShape
        {
            get => _markerShape;
            set { _markerShape = value; Invalidate(); }
        }

        private ArrowDirection _direction;
        [Category("外观"), Description("箭头方向：Left=标记在左, Right=标记在右")]
        [DefaultValue(ArrowDirection.Right)]
        public ArrowDirection Direction
        {
            get => _direction;
            set { _direction = value; Invalidate(); }
        }

        private bool _markerFill;
        [Category("外观"), Description("标记是否填充（true=实心，false=空心）")]
        [DefaultValue(true)]
        public bool MarkerFill
        {
            get => _markerFill;
            set { _markerFill = value; Invalidate(); }
        }

        private int _markerBorderThickness;
        [Category("外观"), Description("空心标记的边框粗细")]
        [DefaultValue(2)]
        public int MarkerBorderThickness
        {
            get => _markerBorderThickness;
            set { _markerBorderThickness = Math.Max(1, value); Invalidate(); }
        }

        #endregion

        private void UpdateSize()
        {
            int width = _lineLength + _markerSize + 4;
            int height = Math.Max(_markerSize, _lineThickness) + 4;
            if (Size.Width != width || Size.Height != height)
                Size = new Size(width, height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int centerY = Height / 2;
            int halfMarker = _markerSize / 2;
            int markerCenterX;

            using (var pen = new Pen(_lineColor, _lineThickness))
            {
                if (_direction == ArrowDirection.Right)
                {
                    int lineStart = 0;
                    int lineEnd = Width - _markerSize - 2;
                    markerCenterX = Width - halfMarker - 2;
                    e.Graphics.DrawLine(pen, lineStart, centerY, lineEnd, centerY);
                }
                else
                {
                    int lineStart = _markerSize + 2;
                    int lineEnd = Width;
                    markerCenterX = halfMarker + 2;
                    e.Graphics.DrawLine(pen, lineStart, centerY, lineEnd, centerY);
                }

                // 标记区域
                Rectangle markerRect = new Rectangle(
                    markerCenterX - halfMarker,
                    centerY - halfMarker,
                    _markerSize,
                    _markerSize);

                // 空心时稍微内缩，确保边框完整显示不被裁切
                if (!_markerFill)
                {
                    int inset = _markerBorderThickness / 2;
                    markerRect.Inflate(-inset, -inset);
                }

                if (_markerShape == MarkerShape.Circle)
                {
                    if (_markerFill)
                    {
                        using (var brush = new SolidBrush(_lineColor))
                            e.Graphics.FillEllipse(brush, markerRect);
                    }
                    else
                    {
                        using (var borderPen = new Pen(_lineColor, _markerBorderThickness))
                            e.Graphics.DrawEllipse(borderPen, markerRect);
                    }
                }
                else // Square
                {
                    if (_markerFill)
                    {
                        using (var brush = new SolidBrush(_lineColor))
                            e.Graphics.FillRectangle(brush, markerRect);
                    }
                    else
                    {
                        using (var borderPen = new Pen(_lineColor, _markerBorderThickness))
                            e.Graphics.DrawRectangle(borderPen, markerRect);
                    }
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _lineLength = Math.Max(0, Width - _markerSize - 4);
        }
    }

    public enum MarkerShape
    {
        Circle,
        Square
    }

    public enum ArrowDirection
    {
        Left,
        Right
    }
}
