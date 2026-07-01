using ScottPlot;
using ScottPlot.WinForms;
using System.ComponentModel;

namespace TrendChartDemo
{
    /// <summary>
    /// 可复用的实时趋势图控件（基于 ScottPlot 5.x）
    /// </summary>
    [ToolboxItem(true)]
    [Description("可复用的实时趋势图控件（基于 ScottPlot 5.x）")]
    public class TrendChart : UserControl
    {
        private FormsPlot? _formsPlot;
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        // ========== 公开属性（设计器可见） ==========
        #region 公开属性（设计器可见）

        // ---- Y 轴 ----
        [Category("图表")]
        [Description("Y轴最小值")]
        [DefaultValue(0.0)]
        [Browsable(true)]
        public double YMin { get; set; } = 0;

        [Category("图表")]
        [Description("Y轴最大值")]
        [DefaultValue(30.0)]
        [Browsable(true)]
        public double YMax { get; set; } = 30;

        [Category("图表")]
        [Description("Y轴刻度步长")]
        [DefaultValue(5.0)]
        [Browsable(true)]
        public double YTickStep { get; set; } = 5;

        // ---- X 轴（新增） ----
        [Category("图表")]
        [Description("X轴时间窗口范围（秒），默认60秒")]
        [DefaultValue(60)]
        [Browsable(true)]
        public int TimeRangeSeconds { get; set; } = 60;

        [Category("图表")]
        [Description("X轴刻度间隔（秒），默认10秒")]
        [DefaultValue(10)]
        [Browsable(true)]
        public int XTickStepSeconds { get; set; } = 10;

        [Category("图表")]
        [Description("X轴时间标签格式，如 HH:mm:ss")]
        [DefaultValue("MM-dd HH:mm:ss")]
        [Browsable(true)]
        public string XAxisLabelFormat { get; set; } = "MM-dd HH:mm:ss";

        // ---- 其他 ----
        [Category("图表")]
        [Description("曲线颜色")]
        [DefaultValue(typeof(System.Drawing.Color), "0x2196F3")]
        [Browsable(true)]
        public System.Drawing.Color LineColor { get; set; } = System.Drawing.Color.FromArgb(0x21, 0x96, 0xF3);

        [Category("图表")]
        [Description("数值单位，显示在最右侧标注上，如 A、V、℃")]
        [DefaultValue("")]
        [Browsable(true)]
        public string Unit { get; set; } = "";

        [Category("图表")]
        [Description("图表标题")]
        [DefaultValue("")]
        [Browsable(true)]
        public string ChartTitle { get; set; } = "";

        #endregion

        public TrendChart()
        {
            InitializeComponent();
            if (!DesignMode)
            {
                this.HandleCreated += (s, e) => InitializePlotStyle();
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = System.Drawing.Color.Silver;
            Name = "TrendChart";
            Size = new Size(600, 300);
            ResumeLayout(false);
        }

        /// <summary>
        /// 初始化图表样式。重复调用会被忽略。
        /// </summary>
        private void InitializePlotStyle()
        {
            if (_isInitialized) return;

            _formsPlot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(_formsPlot);
            this.Controls.SetChildIndex(_formsPlot, 0);

            // 工业风配色
            _formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#C0C0C0");
            _formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#DCDCDC");
            _formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#A9A9A9");
            _formsPlot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("#C0C0C0");
            _formsPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("#333333"));
            // Title
            if (!string.IsNullOrEmpty(ChartTitle))
            {
                _formsPlot.Plot.Title(ChartTitle);
            }
            // Y轴样式
            ApplyYAxisConfig();   // 初始化 Y 轴
            // X轴样式
            ApplyXAxisConfig();   // 初始化 X 轴
            // 禁用用户交互 （鼠标缩放）
            _formsPlot.UserInputProcessor.Disable();
            _formsPlot.Refresh();
            _isInitialized = true;
        }

        /// <summary>
        /// 应用 Y 轴配置。修改 YMin/YMax/YTickStep 后调用。
        /// </summary>
        public void ApplyYAxisConfig()
        {
            if (_formsPlot == null) return;
            _formsPlot.Plot.Axes.Rules.Clear();
            var lockedVerticalRule = new ScottPlot.AxisRules.LockedVertical(
                _formsPlot.Plot.Axes.Left, yMin: YMin, yMax: YMax);
            _formsPlot.Plot.Axes.Rules.Add(lockedVerticalRule);
            _formsPlot.Plot.Axes.SetLimitsY(YMin, YMax);
            var yTicks = GenerateTicks(YMin, YMax, YTickStep).ToArray();
            var yLabels = yTicks.Select(y => y.ToString("F2")).ToArray();
            _formsPlot.Plot.Axes.Left.SetTicks(yTicks, yLabels);
            _formsPlot.Refresh();
        }

        /// <summary>
        /// 应用 X 轴配置。修改 TimeRangeSeconds/XTickStepSeconds/XAxisLabelFormat 后调用。
        /// </summary>
        public void ApplyXAxisConfig()
        {
            if (_formsPlot == null) return;
            var now = DateTime.Now;
            var startTime = now.AddSeconds(-TimeRangeSeconds);
            SetupTimeAxis(startTime);
            _formsPlot.Refresh();
        }

        /// <summary>
        /// 刷新曲线数据（线程安全）
        /// </summary>
        public void UpdateData(List<(DateTime, double)> datas)
        {
            if (datas == null) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateData(datas)));
                return;
            }
            lock (_lock)
            {
                RenderPlot(datas);
            }
        }

        /// <summary>
        /// 设置时间轴刻度：根据 TimeRangeSeconds / XTickStepSeconds 动态计算
        /// </summary>
        private void SetupTimeAxis(DateTime startTime)
        {
            if (_formsPlot == null || XTickStepSeconds <= 0 || TimeRangeSeconds <= 0)
                return;

            int tickCount = (TimeRangeSeconds / XTickStepSeconds) + 1;
            double[] xTicks = new double[tickCount];
            string[] xLabels = new string[tickCount];

            for (int i = 0; i < tickCount; i++)
            {
                DateTime tickTime = startTime.AddSeconds(i * XTickStepSeconds);
                xTicks[i] = tickTime.ToOADate();
                xLabels[i] = tickTime.ToString(XAxisLabelFormat);
            }

            var plot = _formsPlot.Plot;
            plot.Axes.Bottom.SetTicks(xTicks, xLabels);

            double tickSpan = TimeSpan.FromSeconds(XTickStepSeconds).TotalDays;
            double margin = tickSpan * 0.5;
            plot.Axes.SetLimitsX(xTicks[0], xTicks[tickCount - 1] + margin);
        }

        /// <summary>
        /// 渲染曲线图
        /// </summary>
        private void RenderPlot(List<(DateTime Time, double Value)> datas)
        {
            if (!_isInitialized || _formsPlot == null || datas == null || datas.Count == 0)
                return;

            var plot = _formsPlot.Plot;
            plot.Clear();

            datas.Sort((a, b) => a.Time.CompareTo(b.Time));
            var times = datas.Select(x => x.Time).ToList();
            var values = datas.Select(x => x.Value).ToList();

            double[] xs = times.Select(t => t.ToOADate()).ToArray();
            double[] ys = values.ToArray();

            var scatter = plot.Add.Scatter(xs, ys);
            scatter.MarkerStyle.Size = 3;
            scatter.LineStyle.Width = 2;
            scatter.LineStyle.Color = GetScottPlotColor();

            var lastTime = times.Last();
            var lastValue = values.Last();
            var labelText = string.IsNullOrEmpty(Unit)
                ? $"{lastValue:F2}"
                : $"{lastValue:F2}{Unit}";

            var txt = plot.Add.Text(labelText, new Coordinates(lastTime.ToOADate(), lastValue));
            txt.LabelFontSize = 12;
            txt.LabelBold = true;
            txt.LabelFontColor = ScottPlot.Color.FromHex("#333333");
            txt.OffsetX = 10;
            txt.OffsetY = -10;

            // 基于属性配置的时间范围重新绘制 X 轴
            var now = DateTime.Now;
            var startTime = now.AddSeconds(-TimeRangeSeconds);
            SetupTimeAxis(startTime);

            _formsPlot.Refresh();
        }

        private static IEnumerable<double> GenerateTicks(double min, double max, double step)
        {
            for (double v = min; v <= max + 1e-9; v += step)
                yield return v;
        }
        /// <summary>
        /// 转义成 ScottPlot.Color 对象
        /// </summary>
        /// <returns></returns>
        private ScottPlot.Color GetScottPlotColor() =>
            ScottPlot.Color.FromHex($"#{LineColor.R:X2}{LineColor.G:X2}{LineColor.B:X2}");
    }
}
