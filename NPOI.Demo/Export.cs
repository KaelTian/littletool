using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace NPOI.Demo
{
    public static class Export
    {
        /// <summary>
        /// 导出数据到Excel文件
        /// </summary>
        /// <param name="content"><工作表, 数据内容></param>
        /// <param name="columns">显示项目</param>
        /// <param name="ignore">忽略项目</param>
        /// <param name="showHeader">是否显示标题行</param>
        /// <param name="showRowIndex">是否显示行号</param>
        /// <returns>数据流</returns>
        public static byte[] ToExcel(Dictionary<string, object> content, string[]? columns = null, string[]? ignore = null, bool showHeader = true, bool showRowIndex = false)
        {
            // 返回内容
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // 创建工作簿（非线性安全）
                using (IWorkbook workbook = new XSSFWorkbook())
                {
                    // 添加各工作表
                    foreach (KeyValuePair<string, object> item in content)
                    {
                        ISheet sheet = workbook.CreateSheet(item.Key);

                        if (item.Value is List<object> list)
                        {
                            int rowIndex = 0;
                            int colIndex = 0;

                            foreach (IDictionary<string, object> property in list)
                            {
                                IRow row;

                                // 标题行
                                if (showHeader && rowIndex < 1)
                                {
                                    // 添加行
                                    row = sheet.CreateRow(rowIndex++);

                                    // fix bug: showRowIndex 为 true 时，标题行的行号列没有被创建
                                    if (showRowIndex)
                                    {
                                        row.CreateCell(colIndex++).SetCellValue("行号");
                                    }

                                    foreach (string column in property.Keys)
                                    {
                                        // 显示项目
                                        if (columns != null && column.Any() && !columns.Contains(column))
                                        {
                                            continue;
                                        }
                                        // 忽略项目
                                        if (ignore != null && ignore.Contains(column))
                                        {
                                            continue;
                                        }

                                        row.CreateCell(colIndex++).SetCellValue(column);
                                    }
                                }

                                // 添加行
                                row = sheet.CreateRow(rowIndex++);
                                // 列号
                                colIndex = 0;

                                if (showRowIndex)
                                {
                                    // fix bug: showRowIndex 为 true 时，行号列的数字不正确（多计算了一行）
                                    int rowNumber = rowIndex - (showHeader ? 1 : 0);  // 修正计算逻辑
                                    var cell = row.CreateCell(colIndex++);
                                    cell.SetCellValue(rowNumber);      // 写数字，不是字符串
                                }

                                foreach (string column in property.Keys)
                                {
                                    // 显示项目
                                    if (columns != null && column.Any() && !columns.Contains(column))
                                    {
                                        continue;
                                    }
                                    // 忽略项目
                                    if (ignore != null && ignore.Contains(column))
                                    {
                                        continue;
                                    }

                                    row.CreateCell(colIndex++).SetCellValue($"{property[column]}");
                                }
                            }
                        }
                    }

                    // 将工作簿写入内存流
                    workbook.Write(memoryStream);
                }

                return memoryStream.ToArray();
            }
        }

        public static byte[] ExportMultipleSheets(Dictionary<string, object> content, string[]? columns = null, string[]? ignore = null, bool showHeader = true, bool showRowIndex = false)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (IWorkbook workbook = new XSSFWorkbook())
                {
                    // 在 workbook 创建后，提前定义好样式（不要在循环里创建！）
                    ICellStyle integerStyle = workbook.CreateCellStyle();
                    integerStyle.DataFormat = workbook.CreateDataFormat().GetFormat("0");  // 196 显示为 196

                    ICellStyle decimalStyle = workbook.CreateCellStyle();
                    decimalStyle.DataFormat = workbook.CreateDataFormat().GetFormat("0.####################");  // 196.50 显示为 196.5

                    foreach (KeyValuePair<string, object> item in content)
                    {
                        ISheet sheet = workbook.CreateSheet(item.Key);

                        if (item.Value is List<object> list && list.Count > 0)
                        {
                            // 先拿到第一条数据，计算总列数
                            var firstRow = list[0] as IDictionary<string, object>;
                            if (firstRow == null) continue;

                            int columnCount = firstRow.Keys.Count(k => IsColumnVisible(k, columns, ignore));
                            if (showRowIndex) columnCount++;

                            int rowIndex = 0;

                            foreach (IDictionary<string, object> property in list)
                            {
                                IRow row;
                                int colIndex = 0;

                                // 标题行
                                if (showHeader && rowIndex == 0)
                                {
                                    row = sheet.CreateRow(rowIndex++);

                                    // fix bug: showRowIndex 为 true 时，标题行的行号列没有被创建
                                    if (showRowIndex)
                                    {
                                        row.CreateCell(colIndex++).SetCellValue("行号");
                                    }

                                    foreach (string column in property.Keys)
                                    {
                                        if (!IsColumnVisible(column, columns, ignore)) continue;
                                        row.CreateCell(colIndex++).SetCellValue(column);
                                    }
                                }

                                // 数据行
                                row = sheet.CreateRow(rowIndex++);

                                colIndex = 0;

                                if (showRowIndex)
                                {
                                    int rowNumber = rowIndex - (showHeader ? 1 : 0);  // 修正计算逻辑
                                    var cell = row.CreateCell(colIndex++);
                                    cell.SetCellValue(rowNumber);      // 写数字，不是字符串
                                    cell.CellStyle = integerStyle;     // 行号也用整数样式
                                }

                                foreach (string column in property.Keys)
                                {
                                    if (!IsColumnVisible(column, columns, ignore)) continue;

                                    var value = property[column];
                                    var cell = row.CreateCell(colIndex++);

                                    if (IsNumeric(value))
                                    {
                                        // 大整数保护（可选）
                                        if (value is long l && Math.Abs(l) > 9007199254740992L)
                                        {
                                            cell.SetCellValue(value.ToString());  // 超长 ID 转文本
                                            continue;
                                        }

                                        double numVal = Convert.ToDouble(value);
                                        cell.SetCellValue(numVal);
                                        cell.CellStyle = (numVal == Math.Truncate(numVal)) ? integerStyle : decimalStyle;
                                    }
                                    else
                                    {
                                        cell.SetCellValue(value?.ToString() ?? "");
                                    }
                                }
                            }

                            // ✅ 修正：循环外部，根据计算的 columnCount 调整列宽
                            for (int i = 0; i < columnCount; i++)
                            {
                                sheet.AutoSizeColumn(i);
                            }
                        }
                    }

                    workbook.Write(memoryStream);
                }
                return memoryStream.ToArray();
            }
        }

        // 辅助方法：判断是否为数字类型
        private static bool IsNumeric(object? value)
        {
            if (value == null) return false;

            return value is sbyte or byte or short or ushort or int or uint
                or long or ulong or float or double or decimal;
        }

        // 辅助方法：判断列是否可见
        private static bool IsColumnVisible(string column, string[]? columns, string[]? ignore)
        {
            if (columns != null && columns.Length > 0 && !columns.Contains(column))
                return false;
            if (ignore != null && ignore.Contains(column))
                return false;
            return true;
        }
    }
}
