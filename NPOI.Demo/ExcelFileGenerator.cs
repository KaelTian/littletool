using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPOI.Demo
{
    public class ExcelFileGenerator
    {
        /// <summary>
        /// 导出并保存到指定路径
        /// </summary>
        public static void SaveToFile(Dictionary<string, object> content, string filePath, string[]? columns = null, bool showRowIndex = false)
        {
            byte[] bytes = Export.ToExcel(content, columns, showRowIndex: showRowIndex);

            // 确保目录存在
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 写入文件
            System.IO.File.WriteAllBytes(filePath, bytes);
        }

        /// <summary>
        /// 导出并保存到指定路径
        /// </summary>
        public static void ExportMultipleSheets(Dictionary<string, object> content, string filePath, string[]? columns = null, bool showRowIndex = false)
        {
            byte[] bytes = Export.ExportMultipleSheets(content, columns, showRowIndex: showRowIndex);

            // 确保目录存在
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 写入文件
            System.IO.File.WriteAllBytes(filePath, bytes);
        }
    }
}
