using NewDemo;

var monitor = new DeviceMonitor();
monitor.Subscribe(Handler1);
monitor.Subscribe(Handler2);

// 线程 A：遍历
Task.Run(() => monitor.RaiseDeviceStatusChanged("设备001"));

Thread.Sleep(50);

// 线程 B：修改
Task.Run(() =>
{
    Console.WriteLine($"[修改线程 {Thread.CurrentThread.ManagedThreadId}] 移除 Handler2");
    monitor.Unsubscribe(Handler2);
    Console.WriteLine($"[修改线程 {Thread.CurrentThread.ManagedThreadId}] 移除完成");
});

Console.ReadLine();

void Handler1(string id) => Console.WriteLine($"Handler1 收到 {id}");
void Handler2(string id) => Console.WriteLine($"Handler2 收到 {id}");