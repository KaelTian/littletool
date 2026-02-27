using System.Drawing.Imaging;

namespace _005Tools
{
    public partial class PaintForm : Form
    {
        private GlobalInputHook _hook;
        private Bitmap _canvas;
        private Graphics _graphics;
        private bool _isDrawing = false;
        private Point _lastPoint;
        private Pen _currentPen;
        private Color _currentColor = Color.Red;
        private float _brushSize = 3f;
        private bool _restrictToClient = false;

        // 直接引用控件，避免遍历查找
        private PictureBox _picCanvas;
        private ToolStripStatusLabel _lblStatus;

        // 自动滚动偏移（让画布可以在窗口中移动查看）
        private Point _canvasOffset = Point.Empty;

        public PaintForm()
        {
            InitializeComponent();
            InitializeCanvas();
            InitializeHook();
            InitializeUI();

            // 双缓冲防闪烁
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);
        }

        private void InitializeCanvas()
        {
            // 关键修复：使用虚拟屏幕尺寸（覆盖所有显示器），确保窗外坐标能画上去
            var virtualScreen = SystemInformation.VirtualScreen;
            _canvas = new Bitmap(virtualScreen.Width, virtualScreen.Height);

            _graphics = Graphics.FromImage(_canvas);
            _graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            _graphics.Clear(Color.White);

            _currentPen = new Pen(_currentColor, _brushSize);
            _currentPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            _currentPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
        }

        private void InitializeHook()
        {
            _hook = new GlobalInputHook(hookKeyboard: false, hookMouse: true);

            _hook.MouseDown += (s, e) =>
            {
                if (e.LeftButton)
                {
                    // 过滤：如果限制在窗体内且鼠标真在窗外，忽略
                    if (_restrictToClient && !this.Bounds.Contains(e.Location))
                        return;

                    BeginInvoke(new Action(() => StartDrawing(e.Location)));
                }
            };

            _hook.MouseUp += (s, e) =>
            {
                if (e.LeftButton)
                    BeginInvoke(new Action(() => StopDrawing()));
            };

            _hook.MouseMove += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    UpdateStatus(e.Location, e.LeftButton);
                    ContinueDrawing(e.Location);
                }));
            };
        }

        private void InitializeUI()
        {
            this.Text = "全局钩子画板 - 可在窗体外绘画";
            this.Size = new Size(1300, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 主画布 - 使用 Panel 托管，支持滚动
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BorderStyle = BorderStyle.Fixed3D
            };

            _picCanvas = new PictureBox
            {
                Image = _canvas,
                SizeMode = PictureBoxSizeMode.AutoSize, // 关键：让图片框按画布大小显示
                BackColor = Color.Transparent,
                Location = Point.Empty
            };

            // 可选：让画布在 Panel 中居中
            CenterCanvasInPanel(scrollPanel);

            scrollPanel.Controls.Add(_picCanvas);
            scrollPanel.Resize += (s, e) => CenterCanvasInPanel(scrollPanel);

            // 工具栏（保持不变，略作优化）
            var toolStrip = new ToolStrip();

            // 颜色按钮
            var colors = new[] { Color.Red, Color.Blue, Color.Green, Color.Black, Color.Yellow, Color.Purple, Color.Orange };
            foreach (var c in colors)
            {
                var btn = new ToolStripButton
                {
                    BackColor = c,
                    Width = 25,
                    Height = 25,
                    ToolTipText = c.Name,
                    DisplayStyle = ToolStripItemDisplayStyle.None,
                    Margin = new Padding(2)
                };
                btn.Click += (s, e) => ChangeColor(c);
                toolStrip.Items.Add(btn);
            }

            toolStrip.Items.Add(new ToolStripSeparator());

            // 笔刷大小
            toolStrip.Items.Add(new ToolStripLabel("笔刷:"));
            var sizeCombo = new ToolStripComboBox { Width = 50 };
            for (int i = 1; i <= 50; i++) sizeCombo.Items.Add(i);
            sizeCombo.SelectedItem = 3;
            sizeCombo.SelectedIndexChanged += (s, e) =>
            {
                _brushSize = Convert.ToSingle(sizeCombo.SelectedItem);
                _currentPen.Width = _brushSize;
            };
            toolStrip.Items.Add(sizeCombo);

            toolStrip.Items.Add(new ToolStripSeparator());

            // 全局/局部切换
            var chkGlobal = new ToolStripCheckBox("全局绘画模式")
            {
                Checked = true,
                ToolTipText = "勾选后可在窗体外绘画"
            };
            chkGlobal.CheckedChanged += (s, e) =>
            {
                _restrictToClient = !chkGlobal.Checked;
                // 切换模式时调整窗口样式
                if (!_restrictToClient)
                {
                    this.TopMost = true; // 全局模式置顶，方便看到画板
                }
            };
            toolStrip.Items.Add(chkGlobal);

            toolStrip.Items.Add(new ToolStripSeparator());

            // 清屏
            var btnClear = new ToolStripButton("清除", null, (s, e) => ClearCanvas());
            toolStrip.Items.Add(btnClear);

            // 保存
            var btnSave = new ToolStripButton("保存", null, (s, e) => SaveCanvas());
            toolStrip.Items.Add(btnSave);

            // 新增：适应窗口按钮（将画布缩放到适合窗口大小查看）
            var btnFit = new ToolStripButton("适应窗口", null, (s, e) => FitToWindow());
            toolStrip.Items.Add(btnFit);

            // 状态栏
            var statusStrip = new StatusStrip();
            _lblStatus = new ToolStripStatusLabel("就绪 - 按住左键开始绘画");
            statusStrip.Items.Add(_lblStatus);

            // 布局
            this.Controls.Add(scrollPanel);
            this.Controls.Add(toolStrip);
            this.Controls.Add(statusStrip);
        }

        private void CenterCanvasInPanel(Panel panel)
        {
            if (_picCanvas == null || panel == null) return;

            int x = Math.Max(0, (panel.ClientSize.Width - _picCanvas.Width) / 2);
            int y = Math.Max(0, (panel.ClientSize.Height - _picCanvas.Height) / 2);
            _picCanvas.Location = new Point(x, y);
        }

        private void FitToWindow()
        {
            // 缩放画布显示以适应窗口（不修改实际 Bitmap，只修改显示大小）
            var panel = _picCanvas.Parent as Panel;
            if (panel == null) return;

            float scaleX = (float)panel.ClientSize.Width / _canvas.Width;
            float scaleY = (float)panel.ClientSize.Height / _canvas.Height;
            float scale = Math.Min(scaleX, scaleY) * 0.9f; // 留点边距

            _picCanvas.SizeMode = PictureBoxSizeMode.Zoom;
            _picCanvas.Size = new Size((int)(_canvas.Width * scale), (int)(_canvas.Height * scale));
            CenterCanvasInPanel(panel);
        }

        private void StartDrawing(Point screenPoint)
        {
            // 转换为相对于画布的坐标（考虑画布在容器中的偏移）
            var canvasPoint = ScreenToCanvas(screenPoint);

            // 限制模式检查
            if (_restrictToClient)
            {
                var clientPoint = this.PointToClient(screenPoint);
                if (!this.ClientRectangle.Contains(clientPoint))
                    return;
            }

            _isDrawing = true;
            _lastPoint = canvasPoint;

            // 在起点画一个点（避免单点不显示）
            _graphics.FillEllipse(new SolidBrush(_currentColor),
                canvasPoint.X - _brushSize / 2, canvasPoint.Y - _brushSize / 2,
                _brushSize, _brushSize);
            RefreshCanvas(new Rectangle(
                (int)(canvasPoint.X - _brushSize),
                (int)(canvasPoint.Y - _brushSize),
                (int)(_brushSize * 2),
                (int)(_brushSize * 2)));
        }

        private void ContinueDrawing(Point screenPoint)
        {
            if (!_isDrawing) return;

            var canvasPoint = ScreenToCanvas(screenPoint);

            // 如果在限制模式且移出窗体，停止绘画
            if (_restrictToClient)
            {
                var clientPoint = this.PointToClient(screenPoint);
                if (!this.ClientRectangle.Contains(clientPoint))
                {
                    StopDrawing();
                    return;
                }
            }

            // 确保坐标在画布范围内（防止多显示器情况下画到画布外）
            canvasPoint.X = Math.Max(0, Math.Min(canvasPoint.X, _canvas.Width - 1));
            canvasPoint.Y = Math.Max(0, Math.Min(canvasPoint.Y, _canvas.Height - 1));

            // 绘画
            _graphics.DrawLine(_currentPen, _lastPoint, canvasPoint);

            // 计算刷新区域（起点和终点的外接矩形，考虑笔刷宽度）
            var refreshRect = GetLineBounds(_lastPoint, canvasPoint, (int)_brushSize + 2);
            _lastPoint = canvasPoint;

            RefreshCanvas(refreshRect);
        }

        private Point ScreenToCanvas(Point screenPoint)
        {
            // 将屏幕坐标转换为画布坐标
            // 如果画布是全屏尺寸，那么屏幕坐标就是画布坐标（假设画布左上角对齐屏幕左上角）
            // 但如果画布有偏移（居中显示），需要调整
            var canvasLocation = _picCanvas.PointToScreen(Point.Empty);
            return new Point(
                screenPoint.X - canvasLocation.X + _canvasOffset.X,
                screenPoint.Y - canvasLocation.Y + _canvasOffset.Y);
        }

        private Rectangle GetLineBounds(Point p1, Point p2, int padding)
        {
            int minX = Math.Min(p1.X, p2.X) - padding;
            int minY = Math.Min(p1.Y, p2.Y) - padding;
            int maxX = Math.Max(p1.X, p2.X) + padding;
            int maxY = Math.Max(p1.Y, p2.Y) + padding;

            return Rectangle.Intersect(
                new Rectangle(minX, minY, maxX - minX, maxY - minY),
                new Rectangle(0, 0, _canvas.Width, _canvas.Height));
        }

        private void StopDrawing()
        {
            _isDrawing = false;
        }

        private void RefreshCanvas(Rectangle? rect = null)
        {
            if (_picCanvas == null) return;

            if (rect.HasValue)
                _picCanvas.Invalidate(rect.Value); // 局部刷新，性能更好
            else
                _picCanvas.Invalidate();
        }

        private void ChangeColor(Color color)
        {
            _currentColor = color;
            _currentPen.Color = color;
        }

        private void ClearCanvas()
        {
            _graphics.Clear(Color.White);
            RefreshCanvas();
        }

        private void SaveCanvas()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG图片|*.png|JPEG图片|*.jpg|位图|*.bmp|所有文件|*.*";
                dialog.FileName = $"涂鸦_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                dialog.DefaultExt = "png";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ImageFormat format = ImageFormat.Png;
                        string ext = System.IO.Path.GetExtension(dialog.FileName).ToLower();
                        switch (ext)
                        {
                            case ".jpg":
                            case ".jpeg":
                                format = ImageFormat.Jpeg;
                                break;
                            case ".bmp":
                                format = ImageFormat.Bmp;
                                break;
                        }

                        // 保存时裁剪掉多余的空白区域（可选）
                        _canvas.Save(dialog.FileName, format);
                        MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UpdateStatus(Point screenPos, bool isLeftDown)
        {
            if (_lblStatus == null) return;
            var canvasPos = ScreenToCanvas(screenPos);
            _lblStatus.Text = $"屏幕: ({screenPos.X}, {screenPos.Y}) | 画布: ({canvasPos.X}, {canvasPos.Y}) | 状态: {(isLeftDown ? "绘画中" : "就绪")} | 模式: {(_restrictToClient ? "窗体限制" : "全局")}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _hook?.Dispose();
            _graphics?.Dispose();
            _canvas?.Dispose();
            _currentPen?.Dispose();
            base.OnFormClosing(e);
        }
    }

    // ToolStripCheckBox 保持不变
    public class ToolStripCheckBox : ToolStripControlHost
    {
        public ToolStripCheckBox(string text) : base(new CheckBox())
        {
            ((CheckBox)Control).Text = text;
            ((CheckBox)Control).AutoSize = true;
        }

        public bool Checked
        {
            get => ((CheckBox)Control).Checked;
            set => ((CheckBox)Control).Checked = value;
        }

        public event EventHandler CheckedChanged
        {
            add { ((CheckBox)Control).CheckedChanged += value; }
            remove { ((CheckBox)Control).CheckedChanged -= value; }
        }
    }
}
