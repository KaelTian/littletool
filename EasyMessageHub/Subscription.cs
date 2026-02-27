using System.Diagnostics;

namespace EasyMessageHub
{
    /// <summary>
    /// 表示一个带节流控制的消息订阅项。
    /// 封装了消息处理器及其元数据，支持按指定时间间隔限流处理频率。
    /// </summary>
    /// <remarks>
    /// <para><b>线程安全性：</b>此实例非线程安全。若多线程并发调用 <see cref="Handle{T}"/>，需外部同步机制。</para>
    /// <para><b>精度说明：</b>使用 <see cref="Stopwatch"/> 高精度计时器，但节流检查存在微秒级误差。</para>
    /// </remarks>
    internal sealed class Subscription
    {
        /// <summary>
        /// 节流间隔的 Ticks 值。0 表示无节流限制。
        /// </summary>
        private readonly long _throttleByTicks;

        /// <summary>
        /// 上次成功处理消息的时间戳（基于 Stopwatch.GetTimestamp()）。
        /// null 表示尚未处理过任何消息。
        /// </summary>
        /// <remarks>
        /// 使用 double 存储以支持插值计算，但存在精度损失风险（long 转 double）。
        /// </remarks>
        private double? _lastHandleTimestamp;

        /// <summary>
        /// 订阅唯一标识符，用于后续取消订阅。
        /// </summary>
        public Guid Token { get; }

        /// <summary>
        /// 订阅的消息类型（用于运行时类型匹配和过滤）。
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// 实际的消息处理器委托，存储为 object 以支持泛型擦除。
        /// 运行时需强制转换为 <see cref="Action{T}"/>。
        /// </summary>
        /// <exception cref="InvalidCastException">
        /// 若传入的 handler 类型与 Handle&lt;T&gt; 的泛型参数不匹配时抛出。
        /// </exception>
        private object Handler { get; }

        /// <summary>
        /// 初始化订阅项。
        /// </summary>
        /// <param name="type">订阅的消息类型（通常应为 typeof(T)）</param>
        /// <param name="token">唯一标识符</param>
        /// <param name="throttleBy">
        /// 节流间隔。例如 TimeSpan.FromSeconds(1) 表示每秒最多处理一次。
        /// 设为 <see cref="TimeSpan.Zero"/> 禁用节流。
        /// </param>
        /// <param name="handler">消息处理器委托，应为 Action&lt;T&gt; 类型</param>
        /// <exception cref="ArgumentNullException">type 或 handler 为 null</exception>
        public Subscription(Type type, Guid token, TimeSpan throttleBy, object handler)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Token = token;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _throttleByTicks = throttleBy.Ticks;
        }

        /// <summary>
        /// 尝试处理消息。若处于节流冷却期内，则忽略本次调用。
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <remarks>
        /// <para><b>类型安全：</b>若 T 与构造时传入的 handler 实际类型不匹配，将抛出 InvalidCastException。</para>
        /// <para><b>副作用：</b>首次调用或超过节流间隔时，会更新内部时间戳并执行 handler。</para>
        /// </remarks>
        public void Handle<T>(T message)
        {
            if (CanHandle())
            {
                // 强制转换：构造时存入的 handler 必须是 Action<T> 类型
                // 若类型不匹配，此处抛出 InvalidCastException
                ((Action<T>)Handler)(message);
            }
        }

        /// <summary>
        /// 检查当前是否允许处理（节流逻辑核心）。
        /// </summary>
        /// <returns>true 表示允许处理；false 表示处于冷却期</returns>
        /// <remarks>
        /// 算法逻辑：
        /// 1. 无节流设置（_throttleByTicks == 0）：始终允许
        /// 2. 首次处理：记录时间戳，允许处理
        /// 3. 后续处理：计算距上次的时间差，若超过阈值则允许并更新时间戳
        /// </remarks>
        private bool CanHandle()
        {
            // 情况1：未设置节流，直接放行
            if (_throttleByTicks == 0)
            {
                return true;
            }

            // 情况2：首次处理，无需节流检查
            if (_lastHandleTimestamp == null)
            {
                _lastHandleTimestamp = Stopwatch.GetTimestamp();
                return true;
            }

            // 计算时间差并转换单位
            long now = Stopwatch.GetTimestamp();

            // 更准确的计算：将 Stopwatch ticks 转换为 TimeSpan ticks
            double stopwatchTicksPerTimeSpanTick = (double)Stopwatch.Frequency / TimeSpan.TicksPerSecond;
            double elapsedTimeSpanTicks = (now - _lastHandleTimestamp.Value) / stopwatchTicksPerTimeSpanTick;

            if (elapsedTimeSpanTicks >= _throttleByTicks)
            {
                _lastHandleTimestamp = now;
                return true;
            }

            return false;
        }
    }
}
