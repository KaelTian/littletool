using ScottPlot;
using ScottPlot.WinForms;
using System.Windows.Forms;

namespace TrendChartDemo
{
    public partial class MainForm : Form
    {
        private FormsPlot formsPlotZ1;
        private FormsPlot formsPlotZ2;
        private System.Windows.Forms.Timer refreshTimer;

        // 模拟数据存储
        private List<CurrentRecord> z1Records = new List<CurrentRecord>();
        private List<CurrentRecord> z2Records = new List<CurrentRecord>();

        // 数据文件路径（实际项目中使用）
        private string dataFilePath = "current_data.csv";

        public MainForm()
        {
            InitializeComponent();
            InitializePlots();
            GenerateMockData();  // 生成假数据
            SetupTimer();
            RefreshPlots();
        }

        private void InitializePlots()
        {
            this.Text = "Z1/Z2 主轴电流曲线";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 主布局：上下两个曲线图
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.Controls.Add(mainPanel);

            // === Z1 曲线图 ===
            var z1Panel = new GroupBox
            {
                Text = "Z1 主轴电流",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            formsPlotZ1 = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            ConfigurePlot(formsPlotZ1, "Z1");
            z1Panel.Controls.Add(formsPlotZ1);
            mainPanel.Controls.Add(z1Panel, 0, 0);

            // === Z2 曲线图 ===
            var z2Panel = new GroupBox
            {
                Text = "Z2 主轴电流",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            formsPlotZ2 = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            ConfigurePlot(formsPlotZ2, "Z2");
            z2Panel.Controls.Add(formsPlotZ2);
            mainPanel.Controls.Add(z2Panel, 0, 1);

            // 刷新按钮
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5)
            };
            var btnRefresh = new Button
            {
                Text = "刷新数据",
                Width = 100,
                Height = 30
            };
            btnRefresh.Click += (s, e) => RefreshPlots();
            btnPanel.Controls.Add(btnRefresh);

            var btnAddMock = new Button
            {
                Text = "追加实时数据",
                Width = 120,
                Height = 30
            };
            btnAddMock.Click += (s, e) => AppendRealTimeMockData();
            btnPanel.Controls.Add(btnAddMock);

            this.Controls.Add(btnPanel);
        }

        private void ConfigurePlot(FormsPlot formsPlot, string title)
        {
            var plot = formsPlot.Plot;

            // 清除默认样式
            plot.Clear();

            // 设置中文字体（解决方框问题）
            // 使用系统自带的中文字体
            string[] chineseFonts = new[]
            {
                "Microsoft YaHei",
                "SimHei",
                "SimSun",
                "Arial Unicode MS",
                "WenQuanYi Micro Hei"
            };

            string selectedFont = null;
            foreach (var fontName in chineseFonts)
            {
                try
                {
                    using (var testFont = new Font(fontName, 12))
                    {
                        if (testFont.Name == fontName)  // 字体真实存在
                        {
                            selectedFont = fontName;
                            break;
                        }
                    }
                }
                catch { }
            }

            if (selectedFont != null)
            {
                plot.Font.Set(selectedFont);
            }

            //// 标题
            //plot.Title($"{title} 主轴电流曲线", size: 16);

            //// Y轴标签
            //plot.YLabel("电流值 (A)", size: 12);

            // 标题
            plot.Title($"{title}", size: 16);

            // Y轴标签
            plot.YLabel("(A)", size: 12);

            // X轴标签
            plot.XLabel("时间", size: 12);

            // 设置背景色
            plot.FigureBackground.Color = ScottPlot.Color.FromColor(System.Drawing.Color.White);
            plot.DataBackground.Color = ScottPlot.Color.FromColor(System.Drawing.Color.FromArgb(250, 250, 250));

            // 网格线
            plot.Grid.MajorLineColor = ScottPlot.Color.FromColor(System.Drawing.Color.LightGray);
            plot.Grid.MajorLineWidth = 1;

            // 时间轴配置：关键！控制刻度密度避免太乱
            var xAxis = plot.Axes.Bottom;

            // 设置时间显示格式
            xAxis.TickLabelStyle.FontSize = 10;
            xAxis.TickLabelStyle.Rotation = 30;  // 倾斜30度，避免重叠

            // 关键：控制刻度生成，避免每秒一个刻度太密集
            // 方式1：使用 DateTime 刻度，让 ScottPlot 自动决定密度
            plot.Axes.DateTimeTicksBottom();

            //// 只留底部和左侧的轴线和刻度（经典科学图表风格）
            //plot.Axes.Top.FrameLineStyle.IsVisible = false;
            //plot.Axes.Right.FrameLineStyle.IsVisible = false;
            //plot.Axes.Top.TickLabelStyle.IsVisible = false;
            //plot.Axes.Right.TickLabelStyle.IsVisible = false;

            // 方式2：手动控制刻度间隔（如果数据量很大时）
            // 当数据点多时，可以设置最小刻度单位
            // xAxis.TickGenerator = new ScottPlot.TickGenerators.DateTimeFixedInterval(
            //     TimeSpan.FromMinutes(1)  // 每1分钟一个主刻度
            // );
        }

        /// <summary>
        /// 生成模拟假数据（最近10分钟，每秒一个点）
        /// </summary>
        private void GenerateMockData()
        {
            z1Records.Clear();
            z2Records.Clear();

            var now = DateTime.Now;
            var random = new Random();

            // 生成最近 10 分钟的数据，每秒一个点 = 600个点
            // 实际项目中可能是1天的数据，这里先少点看效果
            for (int i = 600; i >= 0; i--)
            {
                var time = now.AddSeconds(-i);

                // Z1: 基础值 10-20A，加上随机波动和正弦波动模拟真实电流
                double z1Base = 15.0;
                double z1Noise = random.NextDouble() * 2 - 1;  // -1 到 1
                double z1Sine = Math.Sin(i * 0.05) * 3;  // 正弦波动
                double z1Value = z1Base + z1Noise + z1Sine + random.Next(0, 5);

                // Z2: 基础值 25-35A，波动更大一些
                double z2Base = 30.0;
                double z2Noise = random.NextDouble() * 3 - 1.5;
                double z2Sine = Math.Cos(i * 0.03) * 5;
                double z2Value = z2Base + z2Noise + z2Sine + random.Next(0, 8);

                z1Records.Add(new CurrentRecord { Time = time, Type = "Z1", Value = z1Value });
                z2Records.Add(new CurrentRecord { Time = time, Type = "Z2", Value = z2Value });
            }

            // 同时生成 CSV 文件模拟真实场景
            SaveMockDataToFile();
        }

        /// <summary>
        /// 追加实时数据（模拟每秒新增）
        /// </summary>
        private void AppendRealTimeMockData()
        {
            var random = new Random();
            var now = DateTime.Now;

            // Z1 追加5个点
            for (int i = 0; i < 5; i++)
            {
                var time = now.AddSeconds(i);
                double value = 15 + random.NextDouble() * 5 + Math.Sin(i) * 2;
                z1Records.Add(new CurrentRecord { Time = time, Type = "Z1", Value = value });
            }

            // Z2 追加5个点
            for (int i = 0; i < 5; i++)
            {
                var time = now.AddSeconds(i);
                double value = 30 + random.NextDouble() * 8 + Math.Cos(i) * 3;
                z2Records.Add(new CurrentRecord { Time = time, Type = "Z2", Value = value });
            }

            // 保持数据量不要无限增长（只保留最近1000个点）
            if (z1Records.Count > 1000) z1Records.RemoveRange(0, z1Records.Count - 1000);
            if (z2Records.Count > 1000) z2Records.RemoveRange(0, z2Records.Count - 1000);

            RefreshPlots();
        }

        /// <summary>
        /// 保存模拟数据到CSV（模拟真实文件格式）
        /// </summary>
        private void SaveMockDataToFile()
        {
            var lines = new List<string>();
            int maxCount = Math.Max(z1Records.Count, z2Records.Count);

            // 合并 Z1 和 Z2 按时间排序
            var allRecords = z1Records.Concat(z2Records).OrderBy(r => r.Time).ToList();

            foreach (var record in allRecords)
            {
                lines.Add($"{record.Time:yyyy-MM-dd HH:mm:ss},{record.Type},{record.Value:F3}");
            }

            File.WriteAllLines(dataFilePath, lines);
        }

        /// <summary>
        /// 从CSV读取数据（实际项目使用）
        /// </summary>
        private List<CurrentRecord> ReadCurrentDataFromFile(string type)
        {
            var records = new List<CurrentRecord>();

            if (!File.Exists(dataFilePath)) return records;

            foreach (var line in File.ReadAllLines(dataFilePath))
            {
                var parts = line.Split(',');
                if (parts.Length >= 3 && parts[1] == type)
                {
                    if (DateTime.TryParse(parts[0], out var time) &&
                        double.TryParse(parts[2], out var value))
                    {
                        records.Add(new CurrentRecord { Time = time, Type = type, Value = value });
                    }
                }
            }

            return records.OrderBy(r => r.Time).ToList();
        }

        /// <summary>
        /// 刷新两个曲线图
        /// </summary>
        private void RefreshPlots()
        {
            RefreshSinglePlot(formsPlotZ1, z1Records, "Z1", ScottPlot.Color.FromColor(System.Drawing.Color.OrangeRed));
            RefreshSinglePlot(formsPlotZ2, z2Records, "Z2", ScottPlot.Color.FromColor(System.Drawing.Color.SteelBlue));
        }

        /// <summary>
        /// 刷新单个曲线图
        /// </summary>
        /// <summary>
        /// 刷新单个曲线图
        /// </summary>
        private void RefreshSinglePlot(FormsPlot formsPlot, List<CurrentRecord> records,
            string type, ScottPlot.Color lineColor)
        {
            var plot = formsPlot.Plot;
            plot.Clear();

            if (records.Count == 0) return;

            // 提取时间和数值
            var times = records.Select(r => r.Time).ToArray();
            var values = records.Select(r => r.Value).ToArray();

            // X轴：转成 OLE Automation Date
            var xValues = times.Select(t => t.ToOADate()).ToArray();

            // 画曲线
            var scatter = plot.Add.Scatter(xValues, values);
            scatter.LineWidth = 1.5f;
            scatter.Color = lineColor;
            scatter.MarkerSize = 0;  // 不画圆点，只画线
            scatter.LinePattern = LinePattern.Solid;

            // Y轴范围：留 10% 边距
            double yMin = values.Min();
            double yMax = values.Max();
            double yPadding = Math.Max((yMax - yMin) * 0.1, 1.0);
            plot.Axes.SetLimitsY(yMin - yPadding, yMax + yPadding);

            // ========== 时间轴配置（只启用自动时间轴 + 样式防重叠）==========
            plot.Axes.DateTimeTicksBottom();

            // 字号缩小 + 倾斜，防止标签重叠
            plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
            plot.Axes.Bottom.TickLabelStyle.Rotation = 30;


            // X轴范围自动适应
            plot.Axes.AutoScaleX();

            // 最右侧显示当前值标注
            var lastRecord = records.Last();
            var lastX = lastRecord.Time.ToOADate();
            var lastY = lastRecord.Value;

            var txt = plot.Add.Text($"{lastY:F2}A", new ScottPlot.Coordinates(lastX, lastY));
            txt.LabelFontSize = 12;
            txt.LabelBold = true;
            txt.LabelFontColor = lineColor;
            txt.OffsetX = 10;
            txt.OffsetY = -10;

            // 刷新画布
            formsPlot.Refresh();
        }

        ///// <summary>
        ///// 刷新单个曲线图
        ///// </summary>
        //private void RefreshSinglePlot(FormsPlot formsPlot, List<CurrentRecord> records,
        //    string type, ScottPlot.Color lineColor)
        //{
        //    var plot = formsPlot.Plot;
        //    plot.Clear();

        //    if (records.Count == 0) return;

        //    // 转换为 DateTime 和 double 数组
        //    var times = records.Select(r => r.Time).ToArray();
        //    var values = records.Select(r => r.Value).ToArray();

        //    // 转换为 OLE Automation Date（ScottPlot DateTime 轴需要的格式）
        //    var xValues = times.Select(t => t.ToOADate()).ToArray();

        //    // 添加信号图（Signal）或散点图（Scatter）
        //    // 数据量大时用 Signal，数据量小时用 Scatter
        //    // 这里每秒一个点，10分钟600个点，用 Scatter 更清楚
        //    var scatter = plot.Add.Scatter(xValues, values);
        //    scatter.LineWidth = 1.5f;
        //    scatter.Color = lineColor;
        //    scatter.MarkerSize = 0;  // 不显示标记点，只显示线
        //    scatter.LinePattern = LinePattern.Solid;

        //    // 填充曲线下方区域（可选，更好看）
        //    // 注意：ScottPlot 5 的填充方式
        //    // 这里简单处理，只画线

        //    // 设置坐标轴范围
        //    // X轴：自动适应时间范围
        //    plot.Axes.AutoScaleX();

        //    // Y轴：留一些边距
        //    double yMin = values.Min();
        //    double yMax = values.Max();
        //    double yPadding = (yMax - yMin) * 0.1;
        //    if (yPadding < 1) yPadding = 1;
        //    plot.Axes.SetLimitsY(yMin - yPadding, yMax + yPadding);
        //    // ========== 时间轴配置（ScottPlot 5 正确写法）==========

        //    // 1. 启用时间轴（核心）
        //    plot.Axes.DateTimeTicksBottom();

        //    // 2. 标签样式：字号 + 倾斜防重叠
        //    plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
        //    plot.Axes.Bottom.TickLabelStyle.Rotation = 25;

        //    // 3. 根据时间跨度，控制刻度密度（用 NumericFixedInterval，单位是"天"）
        //    var timeSpan = times.Last() - times.First();
        //    double intervalDays;  // 刻度间隔，单位：天（OLE Automation Date）

        //    if (timeSpan.TotalMinutes <= 5)
        //        intervalDays = TimeSpan.FromSeconds(10).TotalDays;   // 10秒
        //    else if (timeSpan.TotalMinutes <= 30)
        //        intervalDays = TimeSpan.FromMinutes(1).TotalDays;    // 1分钟
        //    else if (timeSpan.TotalHours <= 2)
        //        intervalDays = TimeSpan.FromMinutes(5).TotalDays;    // 5分钟
        //    else if (timeSpan.TotalHours <= 24)
        //        intervalDays = TimeSpan.FromMinutes(30).TotalDays;   // 30分钟
        //    else
        //        intervalDays = TimeSpan.FromHours(2).TotalDays;    // 2小时

        //    plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(intervalDays);


        //    // 添加当前值标注（最右侧的点）
        //    var lastRecord = records.Last();
        //    var lastX = lastRecord.Time.ToOADate();
        //    var lastY = lastRecord.Value;

        //    var txt = plot.Add.Text($"{lastY:F2}A", new ScottPlot.Coordinates(lastX, lastY));
        //    txt.LabelFontSize = 12;
        //    txt.LabelBold = true;
        //    txt.LabelFontColor = lineColor;
        //    txt.OffsetX = 10;
        //    txt.OffsetY = -10;

        //    // 刷新
        //    formsPlot.Refresh();
        //}



        /// <summary>
        /// 设置定时器，模拟实时刷新
        /// </summary>
        private void SetupTimer()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 2000;  // 2秒刷新一次
            refreshTimer.Tick += (s, e) =>
            {
                // 模拟实时追加数据
                var random = new Random();
                var now = DateTime.Now;

                z1Records.Add(new CurrentRecord
                {
                    Time = now,
                    Type = "Z1",
                    Value = 15 + random.NextDouble() * 5
                });

                z2Records.Add(new CurrentRecord
                {
                    Time = now,
                    Type = "Z2",
                    Value = 30 + random.NextDouble() * 8
                });

                // 限制数据量
                if (z1Records.Count > 500) z1Records.RemoveAt(0);
                if (z2Records.Count > 500) z2Records.RemoveAt(0);

                RefreshPlots();
            };
            // refreshTimer.Start();  // 默认不启动，手动控制
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// 电流记录模型
    /// </summary>
    public class CurrentRecord
    {
        public DateTime Time { get; set; }
        public string Type { get; set; }  // Z1 或 Z2
        public double Value { get; set; }
    }
}