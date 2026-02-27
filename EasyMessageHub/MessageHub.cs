namespace EasyMessageHub
{
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
#if NETSTANDARD
    using System.Reflection;
#endif

    /// <summary>
    /// 实现事件聚合器（Event Aggregator）模式，提供发布/订阅（Pub/Sub）消息总线功能。
    /// <para>
    /// 此类充当消息中介，允许组件之间通过消息进行松耦合通信，无需直接引用彼此。
    /// 支持全局消息拦截、错误处理、消息节流（Throttle）和类型安全的消息路由。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>线程安全说明：</para>
    /// <list type="bullet">
    ///   <item><description>订阅/取消订阅操作是线程安全的（通过内部 <see cref="Subscriptions"/> 保证）</description></item>
    ///   <item><description>发布操作（<see cref="Publish{T}"/>）允许多线程并发执行，通过快照隔离避免锁争用</description></item>
    ///   <item><description>全局处理器赋值是原子的，但非线程安全的复合操作（建议仅在初始化时设置）</description></item>
    /// </list>
    /// </remarks>
    public sealed class MessageHub : IMessageHub, IDisposable
    {
        private readonly Subscriptions _subscriptions;

        /// <summary>
        /// 全局消息处理器，在所有特定订阅之前调用。可用于日志记录、审计或消息拦截。
        /// </summary>
        /// <remarks>注意：此字段的读写不是原子复合操作，建议在单线程初始化阶段设置。</remarks>
        private Action<Type, object>? _globalHandler;

        /// <summary>
        /// 全局错误处理器，当订阅回调抛出异常时调用。
        /// </summary>
        private Action<Guid, Exception>? _globalErrorHandler;

        /// <summary>
        /// 指示实例是否已释放，用于防止 disposed 后使用。
        /// </summary>
        private volatile bool _disposed;

        /// <summary>
        /// 初始化 <see cref="MessageHub"/> 类的新实例。
        /// </summary>
        public MessageHub() => _subscriptions = new Subscriptions();

        /// <summary>
        /// 注册全局消息处理器，该处理器将在每条消息发布时被调用。
        /// </summary>
        /// <param name="onMessage">
        /// 回调委托，接收消息类型和消息对象。
        /// <para>此回调在特定类型订阅者之前调用。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">当 <paramref name="onMessage"/> 为 null 时抛出。</exception>
        /// <remarks>
        /// <para>多次调用此方法会覆盖之前的处理器（非追加）。</para>
        /// <para>建议在应用程序启动时配置，避免运行时频繁变更。</para>
        /// </remarks>
        public void RegisterGlobalHandler(Action<Type, object> onMessage)
        {
            ThrowIfNull(onMessage);
            _globalHandler = onMessage;
        }

        /// <summary>
        /// 注册全局错误处理器，当消息处理回调抛出未捕获异常时调用。
        /// </summary>
        /// <param name="onError">
        /// 回调委托，接收订阅令牌和异常对象。
        /// <para>可用于日志记录、错误恢复或断路器模式实现。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">当 <paramref name="onError"/> 为 null 时抛出。</exception>
        /// <remarks>
        /// <para>多次调用此方法会覆盖之前的处理器。</para>
        /// <para>异常被捕获后不会重新抛出，订阅者之间的异常互不影响。</para>
        /// </remarks>
        public void RegisterGlobalErrorHandler(Action<Guid, Exception> onError)
        {
            ThrowIfNull(onError);
            _globalErrorHandler = onError;
        }

        /// <summary>
        /// 向所有订阅了 <typeparamref name="T"/> 类型消息的订阅者发布消息。
        /// <para>使用快照隔离（Snapshot Isolation）确保在发布过程中对订阅列表的修改不影响当前发布。</para>
        /// </summary>
        /// <typeparam name="T">消息的类型。</typeparam>
        /// <param name="message">要发布的消息实例。</param>
        /// <exception cref="ObjectDisposedException">当 <see cref="MessageHub"/> 已释放时抛出。</exception>
        /// <remarks>
        /// <para>执行流程：</para>
        /// <list type="number">
        ///   <item><description>调用全局处理器（如果已注册）</description></item>
        ///   <item><description>获取当前订阅列表的快照（使用 <see cref="ArrayPool{T}"/> 减少 GC 压力）</description></item>
        ///   <item><description>遍历快照，根据类型匹配调用相关订阅者</description></item>
        ///   <item><description>捕获并路由每个订阅者的异常到全局错误处理器</description></item>
        /// </list>
        /// <para>性能提示：此方法使用数组池避免堆分配，但在高并发场景下仍可能产生锁争用（在订阅列表快照处）。</para>
        /// </remarks>
        public void Publish<T>(T message)
        {
            ThrowIfDisposed();

            Type msgType = typeof(T);
            _globalHandler?.Invoke(msgType, message!);

            // 使用数组池租用临时缓冲区，避免堆分配
            // 注意：Rent 返回的数组长度可能大于请求的长度（通常为2的幂次）
            Subscription[] rentedArray = ArrayPool<Subscription>.Shared.Rent(_subscriptions.Count);

            try
            {
                // 获取订阅快照（内部可能加锁，但返回后立即释放锁，允许并发发布）
                int count = _subscriptions.CopyTo(rentedArray);

                // 遍历快照而非原始集合，避免在迭代时修改集合的问题
                for (int i = 0; i < count; i++)
                {
                    ref readonly Subscription subscription = ref rentedArray[i];

                    // 类型兼容性检查：订阅者是否接受此消息类型
                    // 支持继承关系（例如订阅基类消息但接收派生类消息）
                    if (!IsAssignableFrom(subscription.Type, msgType))
                    {
                        continue;
                    }

                    try
                    {
                        subscription.Handle(message);
                    }
                    catch (Exception ex)
                    {
                        // 捕获单个订阅者的异常，避免影响其他订阅者
                        _globalErrorHandler?.Invoke(subscription.Token, ex);
                    }
                }
            }
            finally
            {
                // 必须归还数组到池中，否则会导致内存泄漏
                ArrayPool<Subscription>.Shared.Return(rentedArray, clearArray: false);
            }
        }

        /// <summary>
        /// 订阅指定类型的消息，不带节流控制。
        /// </summary>
        /// <typeparam name="T">要订阅的消息类型。</typeparam>
        /// <param name="action">消息到达时执行的回调。</param>
        /// <returns>唯一标识此订阅的 GUID 令牌，用于后续取消订阅。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出。</exception>
        public Guid Subscribe<T>(Action<T> action) => Subscribe(action, TimeSpan.Zero);

        /// <summary>
        /// 订阅指定类型的消息，带节流控制。
        /// </summary>
        /// <typeparam name="T">要订阅的消息类型。</typeparam>
        /// <param name="action">消息到达时执行的回调。</param>
        /// <param name="throttleBy">
        /// 最小消息处理间隔。如果在此间隔内收到多条消息，多余消息将被丢弃或缓冲（取决于具体实现）。
        /// <para>设置为 <see cref="TimeSpan.Zero"/> 表示不节流。</para>
        /// </param>
        /// <returns>唯一标识此订阅的 GUID 令牌。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出。</exception>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="throttleBy"/> 为负值时抛出。</exception>
        public Guid Subscribe<T>(Action<T> action, TimeSpan throttleBy)
        {
            ThrowIfNull(action);

            if (throttleBy < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(throttleBy), "节流时间间隔不能为负数。");

            return _subscriptions.Register(throttleBy, action);
        }

        /// <summary>
        /// 取消订阅指定的订阅。
        /// </summary>
        /// <param name="token">订阅时返回的 GUID 令牌。</param>
        /// <remarks>如果令牌不存在或已取消，此方法静默返回。</remarks>
        public void Unsubscribe(Guid token) => _subscriptions.UnRegister(token);

        /// <summary>
        /// 检查指定令牌是否对应活动的订阅。
        /// </summary>
        /// <param name="token">要检查的 GUID 令牌。</param>
        /// <returns>如果订阅活动则为 true；否则为 false。</returns>
        public bool IsSubscribed(Guid token) => _subscriptions.IsRegistered(token);

        /// <summary>
        /// 清除所有订阅（保留全局处理器和错误处理器）。
        /// </summary>
        /// <remarks>
        /// <para>注意：此方法不会触发任何取消订阅回调。</para>
        /// <para>正在进行的 <see cref="Publish{T}"/> 操作基于快照，不受此操作影响。</para>
        /// </remarks>
        public void ClearSubscriptions() => _subscriptions.Clear();

        /// <summary>
        /// 释放 <see cref="MessageHub"/> 使用的资源。
        /// </summary>
        /// <remarks>
        /// <para>此操作会：</para>
        /// <list type="bullet">
        ///   <item><description>清除所有订阅（调用 <see cref="ClearSubscriptions"/>）</description></item>
        ///   <item><description>解除全局处理器和错误处理器的引用</description></item>
        ///   <item><description>标记实例为已释放，后续操作将抛出 <see cref="ObjectDisposedException"/></description></item>
        /// </list>
        /// </remarks>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _subscriptions.Clear();
            _globalHandler = null;
            _globalErrorHandler = null;
        }

        #region Helper Methods

        /// <summary>
        /// 检查 <paramref name="subscriptionType"/> 是否可从 <paramref name="messageType"/> 赋值。
        /// </summary>
        /// <remarks>封装了 .NET Standard 和 .NET Core/5+ 之间的反射 API 差异。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAssignableFrom(Type subscriptionType, Type messageType)
        {
#if NETSTANDARD
        return subscriptionType.GetTypeInfo().IsAssignableFrom(messageType.GetTypeInfo());
#else
            return subscriptionType.IsAssignableFrom(messageType);
#endif
        }

        /// <summary>
        /// 验证对象不为 null，为 null 时抛出 <see cref="ArgumentNullException"/>。
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="obj"/> 为 null 时抛出。</exception>
        [DebuggerStepThrough]
        private static void ThrowIfNull([NotNull] object obj, [CallerArgumentExpression(nameof(obj))] string? paramName = null)
        {
            if (obj is null)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// 验证实例未释放，已释放时抛出 <see cref="ObjectDisposedException"/>。
        /// </summary>
        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageHub));
        }

        #endregion
    }
}
