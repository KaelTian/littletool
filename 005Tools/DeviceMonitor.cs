namespace _005Tools
{
    public class DeviceMonitor
    {
        // 定义事件（委托链表，存储订阅者回调）
        public event Action<string> DeviceStatusChanged;

        // 触发事件的方法（不做线程安全处理，直接触发）
        public void RaiseDeviceStatusChanged(string deviceId)
        {
            // 直接遍历事件对应的委托链表（存在线程安全问题）
            if (DeviceStatusChanged != null)
            {
                DeviceStatusChanged.Invoke(deviceId);
                //Console.WriteLine($"[触发线程 {Thread.CurrentThread.ManagedThreadId}] 开始遍历委托链表，触发事件");
                //// 手动遍历委托链表（模拟CLR底层触发逻辑，更易复现问题）
                //foreach (var handler in DeviceStatusChanged.GetInvocationList())
                //{
                //    (handler as Action<string>).Invoke(deviceId);
                //    // 故意添加延时，放大并发冲突的概率
                //    Thread.Sleep(1000);
                //}
                //Console.WriteLine($"[触发线程 {Thread.CurrentThread.ManagedThreadId}] 委托链表遍历完成");
            }
        }
    }
}
