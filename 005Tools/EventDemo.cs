namespace _005Tools
{
    public class EventDemo
    {
        public static void Main(string[] args)
        {
            var monitor = new DeviceMonitor();
            // 订阅2个事件处理方法（构建委托链表：Handler1 -> Handler2）
            monitor.DeviceStatusChanged += Handler1;
            monitor.DeviceStatusChanged += Handler2;

            // 线程A：触发事件（遍历委托链表）
            Task.Run(() => monitor.RaiseDeviceStatusChanged("设备001"));
            // 线程B：移除订阅者（修改委托链表）
            Task.Run(() =>
            {
                Console.WriteLine($"[修改线程 {Thread.CurrentThread.ManagedThreadId}] 开始移除订阅者Handler2");
                monitor.DeviceStatusChanged -= Handler2;
                Console.WriteLine($"[修改线程 {Thread.CurrentThread.ManagedThreadId}] 订阅者Handler2移除完成");
            });
            // 延时50ms，确保线程A已经开始遍历链表，再启动线程B修改链表
            Thread.Sleep(50);

   

            Console.ReadLine();
        }

        // 订阅者1的回调方法
        static void Handler1(string deviceId)
        {
            Console.WriteLine($"[回调线程 {Thread.CurrentThread.ManagedThreadId}] Handler1 收到 {deviceId} 状态变更");
        }

        // 订阅者2的回调方法
        static void Handler2(string deviceId)
        {
            Console.WriteLine($"[回调线程 {Thread.CurrentThread.ManagedThreadId}] Handler2 收到 {deviceId} 状态变更");
        }
    }
}
