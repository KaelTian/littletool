using System.Collections;
using System.Reflection;
using System.Text;

namespace _005Tools
{
    /// <summary>
    /// 通用测试数据生成器（支持复杂嵌套结构）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TestGenericDataGenetor<T> where T : class, new()
    {
        // 全局Random避免短时间内生成相同随机数
        private static readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// 生成测试数据对象（支持复杂嵌套结构）
        /// </summary>
        /// <param name="messageAction">日志输出委托（可选）</param>
        /// <param name="maxListCount">List集合最大生成数量（默认1-5条）</param>
        /// <returns>赋值完成的测试对象</returns>
        public static T GenerateTestData(Action<string>? messageAction = null, int maxListCount = 5)
        {
            var instance = new T();
            GenerateObjectData(instance, messageAction, maxListCount);
            return instance;
        }

        /// <summary>
        /// 递归生成对象数据（核心：支持嵌套对象/集合）
        /// </summary>
        private static void GenerateObjectData(object instance, Action<string>? messageAction, int maxListCount)
        {
            if (instance == null) return;

            var type = instance.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // 跳过只读属性
                if (!prop.CanWrite) continue;

                try
                {
                    // 根据属性类型生成对应值（支持集合、嵌套对象、基础类型）
                    object? testValue = GeneratePropertyValue(prop, messageAction, maxListCount);
                    prop.SetValue(instance, testValue);
                }
                catch (Exception ex)
                {
                    messageAction?.Invoke($"【警告】属性 {type.Name}.{prop.Name} 赋值失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 核心逻辑：根据属性特征生成测试值（适配复杂结构+可空类型）
        /// </summary>
        private static object? GeneratePropertyValue(PropertyInfo prop, Action<string>? messageAction, int maxListCount)
        {
            Type propType = prop.PropertyType;
            string propName = prop.Name;
            // 处理可空类型（如double? → double，int? → int）
            Type underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            // ========== 1. 集合类型（List/IEnumerable） ==========
            if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
            {
                return GenerateCollectionValue(propType, underlyingType, messageAction, maxListCount);
            }

            // ========== 2. 自定义对象类型（非基础类型/非枚举） ==========
            if (!underlyingType.IsValueType && underlyingType != typeof(string))
            {
                return GenerateNestedObjectValue(underlyingType, messageAction, maxListCount);
            }

            return GenerateBasicTypeValue(propType, propName);
        }
        /// <summary>
        /// 生成基本类型值（字符串、枚举、数值等）
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        private static object? GenerateBasicTypeValue(Type type,string propName)
        {
            // 处理可空类型（如double? → double，int? → int）
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // ========== 3. 字符串类型（包括 string?） ==========
            if (underlyingType == typeof(string))
            {
                // "ListItem_" + Guid.NewGuid().ToString().Substring(0, 6)
                return GetStringValue(propName);
            }

            // ========== 4. 枚举类型（enum / enum?） ==========
            if (underlyingType.IsEnum)
            {
                return GetEnumValue(type, underlyingType);
            }

            // ========== 5. 布尔类型（bool / bool?） ==========
            if (underlyingType == typeof(bool))
            {
                bool boolValue = _random.Next(0, 2) == 1;
                return type == typeof(bool?) ? (bool?)boolValue : boolValue;
            }

            // ========== 6. 整数类型（int / int?） ==========
            if (underlyingType == typeof(int))
            {
                int intValue = _random.Next(0, 1000);
                return type == typeof(int?) ? (int?)intValue : intValue;
            }

            // ========== 7. 双精度浮点类型（double / double?） ==========
            if (underlyingType == typeof(double))
            {
                double doubleValue = Math.Round(_random.NextDouble() * 1000, 2);
                return type == typeof(double?) ? (double?)doubleValue : doubleValue;
            }

            // ========== 8. 浮点类型（float / float?）【新增：适配涂布设备】 ==========
            if (underlyingType == typeof(float))
            {
                float floatValue = (float)Math.Round(_random.NextDouble() * 100, 3); // 保留3位小数
                return type == typeof(float?) ? (float?)floatValue : floatValue;
            }

            // ========== 9. 短整型（short / short?）【新增】 ==========
            if (underlyingType == typeof(short))
            {
                // 生成 0~32767 范围内的随机short值（short最大值为32767）
                short shortValue = (short)_random.Next(0, short.MaxValue);
                // 区分可空/非可空类型返回
                return type == typeof(short?) ? (short?)shortValue : shortValue;
            }

            // ========== 10. 日期时间类型（DateTime / DateTime?）【新增】 ==========
            if (underlyingType == typeof(DateTime))
            {
                // 生成近30天内的随机时间（精确到秒），与原有字符串时间规则逻辑一致
                int daysAgo = _random.Next(0, 30);
                int hours = _random.Next(0, 24);
                int minutes = _random.Next(0, 60);
                int seconds = _random.Next(0, 60);

                DateTime dateTimeValue = DateTime.Now
                                                .AddDays(-daysAgo)
                                                .AddHours(hours)
                                                .AddMinutes(minutes)
                                                .AddSeconds(seconds);

                // 区分可空/非可空类型返回
                return type == typeof(DateTime?) ? (DateTime?)dateTimeValue : dateTimeValue;
            }

            // ========== 兜底：返回类型默认值 ==========
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        #region 细分类型生成逻辑
        /// <summary>
        /// 生成集合类型值（如List<TBMachineAxisDetailDto>）
        /// </summary>
        private static object? GenerateCollectionValue(Type propType, Type underlyingType, Action<string>? messageAction, int maxListCount)
        {
            // 获取集合的泛型参数（如List<T> → T）
            var genericArguments = underlyingType.GetGenericArguments();
            if (genericArguments.Length == 0) return Activator.CreateInstance(propType);

            Type itemType = genericArguments[0];
            // 生成1~maxListCount条集合元素
            int itemCount = _random.Next(1, maxListCount + 1);
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;

            // 为每个集合元素生成数据（支持嵌套对象）
            for (int i = 0; i < itemCount; i++)
            {
                if (itemType.IsValueType || itemType == typeof(string))
                {
                    //// 基础类型元素：直接生成值
                    //var propInfo = typeof(List<>).MakeGenericType(itemType).GetProperty("Item");
                    var value = GenerateBasicTypeValue(itemType, "ListItem_" + Guid.NewGuid().ToString().Substring(0, 6));
                    list.Add(value);
                }
                else
                {
                    // 自定义对象元素：递归生成对象数据
                    var itemInstance = Activator.CreateInstance(itemType);
                    GenerateObjectData(itemInstance!, messageAction, maxListCount);
                    list.Add(itemInstance);
                }
            }

            // 处理可空集合（如List<T>?）
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // 10%概率返回null
                bool returnNull = _random.Next(0, 10) == 0;
                if (returnNull) return null;
            }

            return list;
        }

        /// <summary>
        /// 生成嵌套自定义对象值（如TBMachineAxisDetailDto）
        /// </summary>
        private static object? GenerateNestedObjectValue(Type underlyingType, Action<string>? messageAction, int maxListCount)
        {
            // 10%概率返回null（适配可空对象）
            bool returnNull = _random.Next(0, 10) == 0;
            if (returnNull) return null;

            // 创建嵌套对象实例并递归赋值
            var nestedInstance = Activator.CreateInstance(underlyingType);
            GenerateObjectData(nestedInstance!, messageAction, maxListCount);
            return nestedInstance;
        }

        /// <summary>
        /// 字符串类型值的生成规则（兼容激光设备+通用规则）
        /// </summary>
        private static string GetStringValue(string propName)
        {
            #region 激光设备专属规则
            // 规则1：MES_UserRole → 从三个角色中随机选择一个
            if (propName.Equals("MES_UserRole", StringComparison.OrdinalIgnoreCase))
            {
                var roles = new List<string> { "管理员", "工程师", "操作员" };
                return roles[_random.Next(roles.Count)];
            }

            // 规则2：MES_AlarmMessage → 随机组合的制表符分隔报警信息
            if (propName.Equals("MES_AlarmMessage", StringComparison.OrdinalIgnoreCase))
            {
                // 预设报警信息库
                var alarmMessages = new List<string>
            {
                "功率异常", "冷水机温度过高", "扫码枪连接失败", "加工位偏移超标",
                "激光模块报警", "真空压力不足", "湿度超出阈值", "温度异常",
                "吸尘器故障", "标定阈值超限", "PSO分辨率异常", "Z轴加工位偏差",
                "物流状态异常", "三色灯报警", "冷水机流量不足", "激光频率异常",
                "脉宽参数错误", "加工速度超限", "绝缘线功率异常", "配方加载失败"
            };

                // 随机选择2-5条报警信息，用制表符分隔
                int count = _random.Next(2, 6);
                var randomAlarms = alarmMessages.OrderBy(x => _random.Next()).Take(count).ToList();
                return string.Join("\t", randomAlarms);
            }

            // 规则3：MES_PowerStatistics → 逗号分隔的4个随机瓦特值（保留5位小数）
            if (propName.Equals("MES_PowerStatistics", StringComparison.OrdinalIgnoreCase))
            {
                // 生成4个0-10范围内的随机瓦特值，保留5位小数
                var powerValues = new List<string>();
                for (int i = 0; i < 4; i++)
                {
                    double power = _random.NextDouble() * 100; // 0-100瓦特
                    powerValues.Add(power.ToString("0.00000")); // 格式化为5位小数
                }
                return string.Join(",", powerValues);
            }

            // 规则4：MES_ProcessResult → 85%概率OK，15%概率NG
            if (propName.Equals("MES_ProcessResult", StringComparison.OrdinalIgnoreCase))
            {
                // 生成0-99的随机数，0-84（85%）返回OK，85-99（15%）返回NG
                int randomNum = _random.Next(100);
                return randomNum < 85 ? "OK" : "NG";
            }
            #endregion

            #region 涂布设备专属规则
            // 涂布设备专属规则（保留原有逻辑）
            if (propName.Equals("AxisName", StringComparison.OrdinalIgnoreCase))
            {
                var roles = new List<string> { "龙门左轴", "龙门右轴", "摸头左轴", "摸头右轴", "柱塞泵", "清洗", "pin顶升", "辊轮", "前传输", "中传输1", "中传输2", "后传输" };
                return roles[_random.Next(roles.Count)];
            }
            #endregion
            // 含Time的字符串 → 近30天随机时间
            if (propName.Contains("Time", StringComparison.OrdinalIgnoreCase))
            {
                int daysAgo = _random.Next(0, 30);
                var randomTime = DateTime.Now.AddDays(-daysAgo)
                                            .AddHours(_random.Next(0, 24))
                                            .AddMinutes(_random.Next(0, 60))
                                            .AddSeconds(_random.Next(0, 60));
                return randomTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // 通用字符串规则：1-10位随机字符（数字+字母）
            var sb = new StringBuilder();
            int length = _random.Next(1, 11);
            for (int i = 0; i < length; i++)
            {
                int charCode = _random.Next(3) switch
                {
                    0 => _random.Next(48, 58),    // 数字
                    1 => _random.Next(65, 91),    // 大写字母
                    _ => _random.Next(97, 123)    // 小写字母
                };
                sb.Append((char)charCode);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 生成枚举值（支持可空枚举）
        /// </summary>
        private static object? GetEnumValue(Type propType, Type underlyingEnumType)
        {
            Array enumValues = Enum.GetValues(underlyingEnumType);
            if (enumValues.Length == 0)
            {
                return propType.IsValueType ? Activator.CreateInstance(propType) : null;
            }

            object randomEnumValue = enumValues.GetValue(_random.Next(0, enumValues.Length))!;

            // 可空枚举：10%概率返回null
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                bool returnNull = _random.Next(0, 10) == 0;
                if (returnNull) return null;
            }

            return randomEnumValue;
        }
        #endregion
    }
}
