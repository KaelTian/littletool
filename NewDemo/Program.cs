//using NewDemo;

//var monitor = new DeviceMonitor();
//monitor.Subscribe(Handler1);
//monitor.Subscribe(Handler2);

//// 线程 A：遍历
//Task.Run(() => monitor.RaiseDeviceStatusChanged("设备001"));

//Thread.Sleep(50);

//// 线程 B：修改
//Task.Run(() =>
//{
//    Console.WriteLine($"[修改线程 {Thread.CurrentThread.ManagedThreadId}] 移除 Handler2");
//    monitor.Unsubscribe(Handler2);
//    Console.WriteLine($"[修改线程 {Thread.CurrentThread.ManagedThreadId}] 移除完成");
//});

//Console.ReadLine();

//void Handler1(string id) => Console.WriteLine($"Handler1 收到 {id}");
//void Handler2(string id) => Console.WriteLine($"Handler2 收到 {id}");


try
{

    //User? a = new User { Name = "Alice", Age = 30 };
    //Console.WriteLine($"调用 ClearUser 前: Name={a.Name}, Age={a.Age}");
    //ClearUser(ref a);
    //Console.WriteLine($"调用 ClearUser 后: Name={a?.Name}, Age={a?.Age}");

    //if (int.TryParse("3.00", out int i1))
    //{
    //    Console.WriteLine($"解析成功: {i1}");
    //}
    //else
    //{
    //    Console.WriteLine("解析失败");
    //}

    // 使用
    if (TryParseIntFlexible("3.00", out int i1))
        Console.WriteLine($"成功: {i1}");   // 输出 3

    if (TryParseIntFlexible("3", out int i2))
        Console.WriteLine($"成功: {i2}");   // 输出 3

    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"发生异常: {ex.Message}");
}

bool TryParseIntFlexible(string input, out int result)
{
    result = 0;
    if (string.IsNullOrWhiteSpace(input)) return false;

    // decimal 既能解析 "3" 也能解析 "3.000"，且无精度损失
    if (decimal.TryParse(input, out decimal d))
    {
        result = (int)d;  // 默认截断小数位
        return true;
    }
    return false;
}

void ClearUser(ref User? user)
{
    if (user != null)
    {
        user = null;
    }
}

class User
{
    public string? Name { get; set; }
    public int Age { get; set; }
}