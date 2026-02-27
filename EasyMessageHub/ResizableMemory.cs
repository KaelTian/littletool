namespace EasyMessageHub
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// 一个可自动扩容的内存缓冲区，专门用于存储 <see cref="Subscription"/> 对象。
    /// <para>特性说明：</para>
    /// <list type="bullet">
    ///   <item><description>采用"交换并弹出"(swap-and-pop)策略实现 O(1) 删除，但会破坏元素顺序</description></item>
    ///   <item><description>非线程安全，多线程环境需外部同步</description></item>
    ///   <item><description>使用2倍扩容策略，均摊时间复杂度 O(1)</description></item>
    /// </list>
    /// </summary>
    internal sealed class ResizableMemory : IEnumerable<Subscription>
    {
        private int count;
        private Subscription[] memory;

        /// <summary>
        /// 初始化 <see cref="ResizableMemory"/> 类的新实例。
        /// </summary>
        /// <param name="initialCapacity">初始容量，必须大于 0。默认为 100。</param>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="initialCapacity"/> 小于或等于 0 时抛出。</exception>
        public ResizableMemory(int initialCapacity = 100)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "初始容量必须大于 0。");

            memory = new Subscription[initialCapacity];
        }

        /// <summary>
        /// 获取当前包含的元素数量。
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 获取当前内部数组的容量（在不扩容的情况下可容纳的最大元素数）。
        /// </summary>
        public int Capacity => memory.Length;

        /// <summary>
        /// 获取或设置指定索引处的元素。
        /// </summary>
        /// <param name="index">要获取或设置的元素从零开始的索引。</param>
        /// <returns>指定索引处的 <see cref="Subscription"/>。</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 超出有效范围时抛出。</exception>
        public Subscription this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)count)
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
                return memory[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)count)
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
                memory[index] = value;
            }
        }

        /// <summary>
        /// 将元素添加到集合末尾。如果内部数组已满，则自动扩容。
        /// <para>时间复杂度：均摊 O(1)</para>
        /// </summary>
        /// <param name="item">要添加的 <see cref="Subscription"/> 对象。</param>
        public void Add(Subscription item)
        {
            // 确保容量充足，触发扩容如果需要
            if (count == memory.Length)
            {
                Resize();
            }
            memory[count++] = item;
        }

        /// <summary>
        /// 确保内部数组至少具有指定的容量。
        /// </summary>
        /// <param name="capacity">所需的最小容量。</param>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="capacity"/> 为负数时抛出。</exception>
        /// <exception cref="OutOfMemoryException">当系统内存不足无法分配时抛出。</exception>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (memory.Length < capacity)
            {
                Grow(capacity);
            }
        }

        /// <summary>
        /// 通过 Guid 标识符移除指定的订阅。
        /// <para>注意：此操作使用"交换并弹出"策略，将最后一个元素移动到被删除位置以保持 O(1) 性能，这会改变集合中元素的顺序。</para>
        /// <para>时间复杂度：O(n)，用于搜索元素</para>
        /// </summary>
        /// <param name="token">要移除的订阅的 Guid 标识符。</param>
        /// <returns>如果成功找到并移除元素，则为 true；否则为 false。</returns>
        public bool Remove(Guid token)
        {
            for (int i = 0; i < count; i++)
            {
                if (memory[i].Token == token)
                {
                    // 计算最后一个元素的索引
                    int lastIndex = count - 1;

                    // 只有当被删除元素不是最后一个时，才需要交换
                    // 这避免了不必要的引用赋值，对引用类型尤其重要
                    if (i != lastIndex)
                    {
                        memory[i] = memory[lastIndex];
                    }

                    // 清除最后一个位置的引用，帮助 GC（如果是引用类型）
                    memory[lastIndex] = default!;
                    count--;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 确定集合是否包含具有指定 Guid 的订阅。
        /// <para>时间复杂度：O(n)</para>
        /// </summary>
        /// <param name="token">要查找的 Guid 标识符。</param>
        /// <returns>如果找到指定 Guid，则为 true；否则为 false。</returns>
        public bool Contains(Guid token)
        {
            // 使用 Span 优化循环性能，启用边界检查消除
            ReadOnlySpan<Subscription> span = memory.AsSpan(0, count);
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Token == token)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 从集合中移除所有元素，并将计数重置为 0。
        /// <para>注意：此方法会清除内部数组中的引用以帮助 GC，但保留当前容量。</para>
        /// </summary>
        public void Clear()
        {
            if (count > 0)
            {
                // 清除范围内的引用，帮助垃圾回收器回收对象
                // AsSpan(0, count).Clear() 在 .NET Core 2.1+ 中经过高度优化
                memory.AsSpan(0, count).Clear();
                count = 0;
            }
        }

        /// <summary>
        /// 将集合中的元素复制到指定的 <see cref="Span{T}"/>。
        /// </summary>
        /// <param name="destination">目标 Span，其长度必须至少等于 <see cref="Count"/>。</param>
        /// <returns>实际复制的元素数量（即 <see cref="Count"/>）。</returns>
        /// <exception cref="ArgumentException">当目标 Span 长度小于当前元素数量时抛出。</exception>
        public int CopyTo(Span<Subscription> destination)
        {
            if (destination.Length < count)
            {
                throw new ArgumentException("目标 Span 长度不足以容纳所有元素。", nameof(destination));
            }

            memory.AsSpan(0, count).CopyTo(destination);
            return count;
        }

        /// <summary>
        /// 将当前容量设置为与实际元素数量相等（如果当前容量大于元素数量）。
        /// <para>如果当前容量等于元素数量，则不执行任何操作。</para>
        /// <para>注意：此方法会分配新的内部数组并复制元素。</para>
        /// </summary>
        public void TrimExcess()
        {
            if (count < memory.Length)
            {
                Subscription[] newMemory = new Subscription[count];
                Array.Copy(memory, newMemory, count);
                memory = newMemory;
            }
        }

        /// <summary>
        /// 将内部数组容量翻倍以容纳更多元素。
        /// </summary>
        private void Resize()
        {
            // 计算新容量，处理溢出情况
            // Array.MaxLength 是 .NET 6+ 的常量，此处使用硬编码值以保持兼容性
            const int ArrayMaxLength = 0X7FEFFFFF;

            int currentLength = memory.Length;
            int newSize = currentLength == 0 ? 4 : currentLength * 2;

            // 检查溢出或超过数组最大长度限制
            if ((uint)newSize > ArrayMaxLength)
            {
                newSize = ArrayMaxLength;
            }

            if (newSize == currentLength)
            {
                throw new OutOfMemoryException("无法进一步扩容数组，已达到最大限制。");
            }

            Grow(newSize);
        }

        /// <summary>
        /// 将内部数组扩容到指定大小。
        /// </summary>
        /// <param name="newSize">新的数组大小。</param>
        private void Grow(int newSize)
        {
            Subscription[] newMemory = new Subscription[newSize];
            Array.Copy(memory, newMemory, count); // 只复制有效数据，而非整个数组
            memory = newMemory;
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// </summary>
        public IEnumerator<Subscription> GetEnumerator()
        {
            // 直接遍历数组，避免 Span 的 yield 限制
            for (int i = 0; i < count; i++)
            {
                yield return memory[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 内部辅助类，用于抛出异常（避免在热路径中产生代码膨胀）。
    /// </summary>
    internal static class ThrowHelper
    {
        public static void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
