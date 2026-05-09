namespace CsvHelperDemo
{
    using System.Collections.Generic;

    /// <summary>
    /// 横向视角：每个参数在各配方中的值
    /// </summary>
    public class RecipeParameter
    {
        public string? ItemNo { get; set; }        // 项1, 项2...
        public string? PropertyName { get; set; }  // Name, nStepType1...
        public string? DataType { get; set; }      // string?, Int32, Float
        public string? Unit1 { get; set; }         // \\local\...
        public string? Unit2 { get; set; }         // \\local\...

        // 配方名 -> 原始值（字符串）
        public Dictionary<string, string> RecipeValues { get; set; } = new();
    }

    /// <summary>
    /// 纵向视角：单个配方下的所有属性（通常下游更喜欢这种）
    /// </summary>
    public class Recipe1
    {
        public string? RecipeName { get; set; }
        public List<RecipeProperty> Properties { get; set; } = new();
    }

    public class RecipeProperty
    {
        public string? ItemNo { get; set; }
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public string? Unit1 { get; set; }
        public string? Unit2 { get; set; }

        public string? RawValue { get; set; }

        // 按需转换，容错处理（如 "200+2000" 转 Float 失败会保留原字符串）
        public object? TypedValue => ConvertValue(RawValue, DataType);

        private static object? ConvertValue(string? val, string? type)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            return type?.ToLower() switch
            {
                "int32" => int.TryParse(val, out var i) ? i : val,
                "float" => float.TryParse(val, out var f) ? f : val,
                _ => val
            };
        }
    }
}
