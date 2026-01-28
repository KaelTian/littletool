namespace _005Tools
{
    public static class RefAndOutExample
    {
        public static void Run()
        {
            int number = 5;
            Console.WriteLine($"Before RefMethod: {number}");
            RefMethod(ref number);
            Console.WriteLine($"After RefMethod: {number}");
            RefNotSetMethod(ref number); // This works because 'number' is initialized
            Console.WriteLine($"RefNotSetMethod: {number}");
            
            var boolResult = TryParseInt("123", out int parsedValue);
            Console.WriteLine($"TryParseInt success: {boolResult}, parsed value: {parsedValue}");
            var boolResultFail = TryParseInt("abc", out int parsedValueFail);
            Console.WriteLine($"TryParseInt failed: {boolResultFail}, parsed value: {parsedValueFail}");

            int result;
            OutMethod(out result);
            Console.WriteLine($"After OutMethod: {result}");
        }
        private static void RefMethod(ref int num)
        {
            num += 10;
        }
        private static void OutMethod(out int num)
        {
            num = 20;
        }

        private static void RefNotSetMethod(ref int num)
        {
            // This will cause a compile-time error if 'num' is not initialized before being passed.
            Console.WriteLine("Ref number not set: " + num);
        }

        //private static void OutNotSetMethod(out int num)
        //{
        //    num = 30; // Must assign a value before exiting the method
        //    // This will cause a compile-time error if 'num' is not assigned before the method ends.
        //    Console.WriteLine("Out number is set: " + num); // Uncommenting this line will cause an error
        //}

        /// <summary>
        /// 解析字符串为整数，返回是否成功（bool），并通过 out 传出解析后的值
        /// </summary>
        /// <param name="input"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        static bool TryParseInt(string input, out int result)
        {
            // 必须给 out 参数赋值（无论分支如何）
            if (int.TryParse(input, out int temp))
            {
                result = temp;
                return true;
            }
            else
            {
                result = 0; // 即使失败也要赋值
                return false;
            }
        }
    }
}
