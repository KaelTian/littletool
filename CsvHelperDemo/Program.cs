//// 设备商共享出来的路径，直接用 UNC 格式
//using CsvHelperDemo;

using CsvHelperDemo;

string csvPath = @"D:\works\005\ald\recipe.csv";

//// 解析
//var (parameters, recipes) = RecipeCsvParser.Parse(csvPath);

//// ---------------- 下游用法 1：按配方名提取单个配方 ----------------
//var recipeAld = recipes.FirstOrDefault(r => r.RecipeName == "ALO");
//if (recipeAld != null)
//{
//    foreach (var prop in recipeAld.Properties)
//    {
//        Console.WriteLine($"{prop.ItemNo,-5} {prop.Name,-20} ({prop.DataType,-6}) = {prop.RawValue}");
//    }
//}

//// ---------------- 下游用法 2：查某个属性在所有配方中的值 ----------------
//var nStepType1 = parameters.First(p => p.PropertyName == "nStepType1");
//foreach (var kvp in nStepType1.RecipeValues)
//{
//    Console.WriteLine($"配方 [{kvp.Key}] 的 nStepType1 = {kvp.Value}");
//}

//// ---------------- 下游用法 3：序列化成 Dictionary / JSON ----------------
//// 适合传给 Web API 或写入数据库
//var dict = recipes.ToDictionary(
//    r => r.RecipeName!,
//    r => r.Properties!.ToDictionary(
//        p => p.Name!,
//        p => p.TypedValue   // 自动按 String/Int32/Float 转换
//    )
//);
//// 此时 dict["Ald"]["nStepType1"] 是 int 类型 1


///

var recipes = RecipeParser.Parse(csvPath);

Console.WriteLine($"解析完成：{recipes.Count} 个配方");
foreach (var r in recipes)
{
    Console.WriteLine($"\n配方{r.RecipeId}：{r.RecipeName}");
    foreach (var kv in r.Attributes)
        Console.WriteLine($"  {kv.Key} = {kv.Value}");
}

Console.ReadLine();