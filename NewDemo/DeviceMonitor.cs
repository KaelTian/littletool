using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewDemo
{
    public class DeviceMonitor
    {
        // 真正的集合——List<T>
        private readonly List<Action<string>> _handlers = new();
        private readonly object _lock = new();

        public void Subscribe(Action<string> handler)
        {
            lock (_lock) _handlers.Add(handler);
        }
        public void Unsubscribe(Action<string> handler)
        {
            lock (_lock) _handlers.Remove(handler);
        }

        // 故意不加锁，直接遍历
        public void RaiseDeviceStatusChanged(string deviceId)
        {
            Console.WriteLine($"[触发线程 {Thread.CurrentThread.ManagedThreadId}] 开始遍历 List");
            foreach (var h in _handlers)   // 这里会抛异常
            {
                h(deviceId);
                Thread.Sleep(100);
            }
            Console.WriteLine($"[触发线程 {Thread.CurrentThread.ManagedThreadId}] 遍历完成");
        }
    }

}
