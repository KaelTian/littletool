namespace CsvHelperDemo
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper;

    // 配方实体（下游直接用）
    public class Recipe
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    // 解析器
    public static class RecipeParser
    {
        public static List<Recipe> Parse(string csvPath)
        {
            var allRows = new List<string[]>();

            // 1. 读取所有行，并把每一行拆成 string[]
            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                while (csv.Read())
                {
                    var row = new List<string>();
                    int index = 0;
                    while (csv.TryGetField(index, out string? field))
                    {
                        row.Add(field?.Trim() ?? string.Empty);
                        index++;
                    }
                    allRows.Add(row.ToArray());
                }
            }

            if (allRows.Count < 3)
                return new List<Recipe>();

            // 2. 获取最大列数（自动判断）
            int columnCount = allRows.Max(r => r.Length);

            // 3. 配方从第 6 列开始（索引 5）
            int recipeStartCol = 5;
            int recipeCount = allRows[1].Skip(recipeStartCol).TakeWhile(c => !string.IsNullOrWhiteSpace(c)).Count();

            // 4. 初始化配方
            var recipes = new List<Recipe>();
            for (int i = 0; i < recipeCount; i++)
            {
                int col = recipeStartCol + i;
                string name = col < allRows[1].Length ? allRows[1][col] : $"配方{i + 1}";
                recipes.Add(new Recipe
                {
                    RecipeId = i + 1,
                    RecipeName = name
                });
            }

            // 5. 逐行解析属性（从第3行开始）
            for (int r = 2; r < allRows.Count; r++)
            {
                var row = allRows[r];
                if (row.Length < 3) continue;

                string attrName = row[1];
                string dataType = row[2];

                for (int i = 0; i < recipeCount; i++)
                {
                    int valCol = recipeStartCol + i;
                    string valStr = valCol < row.Length ? row[valCol] : string.Empty;
                    object value = ConvertValue(valStr, dataType);
                    recipes[i].Attributes[attrName] = value;
                }
            }

            return recipes;
        }

        private static object ConvertValue(string val, string type)
        {
            try
            {
                return type.ToLower() switch
                {
                    "string" => val,
                    "int32" => int.TryParse(val, out int i) ? i : 0,
                    "float" => float.TryParse(val, out float f) ? f : 0.0f,
                    _ => val
                };
            }
            catch { return val; }
        }
    }
}
