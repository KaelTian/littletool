namespace EasyMessageHub
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// 线程安全的订阅管理器，提供对 <see cref="ResizableMemory"/> 的并发访问封装。
    /// <para>
    /// 此类通过粗粒度锁定（Coarse-Grained Locking）策略保证线程安全，
    /// 适用于订阅/取消订阅操作不极端频繁（非高并发写入）的场景。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>性能特征：</para>
    /// <list type="bullet">
    ///   <item><description>读操作（<see cref="IsRegistered"/>）：O(n)，持有锁期间线性搜索</description></item>
    ///   <item><description>写操作（<see cref="Register"/>/<see cref="UnRegister"/>）：O(1) ~ O(n)，均摊 O(1)</description></item>
    ///   <item><description>快照操作（<see cref="CopyTo"/>）：O(n)，一次性复制所有订阅</description></item>
    /// </list>
    /// <para>线程安全：此类所有公共成员都是线程安全的，可多线程并发访问。</para>
    /// </remarks>
    internal sealed class Subscriptions
    {
        // 使用专门的锁对象而非 ResizableMemory 实例本身，避免外部锁定导致的死锁
        private readonly object _syncRoot = new();
        private readonly ResizableMemory _subscriptions = new();

        /// <summary>
        /// 获取当前注册的订阅数量。
        /// </summary>
        /// <value>当前活动的订阅总数。</value>
        /// <remarks>此操作在锁保护下读取计数字段，是原子的。</remarks>
        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _subscriptions.Count;
                }
            }
        }

        /// <summary>
        /// 注册一个新的订阅。
        /// </summary>
        /// <typeparam name="T">订阅处理的消息类型。</typeparam>
        /// <param name="throttleBy">节流时间间隔，用于控制消息处理频率。</param>
        /// <param name="action">消息到达时执行的回调操作。</param>
        /// <returns>唯一标识此订阅的 GUID 令牌，用于后续注销操作。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出。</exception>
        /// <remarks>
        /// <para>此方法线程安全，可在多线程环境下并发调用。</para>
        /// <para>生成的 GUID 使用 <see cref="Guid.NewGuid()"/>，具有全局唯一性。</para>
        /// </remarks>
        public Guid Register<T>(TimeSpan throttleBy, Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            Type type = typeof(T);
            Guid token = Guid.NewGuid();

            // 在堆栈上创建订阅对象，避免在锁内执行复杂构造逻辑
            Subscription subscription = new(type, token, throttleBy, action);

            lock (_syncRoot)
            {
                _subscriptions.Add(subscription);
            }

            return token;
        }

        /// <summary>
        /// 检查指定令牌对应的订阅是否已注册。
        /// </summary>
        /// <param name="token">要检查的订阅令牌。</param>
        /// <returns>如果找到对应的订阅，则为 true；否则为 false。</returns>
        /// <remarks>此方法会在线性搜索期间持有锁，对于大量订阅可能产生竞争。</remarks>
        public bool IsRegistered(Guid token)
        {
            lock (_syncRoot)
            {
                return _subscriptions.Contains(token);
            }
        }

        /// <summary>
        /// 将所有当前订阅复制到指定的缓冲区。
        /// <para>这是一个快照操作，返回的是调用瞬间的订阅集合视图。</para>
        /// </summary>
        /// <param name="buffer">
        /// 目标缓冲区，长度必须至少等于 <see cref="Count"/>。
        /// 建议使用 <see cref="GetSnapshot"/> 获取精确大小的缓冲区，或先查询 Count 预分配。
        /// </param>
        /// <returns>实际复制的订阅数量。</returns>
        /// <exception cref="ArgumentException">当 buffer 长度小于当前订阅数量时抛出。</exception>
        /// <remarks>
        /// <para>此操作在锁保护下执行复制，保证线程安全。</para>
        /// <para>注意：返回的订阅是值类型副本，修改副本不会影响内部存储。</para>
        /// <para>对于频繁调用，建议使用 <see cref="GetSnapshot"/> 避免手动管理缓冲区大小。</para>
        /// </remarks>
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach",
            Justification = "使用 for 循环避免迭代器分配，在高频调用场景下性能更优")]
        public int CopyTo(Span<Subscription> buffer)
        {
            lock (_syncRoot)
            {
                return _subscriptions.CopyTo(buffer);
            }
        }

        /// <summary>
        /// 获取当前所有订阅的快照数组。
        /// <para>这是 <see cref="CopyTo"/> 的便捷封装，自动管理缓冲区分配。</para>
        /// </summary>
        /// <returns>包含所有当前订阅的数组。如果无订阅，返回空数组而非 null。</returns>
        /// <remarks>
        /// <para>注意：此方法在堆上分配新数组。如果追求零分配，请使用 <see cref="CopyTo"/>。</para>
        /// <para>数组内容为调用瞬间的快照，后续对管理器的修改不会影响此数组。</para>
        /// </remarks>
        public Subscription[] GetSnapshot()
        {
            lock (_syncRoot)
            {
                int count = _subscriptions.Count;
                if (count == 0)
                    return Array.Empty<Subscription>();

                Subscription[] snapshot = new Subscription[count];
                _subscriptions.CopyTo(snapshot.AsSpan());
                return snapshot;
            }
        }

        /// <summary>
        /// 注销指定令牌的订阅。
        /// </summary>
        /// <param name="token">要注销的订阅令牌。</param>
        /// <remarks>
        /// <para>如果未找到指定令牌，此方法静默返回（无异常）。</para>
        /// <para>此操作使用"交换并弹出"策略，时间复杂度 O(n) 用于搜索，O(1) 用于删除。</para>
        /// <para>注意：删除操作会改变内部存储顺序（将最后一个元素移至被删位置）。</para>
        /// </remarks>
        public void UnRegister(Guid token)
        {
            lock (_syncRoot)
            {
                _subscriptions.Remove(token);
            }
        }

        /// <summary>
        /// 清空所有订阅。
        /// </summary>
        /// <remarks>
        /// 此方法会清空内部缓冲区并重置计数，但保留当前容量（不释放内存）。
        /// 如需释放内存，请在使用此方法后手动调用（如暴露 TrimExcess 方法）。
        /// </remarks>
        public void Clear()
        {
            lock (_syncRoot)
            {
                _subscriptions.Clear();
            }
        }

        /// <summary>
        /// 尝试注销指定令牌的订阅。
        /// </summary>
        /// <param name="token">要注销的订阅令牌。</param>
        /// <returns>如果成功找到并移除订阅，则为 true；否则为 false。</returns>
        public bool TryUnRegister(Guid token)
        {
            lock (_syncRoot)
            {
                return _subscriptions.Remove(token);
            }
        }
    }
}
