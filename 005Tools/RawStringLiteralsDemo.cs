using System.Text.RegularExpressions;

namespace _005Tools
{
    /// <summary>
    /// 原始字符串字面量
    /// </summary>
    internal static class RawStringLiteralsDemo
    {
        public static void LiteralsDemo()
        {
            // 使用传统字符串字面量表示多行文本
            string traditional = "第一行文本\n" +
                                 "第二行文本，包含\"引号\"\n" +
                                 "第三行文本，路径示例：C:\\Users\\Name\\Documents";
            Console.WriteLine("传统字符串字面量:");
            Console.WriteLine(traditional);
            Console.WriteLine();
            // 使用原始字符串字面量表示多行文本
            string rawLiteral = """
                第一行文本
                第二行文本，包含"引号"
                第三行文本，路径示例：C:\Users\Name\Documents
                """;
            Console.WriteLine("原始字符串字面量:");
            Console.WriteLine(rawLiteral);
            Console.WriteLine();
            // 使用原始字符串字面量表示带缩进的代码块
            string codeBlock = """
                public void HelloWorld()
                {
                    Console.WriteLine("Hello, World!");
                }
                """;
            Console.WriteLine("代码块示例:");
            Console.WriteLine(codeBlock);

            string basic = """这是一个原始字符串，不需要转义 "引号" 和 \反斜杠   """;

            // JSON 示例
            string json = """
                {
                    "name": "Abby Zhang",
                    "age": 36,
                    "address": "YLHW"
                }
                """;
            string json1 = """"
                        {
                        "name":"Kael Tian",
                        "age":28,
                        "address":"YLHW"
                        }
                """";
            Console.WriteLine(basic);
            Console.WriteLine("JSON 示例:");
            Console.WriteLine(json);
            Console.WriteLine("JSON1 示例:");
            Console.WriteLine(json1);
            // 复杂的正则表达式（无需转义反斜杠）
            string emailPattern = """^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$""";
            string phonePattern = """^(\+\d{1,3}\s?)?(\(\d{1,4}\)\s?)?\d{1,4}[\s.-]?\d{1,4}[\s.-]?\d{1,9}$""";

            // 使用正则表达式
            bool isValidEmail = Regex.IsMatch("test@example.com", emailPattern);
            bool isValidPhone = Regex.IsMatch("+1 (555) 123-4567", phonePattern);

            Console.WriteLine($"电子邮件格式有效: {isValidEmail}");
            Console.WriteLine($"电话号码格式有效: {isValidPhone}");
        }
    }
}
