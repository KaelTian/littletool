namespace TrendChartDemo
{
    /// <summary>
    /// 电流曲线
    /// </summary>
    public partial class ICurveChart : UserControl
    {
        /// <summary>
        /// 定时刷新曲线图数据
        /// </summary>
        private System.Windows.Forms.Timer? _dataTimer;

        private bool _disposed = false;

        /// <summary>
        /// 是否是假数据模式
        /// to do: 生产环境下删掉
        /// </summary>
        private readonly bool _isMockMode = false;

        public ICurveChart()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 重载加载方法，启动Timer轮询获取曲线图数据源
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // 设计模式下跳过，防止设计器报错
            if (DesignMode) return;
            base.OnLoad(e);

            //先生成一批mock数据
            //to do: 后续去掉
            if (_isMockMode)
                InitializeMockData();

            // 启动 Timer 获取外部资源
            _dataTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1秒刷新
            };
            _dataTimer.Tick += (s, ev) => FetchCurveChartData();
            _dataTimer.Start();
        }

        /// <summary>
        /// 获取曲线图数据
        /// </summary>
        private void FetchCurveChartData()
        {
            if (_isMockMode)
            {
                // mock 获取数据方法
                MockGetCurveChartData();
            }
            else
            {
                //// 获取真实数据方法
                //List<(DateTime Time, double Value)>? z1Datas = WorkUnit?.GetD76CurrentData();
                //if (z1Datas?.Count > 0)
                //{
                //    z1TrendChart.UpdateData(z1Datas);
                //}
                //List<(DateTime Time, double Value)>? z2Datas = WorkUnit?.GetD78CurrentData();
                //if (z2Datas?.Count > 0)
                //{
                //    z2TrendChart.UpdateData(z2Datas);
                //}
            }
        }

        #region 测试数据,to do: 生产环境下删除.

        private readonly Random _rand = new Random();

        /// <summary>
        /// Z1 mock 曲线图数据源
        /// </summary>
        private readonly List<(DateTime Time, double Value)> _rt1Datas = new List<(DateTime Time, double Value)>();
        /// <summary>
        /// Z2 mock 曲线图数据源
        /// </summary>
        private readonly List<(DateTime Time, double Value)> _rt2Datas = new List<(DateTime Time, double Value)>();

        /// <summary>
        /// 初始化 Mock：预填充当前时间前 1 分钟（61 个点）的历史数据
        /// 建议在 Form_Load 或启动时调用一次
        /// to do: 生产环境下删掉
        /// </summary>
        private void InitializeMockData()
        {
            DateTime now = DateTime.Now;

            // 清空旧数据（防止重复初始化时叠加）
            _rt1Datas.Clear();
            _rt2Datas.Clear();
            // 生成 61 个点：从 (Now - 60秒) 到 Now，每秒一个点
            for (int i = 0; i < 61; i++)
            {
                DateTime pointTime = now.AddSeconds(-60 + i);

                // 根据该历史时间点生成对应的模拟值
                double value1 = ReadFromPLC(pointTime);
                double value2 = ReadFromPLC(pointTime); // 如需两条线不一样，可换随机种子逻辑

                _rt1Datas.Add((pointTime, value1));
                _rt2Datas.Add((pointTime, value2));
            }

            // 一次性刷新到控件
            z1TrendChart.UpdateData(_rt1Datas);
            z2TrendChart.UpdateData(_rt2Datas);
        }

        /// <summary>
        /// 模拟 PLC 实时读取：支持指定时间生成历史数据
        /// </summary>
        /// <param name="specificTime">指定时间，null 则使用 DateTime.Now</param>
        private double ReadFromPLC(DateTime? specificTime = null)
        {
            DateTime time = specificTime ?? DateTime.Now;
            double nowHour = time.TimeOfDay.TotalHours;

            // 1. 日周期趋势
            double trend = 15 + 5 * Math.Sin((nowHour - 6) * Math.PI / 12);

            // 2. 随机噪声
            double noise = (_rand.NextDouble() - 0.5) * 4.0;

            // 3. 偶尔尖峰
            double spike = 0;
            if (_rand.Next(0, 100) < 3)
            {
                spike = (_rand.NextDouble() - 0.5) * 10;
            }

            // 4. 缓慢漂移
            double drift = Math.Sin(time.TimeOfDay.TotalMinutes / 30.0 * Math.PI) * 2;

            double value = trend + noise + spike + drift;
            return Clamp(value, 0, 30);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void MockGetCurveChartData()
        {
            // 添加mock数据
            DateTime now = DateTime.Now;

            double value1 = ReadFromPLC(now);
            double value2 = ReadFromPLC(now);

            _rt1Datas.Add((now, value1));
            _rt2Datas.Add((now, value2));


            // 只保留最近 1 分钟（61 个点），模拟滚动显示
            if (_rt1Datas.Count > 61)
            {
                _rt1Datas.RemoveAt(0);
            }
            if (_rt2Datas.Count > 61)
            {
                _rt2Datas.RemoveAt(0);
            }

            // === 测试用：打乱顺序后传入，验证 UpdateData 内部排序 ===
            var shuffled1 = _rt1Datas.OrderBy(x => Guid.NewGuid()).ToList();
            var shuffled2 = _rt2Datas.OrderBy(x => Guid.NewGuid()).ToList();

            z1TrendChart.UpdateData(shuffled1);
            z2TrendChart.UpdateData(shuffled2);
        }

        #endregion
    }
}
