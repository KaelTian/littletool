namespace Tool.Service
{
    /// <summary>
    /// 单个文件转换结果
    /// </summary>
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long OriginalSize { get; set; }
        public long NewSize { get; set; }
        public double CompressionRatio { get; set; }

        /// <summary>
        /// 获取智能格式化的文件大小信息
        /// </summary>
        public string GetSizeInfo(bool showCompressionRatio = true)
        {
            if (OriginalSize == 0) return "N/A";

            var originalFormatted = FormatFileSize(OriginalSize);
            var newFormatted = FormatFileSize(NewSize);

            if (showCompressionRatio)
            {
                var ratio = CompressionRatio * 100;
                return $"{originalFormatted} → {newFormatted} (压缩至{ratio:F1}%)";
            }
            else
            {
                return $"{originalFormatted} → {newFormatted}";
            }
        }

        /// <summary>
        /// 根据文件大小自动选择合适的单位进行格式化
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            // 根据单位选择合适的小数位数
            string formatString = order == 0 ? "F0" : // B - 无小数
                                 order == 1 ? "F1" : // KB - 1位小数
                                 order == 2 ? "F2" : // MB - 2位小数
                                 "F3"; // GB/TB - 3位小数

            return $"{size.ToString(formatString)}{sizes[order]}";
        }

        /// <summary>
        /// 获取详细的文件大小信息（包含原始字节数）
        /// </summary>
        public string GetDetailedSizeInfo()
        {
            if (OriginalSize == 0) return "N/A";

            var originalFormatted = FormatFileSize(OriginalSize);
            var newFormatted = FormatFileSize(NewSize);
            var ratio = CompressionRatio * 100;
            var spaceSaved = OriginalSize - NewSize;
            var spaceSavedFormatted = FormatFileSize(spaceSaved);

            return $"{originalFormatted} → {newFormatted} (压缩至{ratio:F1}%, 节省{spaceSavedFormatted})";
        }

        /// <summary>
        /// 获取简化的大小信息（不显示压缩比例）
        /// </summary>
        public string GetSimpleSizeInfo()
        {
            return GetSizeInfo(false);
        }
    }

    /// <summary>
    /// 批量转换结果 - 添加智能格式化的方法
    /// </summary>
    public class BatchConversionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int TotalFiles { get; set; }
        public int SuccessfulConversions { get; set; }
        public int FailedConversions { get; set; }
        public int SkippedFiles { get; set; }
        public long TotalOriginalSize { get; set; }
        public long TotalNewSize { get; set; }
        public List<ConversionResult> IndividualResults { get; set; } = new List<ConversionResult>();

        public double TotalCompressionRatio => TotalOriginalSize > 0 ? (double)TotalNewSize / TotalOriginalSize : 0;

        // 定义输出事件
        public event Action<string>? OnOutput;

        /// <summary>
        /// 触发输出事件
        /// </summary>
        private void Output(string message)
        {
            OnOutput?.Invoke(message);
        }

        /// <summary>
        /// 添加控制台输出处理器
        /// </summary>
        public void AddConsoleOutput()
        {
            OnOutput += message => Console.WriteLine(message);
        }

        ///// <summary>
        ///// 添加日志输出处理器
        ///// </summary>
        //public void AddLogOutput(ILogger logger)
        //{
        //    OnOutput += message => logger.Info(message);
        //}

        /// <summary>
        /// 添加自定义输出处理器
        /// </summary>
        public void AddCustomOutput(Action<string> outputAction)
        {
            OnOutput += outputAction;
        }

        /// <summary>
        /// 移除所有输出处理器
        /// </summary>
        public void ClearOutputHandlers()
        {
            OnOutput = null;
        }


        /// <summary>
        /// 格式化文件大小（静态方法，可在其他地方使用）
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            string formatString = order == 0 ? "F0" :
                                 order == 1 ? "F1" :
                                 order == 2 ? "F2" :
                                 "F3";

            return $"{size.ToString(formatString)}{sizes[order]}";
        }

        /// <summary>
        /// 获取总大小信息
        /// </summary>
        public string GetTotalSizeInfo()
        {
            if (TotalOriginalSize == 0) return "N/A";

            var originalFormatted = FormatFileSize(TotalOriginalSize);
            var newFormatted = FormatFileSize(TotalNewSize);
            var totalRatio = TotalCompressionRatio * 100;
            var totalSpaceSaved = TotalOriginalSize - TotalNewSize;
            var spaceSavedFormatted = FormatFileSize(totalSpaceSaved);

            return $"{originalFormatted} → {newFormatted} (压缩至{totalRatio:F1}%, 总共节省{spaceSavedFormatted})";
        }

        public void PrintSummary()
        {
            Output("=== 批量转换结果 ===");
            Output($"总文件数: {TotalFiles}");
            Output($"成功转换: {SuccessfulConversions}");
            Output($"转换失败: {FailedConversions}");
            Output($"跳过文件: {SkippedFiles}");

            if (TotalOriginalSize > 0)
            {
                Output($"总大小: {GetTotalSizeInfo()}");
            }

            Output($"详细信息: {Message}");
        }

        /// <summary>
        /// 打印详细转换结果
        /// </summary>
        public void PrintDetailedResults()
        {
            Output("\n=== 详细转换结果 ===");
            foreach (var result in IndividualResults)
            {
                var status = result.Success ? "成功" : "失败";
                var sizeInfo = result.Success ? result.GetDetailedSizeInfo() : "转换失败";
                Output($"{status} {result.Message} {sizeInfo}");
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public void PrintStatistics()
        {
            Output("\n=== 转换统计 ===");
            Output($"总处理文件: {TotalFiles}");
            Output($"成功: {SuccessfulConversions} ({((double)SuccessfulConversions / TotalFiles) * 100:F1}%)");
            Output($"失败: {FailedConversions} ({((double)FailedConversions / TotalFiles) * 100:F1}%)");
            Output($"跳过: {SkippedFiles} ({((double)SkippedFiles / TotalFiles) * 100:F1}%)");

            if (TotalOriginalSize > 0)
            {
                var spaceSaved = TotalOriginalSize - TotalNewSize;
                var spaceSavedFormatted = FormatFileSize(spaceSaved);
                Output($"空间节省: {spaceSavedFormatted}");
                Output($"平均压缩率: {TotalCompressionRatio * 100:F1}%");
            }
        }
    }
}
