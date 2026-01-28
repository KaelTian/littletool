using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _005Tools
{
    /// <summary>
    /// 激光操作数据模型
    /// </summary>
    public class JiGuangOperationModel
    {
        /// <summary>
        /// 操作类型
        /// LASERQR_Change 激光状态修改  
        /// Recipe_Switch 配方切换 
        /// Machining_Mode 加工模式  
        /// Recipe_Change 修改当前配方
        /// </summary>
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        /// <summary>
        /// 操作数据字典
        /// </summary>
        [JsonPropertyName("Data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 将对象序列化为JSON字符串
        /// </summary>
        /// <returns>JSON格式的字符串</returns>
        public string SerializeToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // 允许中文字符
            };

            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// 从JSON字符串反序列化为对象
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>JiGuangOperationModel对象</returns>
        public static JiGuangOperationModel? DeserializeFromJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 先反序列化
            var model = JsonSerializer.Deserialize<JiGuangOperationModel>(json, options);

            // 处理 Data 字典中的 JsonElement
            if (model?.Data != null)
            {
                model.ConvertJsonElementsToRealTypes();
            }

            return model;
        }

        /// <summary>
        /// 将字典中的 JsonElement 转换为实际类型
        /// </summary>
        private void ConvertJsonElementsToRealTypes()
        {
            var newData = new Dictionary<string, object>();

            foreach (var kvp in Data)
            {
                if (kvp.Value is JsonElement jsonElement)
                {
                    newData[kvp.Key] = ConvertJsonElementToObject(jsonElement)!;
                }
                else
                {
                    newData[kvp.Key] = kvp.Value;
                }
            }

            Data = newData;
        }

        /// <summary>
        /// 将 JsonElement 转换为对应的 C# 对象
        /// </summary>
        private object? ConvertJsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number =>
                    element.TryGetDouble(out double doubleValue) ? doubleValue :
                    element.TryGetInt32(out int intValue) ? intValue :
                    element.TryGetInt64(out long longValue) ? longValue :
                    (object)element.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => ConvertJsonObjectToDictionary(element),
                JsonValueKind.Array => ConvertJsonArrayToList(element),
                _ => element.GetRawText()
            };
        }

        /// <summary>
        /// 将 JsonElement 对象转换为字典
        /// </summary>
        private Dictionary<string, object> ConvertJsonObjectToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonElementToObject(property.Value)!;
            }
            return dict;
        }

        /// <summary>
        /// 将 JsonElement 数组转换为列表
        /// </summary>
        private List<object> ConvertJsonArrayToList(JsonElement element)
        {
            var list = new List<object>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(ConvertJsonElementToObject(item)!);
            }
            return list;
        }

        /// <summary>
        /// 创建包含示例数据的激光操作实例
        /// </summary>
        /// <returns>JiGuangOperationModel实例</returns>
        public static JiGuangOperationModel CreateExample()
        {
            return new JiGuangOperationModel
            {
                Type = "LASERQR_Change",
                Data = new Dictionary<string, object>
            {
                { "MES_QRLaserPulseWidth", 12.2 },
                { "MES_QRLaserFrequency", 1.3 },
                { "MES_QRActiveRecipe", "当前加工模板" },
                { "MES_QRGlassModel", "玻璃型号" },
                { "MES_QRID", "编辑序列号" },
                { "MES_ISProcessMode",true },
                { "MES_Recipe_CleanSideSpeed_0Deg","400ns" }
            }
            };
        }

        /// <summary>
        /// 便捷方法：获取数据值（带类型转换）
        /// </summary>
        public T? GetDataValue<T>(string key, T? defaultValue = default)
        {
            if (Data != null && Data.ContainsKey(key) && Data[key] is T value)
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// 便捷方法：设置数据值
        /// </summary>
        public void SetDataValue<T>(string key, T value)
        {
            if (Data == null)
                Data = new Dictionary<string, object>();

            Data[key] = value!;
        }

    }
}
