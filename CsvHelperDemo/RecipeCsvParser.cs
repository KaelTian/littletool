namespace CsvHelperDemo
{
    using CsvHelper;
    using CsvHelper.Configuration;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class RecipeCsvParser
    {
        public static (List<RecipeParameter> parameters, List<Recipe1> recipes) Parse(string filePath)
        {
            var encoding = DetectEncoding(filePath);

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = false,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            // 第一步：把所有行读成 string[]，保留原始列索引
            var rows = new List<string[]>();
            using (var reader = new StreamReader(filePath, encoding))
            using (var csv = new CsvReader(reader, config))
            {
                while (csv.Read())
                {
                    var row = new string[csv.Parser.Count];
                    for (int i = 0; i < csv.Parser.Count; i++)
                        row[i] = csv.GetField(i) ?? "";
                    rows.Add(row);
                }
            }

            if (rows.Count < 3)
                throw new InvalidDataException("文件至少需要 3 行：标题行、配方名称行、数据行");

            const int MetaColumnCount = 5;      // A~E 列是元数据
            int recipeStartIndex = MetaColumnCount; // F 列，索引 5

            // ================== 关键修正 ==================
            // 记录每个配方所在的【绝对列索引】和配方名称
            // 即使配方名称为空，也保留列位，防止后续数据错位
            var nameRow = rows[1];
            var recipeColumns = new List<(int ColumnIndex, string RecipeName)>();

            for (int col = recipeStartIndex; col < nameRow.Length; col++)
            {
                var name = nameRow[col]?.Trim();
                // 空名称用 null 占位，但列索引必须保留
                recipeColumns.Add((col, string.IsNullOrEmpty(name) ? string.Empty : name));
            }

            var parameters = new List<RecipeParameter>();

            // 从第三行（索引 2）开始解析参数数据
            for (int r = 2; r < rows.Count; r++)
            {
                var row = rows[r];

                // 健壮性判断：A列必须以"项"开头（项1、项2...）才是有效数据行
                // 同时如果行长度连元数据都不够，直接跳过
                if (row.Length == 0 ||
                    string.IsNullOrWhiteSpace(row[0]) ||
                    !row[0].Trim().StartsWith("项"))
                    continue;

                if (row.Length < MetaColumnCount)
                    continue; // 元数据列缺失，跳过

                var param = new RecipeParameter
                {
                    ItemNo = row[0]?.Trim(),
                    PropertyName = row[1]?.Trim(),
                    DataType = row[2]?.Trim(),
                    Unit1 = row[3]?.Trim(),
                    Unit2 = row[4]?.Trim()
                };

                // 按绝对列索引取值，一一对应，绝不错位
                foreach (var (colIndex, recipeName) in recipeColumns)
                {
                    var value = colIndex < row.Length ? (row[colIndex]?.Trim() ?? "") : "";

                    // 用配方名做 Key；如果该列配方名为空，用占位符避免覆盖其他列
                    var key = recipeName ?? $"__EMPTY_COL_{colIndex}__";
                    param.RecipeValues[key] = value;
                }

                parameters.Add(param);
            }

            // 转置为“按配方组织”的纵向视图（下游最常用）
            var recipes = new List<Recipe1>();
            foreach (var (colIndex, recipeName) in recipeColumns)
            {
                if (string.IsNullOrEmpty(recipeName)) continue; // 跳过无名空列

                var recipe = new Recipe1 { RecipeName = recipeName };
                foreach (var param in parameters)
                {
                    recipe.Properties.Add(new RecipeProperty
                    {
                        ItemNo = param.ItemNo,
                        Name = param.PropertyName,
                        DataType = param.DataType,
                        Unit1 = param.Unit1,
                        Unit2 = param.Unit2,
                        RawValue = param.RecipeValues.GetValueOrDefault(recipeName, "")
                    });
                }
                recipes.Add(recipe);
            }

            return (parameters, recipes);
        }

        private static Encoding DetectEncoding(string path)
        {
            using var reader = new StreamReader(path, Encoding.Default, true);
            reader.Read();
            return reader.CurrentEncoding;
        }
    }
}
