using ScottPlot;
using ScottPlot.WinForms;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using Label = System.Windows.Forms.Label;

namespace _005Tools
{
    public class TrendChartForm : Form
    {
        // === ScottPlot 控件 ===
        private FormsPlot _formsPlot;

        // === 数据 ===
        private readonly List<DateTime> _rtTimes = new List<DateTime>();
        private readonly List<double> _rtValues = new List<double>();
        private readonly List<(DateTime Time, double Value)> _historyData = new List<(DateTime, double)>();
        private readonly Random _rand = new Random();

        // === 模式 ===
        private enum TrendMode { Realtime, History }
        private TrendMode _currentMode = TrendMode.Realtime;

        // === 历史视图 ===
        private DateTime _historyViewStart;
        private DateTime _historyViewEnd;
        private readonly TimeSpan _historyViewSpan = TimeSpan.FromHours(2);
        private DateTime _historyFullStart;
        private DateTime _historyFullEnd;

        // === UI 控件 ===
        private Panel _rightPanel;
        private Panel _bottomPanel;
        private Button _btnRealtime;
        private Button _btnHistory;
        private Button _btnExport;
        private Button _btnBack;
        private Button _btnFirst;
        private Button _btnPrevFast;
        private Button _btnPrev;
        private Button _btnPlay;
        private Button _btnNext;
        private Button _btnNextFast;
        private Button _btnLast;
        private Label _lblTitle;
        private System.Windows.Forms.Timer _timer;
        private System.Windows.Forms.Timer _playTimer;
        private bool _isPlaying = false;

        public TrendChartForm()
        {
            this.Text = "趋势图";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightGray;

            InitializeUI();
            InitializePlot();
            GenerateMockHistoryData();
            StartRealtimeMode();
        }

        private void InitializeUI()
        {
            // 顶部标题栏（蓝色，匹配截图）
            _lblTitle = new Label
            {
                Text = "ABCD",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.RoyalBlue,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0)
            };
            this.Controls.Add(_lblTitle);

            // 右侧功能按钮面板
            _rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 95,
                BackColor = Color.LightGray,
                Padding = new Padding(5, 10, 5, 5)
            };
            this.Controls.Add(_rightPanel);

            _btnRealtime = CreateSideButton("实时曲线", 0);
            _btnHistory = CreateSideButton("历史曲线", 1);
            _btnExport = CreateSideButton("导出", 2);
            _btnBack = CreateSideButton("返回", 3);

            _btnRealtime.Click += (s, e) => StartRealtimeMode();
            _btnHistory.Click += (s, e) => StartHistoryMode();
            _btnExport.Click += (s, e) => ExportData();
            _btnBack.Click += (s, e) => this.Close();

            // 底部播放控制面板
            _bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.LightGray,
                Padding = new Padding(5, 5, 5, 5)
            };
            this.Controls.Add(_bottomPanel);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.LightGray
            };
            _bottomPanel.Controls.Add(btnPanel);

            _btnFirst = CreateNavButton("|<");
            _btnPrevFast = CreateNavButton("<<");
            _btnPrev = CreateNavButton("<");
            _btnPlay = CreateNavButton("▶");
            _btnNext = CreateNavButton(">");
            _btnNextFast = CreateNavButton(">>");
            _btnLast = CreateNavButton(">|");

            btnPanel.Controls.Add(_btnFirst);
            btnPanel.Controls.Add(_btnPrevFast);
            btnPanel.Controls.Add(_btnPrev);
            btnPanel.Controls.Add(_btnPlay);
            btnPanel.Controls.Add(_btnNext);
            btnPanel.Controls.Add(_btnNextFast);
            btnPanel.Controls.Add(_btnLast);

            // 导航事件绑定
            _btnFirst.Click += (s, e) => HistoryNavigate(-1, true);
            _btnPrevFast.Click += (s, e) => HistoryNavigate(-0.5, false);
            _btnPrev.Click += (s, e) => HistoryNavigate(-0.2, false);
            _btnNext.Click += (s, e) => HistoryNavigate(0.2, false);
            _btnNextFast.Click += (s, e) => HistoryNavigate(0.5, false);
            _btnLast.Click += (s, e) => HistoryNavigate(1, true);
            _btnPlay.Click += (s, e) => TogglePlay();
        }

        private Button CreateSideButton(string text, int index)
        {
            var btn = new Button
            {
                Text = text,
                Width = 85,
                Height = 34,
                Top = 10 + index * 44,
                Left = 5,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke,
                Font = new Font("微软雅黑", 9),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.DarkGray;
            btn.FlatAppearance.BorderSize = 1;
            _rightPanel.Controls.Add(btn);
            return btn;
        }

        private Button CreateNavButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 52,
                Height = 32,
                Margin = new Padding(3, 2, 3, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.DarkGray;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private void InitializePlot()
        {
            _formsPlot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            // 将图表控件插入到右侧面板之前（确保层级正确）
            this.Controls.Add(_formsPlot);
            this.Controls.SetChildIndex(_formsPlot, 0);

            // ========== 工业风配色（还原截图风格） ==========
            _formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#C0C0C0"); // 整体背景灰
            _formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#DCDCDC");  // 绘图区浅灰
            _formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#A9A9A9"); // 网格深灰
            _formsPlot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("#C0C0C0");
            _formsPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("#333333"));            // 轴线颜色

            // 标题和轴标签
            _formsPlot.Plot.Title("Real Time Trend", size: 16);
            _formsPlot.Plot.YLabel("Value", size: 12);
            _formsPlot.Plot.XLabel("Time", size: 12);

            // 固定 Y 轴 0-30（匹配截图纵轴）
            _formsPlot.Plot.Axes.Left.Min = 0;
            _formsPlot.Plot.Axes.Left.Max = 30;

            // ========== ScottPlot 5 自定义时间轴刻度格式 ==========
            // ScottPlot 5 没有 TickLabelStyle.Format 属性，
            // 需通过 RenderStarting 事件在渲染前重写 tick 标签文本
            _formsPlot.Plot.RenderManager.RenderStarting += (s, e) =>
            {
                var ticks = _formsPlot.Plot.Axes.Bottom.TickGenerator.Ticks;
                for (int i = 0; i < ticks.Length; i++)
                {
                    DateTime dt = DateTime.FromOADate(ticks[i].Position);
                    string label = _currentMode == TrendMode.Realtime
                        ? dt.ToString("HH:mm:ss")
                        : dt.ToString("MM-dd HH:mm");
                    ticks[i] = new Tick(ticks[i].Position, label);
                }
            };
            // 禁用默认右键菜单（工业上位机风格）
            _formsPlot.Menu?.Clear();

            _formsPlot.Refresh();
        }

        // ==================== Mock 数据生成 ====================

        /// <summary>
        /// 模拟 PLC 实时读取：带日周期正弦趋势 + 随机噪声 + 偶尔尖峰 + 缓慢漂移
        /// 返回值范围控制在 0-30 之间
        /// </summary>
        private double ReadFromPLC()
        {
            double nowHour = DateTime.Now.TimeOfDay.TotalHours;

            // 1. 日周期趋势：白天高、晚上低，基准 15，振幅 5
            double trend = 15 + 5 * Math.Sin((nowHour - 6) * Math.PI / 12);

            // 2. 随机噪声 ±2.0
            double noise = (_rand.NextDouble() - 0.5) * 4.0;

            // 3. 偶尔尖峰（3% 概率，模拟设备异常或切换）
            double spike = 0;
            if (_rand.Next(0, 100) < 3)
            {
                spike = (_rand.NextDouble() - 0.5) * 10;
            }

            // 4. 缓慢漂移（30 分钟周期）
            double drift = Math.Sin(DateTime.Now.TimeOfDay.TotalMinutes / 30.0 * Math.PI) * 2;

            double value = trend + noise + spike + drift;
            return Clamp(value, 0, 30);
        }

        /// <summary>
        /// 生成模拟历史数据（过去 7 天，每分钟一个点，共约 1 万点）
        /// 模拟真实 PLC 历史记录：日周期 + 工作日差异 + 噪声 + 偶尔异常
        /// </summary>
        private void GenerateMockHistoryData()
        {
            DateTime end = DateTime.Now;
            DateTime start = end.AddDays(-7);
            _historyFullStart = start;
            _historyFullEnd = end;

            DateTime current = start;
            while (current <= end)
            {
                // 日周期：6 点开始上升，18 点达到峰值
                double hour = current.Hour + current.Minute / 60.0;
                double dailyCycle = 15 + 6 * Math.Sin((hour - 6) * Math.PI / 12);

                // 周末略低
                bool isWeekend = current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday;
                double weeklyFactor = isWeekend ? -1.5 : 0;

                // 噪声
                double noise = (_rand.NextDouble() - 0.5) * 2.5;

                // 偶尔异常（0.1% 概率）
                double anomaly = 0;
                if (_rand.Next(0, 1000) < 1)
                {
                    anomaly = (_rand.NextDouble() - 0.5) * 12;
                }

                double value = dailyCycle + weeklyFactor + noise + anomaly;
                value = Clamp(value, 0, 30);

                _historyData.Add((current, value));
                current = current.AddMinutes(1);
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ==================== 实时模式 ====================

        private void StartRealtimeMode()
        {
            _currentMode = TrendMode.Realtime;
            _formsPlot.Plot.Title("实时趋势");
            //_formsPlot.Plot.Axes.Bottom.TickLabelStyle.Format = "HH:mm:ss";

            _rtTimes.Clear();
            _rtValues.Clear();

            // 启动定时器，每秒读取一次 PLC
            if (_timer == null)
            {
                _timer = new System.Windows.Forms.Timer { Interval = 1000 };
                _timer.Tick += RealtimeTimer_Tick;
            }
            _timer.Start();

            HighlightButton(_btnRealtime);
            _btnPlay.Enabled = false;
            _btnPlay.Text = "▶";
            _isPlaying = false;
            _playTimer?.Stop();

            RefreshRealtimePlot();
        }

        private void RealtimeTimer_Tick(object sender, EventArgs e)
        {
            if (_currentMode != TrendMode.Realtime) return;

            double value = ReadFromPLC();
            DateTime now = DateTime.Now;

            _rtTimes.Add(now);
            _rtValues.Add(value);

            // 只保留最近 5 分钟（300 个点），模拟滚动显示
            if (_rtTimes.Count > 300)
            {
                _rtTimes.RemoveAt(0);
                _rtValues.RemoveAt(0);
            }

            RefreshRealtimePlot();
        }

        private void RefreshRealtimePlot()
        {
            _formsPlot.Plot.Clear();

            if (_rtValues.Count > 0)
            {
                // 使用 Scatter 绘制实时曲线
                var scatter = _formsPlot.Plot.Add.Scatter(_rtTimes.ToArray(), _rtValues.ToArray());
                scatter.LineWidth = 2;
                scatter.MarkerSize = 3;
                scatter.Color = ScottPlot.Color.FromHex("#0066CC");

                // X 轴自动滚动：始终显示最近 2 分钟窗口
                DateTime maxTime = _rtTimes[_rtTimes.Count - 1];
                DateTime minTime = maxTime.AddMinutes(-2);
                _formsPlot.Plot.Axes.SetLimitsX(minTime.ToOADate(), maxTime.AddSeconds(5).ToOADate());
            }

            _formsPlot.Plot.Axes.SetLimitsY(0, 30);
            _formsPlot.Refresh();
        }

        // ==================== 历史模式 ====================

        private void StartHistoryMode()
        {
            _currentMode = TrendMode.History;
            _timer?.Stop();

            _formsPlot.Plot.Title("历史趋势");
            //_formsPlot.Plot.Axes.Bottom.TickLabelStyle.Format = "MM-dd HH:mm";

            // 默认显示最近 2 小时
            _historyViewEnd = _historyFullEnd;
            _historyViewStart = _historyViewEnd - _historyViewSpan;

            HighlightButton(_btnHistory);
            _btnPlay.Enabled = true;
            _btnPlay.Text = "▶";
            _isPlaying = false;
            _playTimer?.Stop();

            RefreshHistoryPlot();
        }

        private void RefreshHistoryPlot()
        {
            _formsPlot.Plot.Clear();

            var filtered = _historyData
                .Where(d => d.Time >= _historyViewStart && d.Time <= _historyViewEnd)
                .ToList();

            if (filtered.Count > 0)
            {
                var xs = filtered.Select(d => d.Time).ToArray();
                var ys = filtered.Select(d => d.Value).ToArray();

                var scatter = _formsPlot.Plot.Add.Scatter(xs, ys);
                scatter.LineWidth = 1.5f;
                scatter.MarkerSize = 0; // 历史数据点密集，不显示标记点
                scatter.Color = ScottPlot.Color.FromHex("#0066CC");

                _formsPlot.Plot.Axes.SetLimitsX(
                    _historyViewStart.ToOADate(),
                    _historyViewEnd.ToOADate()
                );
            }

            _formsPlot.Plot.Axes.SetLimitsY(0, 30);
            _formsPlot.Refresh();
        }

        /// <summary>
        /// 历史导航
        /// </summary>
        /// <param name="ratio">移动比例（正数向前，负数向后），绝对值 1 表示跳转到边缘</param>
        /// <param name="toEdge">true 表示直接跳转到最前/最后</param>
        private void HistoryNavigate(double ratio, bool toEdge)
        {
            if (_currentMode != TrendMode.History) return;

            TimeSpan viewSpan = _historyViewEnd - _historyViewStart;

            if (toEdge)
            {
                if (ratio < 0) // 最前
                {
                    _historyViewStart = _historyFullStart;
                    _historyViewEnd = _historyViewStart + viewSpan;
                }
                else // 最后
                {
                    _historyViewEnd = _historyFullEnd;
                    _historyViewStart = _historyViewEnd - viewSpan;
                }
            }
            else
            {
                TimeSpan shift = TimeSpan.FromTicks((long)(viewSpan.Ticks * Math.Abs(ratio)));
                if (ratio < 0)
                {
                    _historyViewStart -= shift;
                    _historyViewEnd -= shift;
                }
                else
                {
                    _historyViewStart += shift;
                    _historyViewEnd += shift;
                }

                // 边界检查
                if (_historyViewStart < _historyFullStart)
                {
                    _historyViewStart = _historyFullStart;
                    _historyViewEnd = _historyViewStart + viewSpan;
                }
                if (_historyViewEnd > _historyFullEnd)
                {
                    _historyViewEnd = _historyFullEnd;
                    _historyViewStart = _historyViewEnd - viewSpan;
                }
            }

            RefreshHistoryPlot();
        }

        private void TogglePlay()
        {
            if (_currentMode != TrendMode.History) return;

            _isPlaying = !_isPlaying;
            _btnPlay.Text = _isPlaying ? "❚❚" : "▶";

            if (_isPlaying)
            {
                if (_playTimer == null)
                {
                    _playTimer = new System.Windows.Forms.Timer { Interval = 800 };
                    _playTimer.Tick += (s, e) =>
                    {
                        if (!_isPlaying) return;
                        HistoryNavigate(0.08, false); // 每次前进 8%
                        if (_historyViewEnd >= _historyFullEnd)
                        {
                            _isPlaying = false;
                            _btnPlay.Text = "▶";
                            _playTimer.Stop();
                        }
                    };
                }
                _playTimer.Start();
            }
            else
            {
                _playTimer?.Stop();
            }
        }

        // ==================== 导出功能 ====================

        private void ExportData()
        {
            if (_currentMode == TrendMode.Realtime)
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "CSV 文件|*.csv",
                    FileName = $"实时数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var lines = new List<string> { "Time,Value" };
                    for (int i = 0; i < _rtTimes.Count; i++)
                    {
                        lines.Add($"{_rtTimes[i]:yyyy-MM-dd HH:mm:ss},{_rtValues[i]:F2}");
                    }
                    File.WriteAllLines(dlg.FileName, lines);
                    MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "CSV 文件|*.csv",
                    FileName = $"历史数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var filtered = _historyData
                        .Where(d => d.Time >= _historyViewStart && d.Time <= _historyViewEnd)
                        .ToList();

                    var lines = new List<string> { "Time,Value" };
                    foreach (var item in filtered)
                    {
                        lines.Add($"{item.Time:yyyy-MM-dd HH:mm:ss},{item.Value:F2}");
                    }
                    File.WriteAllLines(dlg.FileName, lines);
                    MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // ==================== 辅助方法 ====================

        private void HighlightButton(Button active)
        {
            foreach (var btn in new[] { _btnRealtime, _btnHistory })
            {
                btn.BackColor = Color.WhiteSmoke;
                btn.ForeColor = Color.Black;
                btn.Font = new Font("微软雅黑", 9, FontStyle.Regular);
            }
            active.BackColor = Color.RoyalBlue;
            active.ForeColor = Color.White;
            active.Font = new Font("微软雅黑", 9, FontStyle.Bold);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _playTimer?.Stop();
            _playTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }

    // ==================== 程序入口 ====================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrendChartForm());
        }
    }
}