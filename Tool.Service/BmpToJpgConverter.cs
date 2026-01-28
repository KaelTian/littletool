using System.Drawing;
using System.Drawing.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Tool.Service
{
    public static class BmpToJpgConverter
    {
        /// <summary>
        /// 单个BMP文件转换为JPG
        /// </summary>
        /// <param name="sourceBmpPath">源BMP文件路径</param>
        /// <param name="targetJpgPath">目标JPG文件路径</param>
        /// <param name="quality">图片质量(1-100)</param>
        /// <returns>转换结果信息</returns>
        public static ConversionResult ConvertSingleFile(string sourceBmpPath, string targetJpgPath, long quality = 85L)
        {
            try
            {
                // 检查源文件是否存在
                if (!File.Exists(sourceBmpPath))
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = $"源文件不存在: {sourceBmpPath}",
                        OriginalSize = 0,
                        NewSize = 0
                    };
                }

                // 检查文件扩展名
                if (!Path.GetExtension(sourceBmpPath).Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    return new ConversionResult
                    {
                        Success = false,
                        Message = "源文件不是BMP格式",
                        OriginalSize = 0,
                        NewSize = 0
                    };
                }

                // 确保目标目录存在
                var targetDirectory = Path.GetDirectoryName(targetJpgPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory!);
                }

                // 获取原始文件大小
                var originalFileInfo = new FileInfo(sourceBmpPath);
                long originalSize = originalFileInfo.Length;
#pragma warning disable CA1416
                // 执行转换
                using (var bmp = new Bitmap(sourceBmpPath))
                {
                    // 设置JPG编码参数
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                    // 获取JPG编码器
                    var jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                    // 保存为JPG
                    bmp.Save(targetJpgPath, jpgEncoder!, encoderParameters);
                }

                // 获取新文件大小
                var newFileInfo = new FileInfo(targetJpgPath);
                long newSize = newFileInfo.Length;

                return new ConversionResult
                {
                    Success = true,
                    Message = $"转换成功: {Path.GetFileName(sourceBmpPath)}",
                    OriginalSize = originalSize,
                    NewSize = newSize,
                    CompressionRatio = (double)newSize / originalSize
                };
            }
            catch (Exception ex)
            {
                return new ConversionResult
                {
                    Success = false,
                    Message = $"转换失败: {ex.Message}",
                    OriginalSize = 0,
                    NewSize = 0
                };
            }
        }
        /// <summary>
        /// 批量转换目录中的所有BMP文件为JPG
        /// </summary>
        /// <param name="sourceDirectory">源目录</param>
        /// <param name="targetDirectory">目标目录</param>
        /// <param name="quality">图片质量(1-100)</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        /// <returns>批量转换结果</returns>
        public static BatchConversionResult ConvertDirectory(string sourceDirectory, string targetDirectory,
            long quality = 85L, bool overwrite = false)
        {
            var result = new BatchConversionResult();

            try
            {
                // 检查源目录是否存在
                if (!Directory.Exists(sourceDirectory))
                {
                    result.Success = false;
                    result.Message = $"源目录不存在: {sourceDirectory}";
                    return result;
                }

                // 创建目标目录
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // 获取所有BMP文件
                var bmpFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                                       .Where(file => Path.GetExtension(file).Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                                       .ToArray();

                if (bmpFiles.Length == 0)
                {
                    result.Message = "在源目录中未找到BMP文件";
                    return result;
                }

                result.TotalFiles = bmpFiles.Length;

                foreach (var bmpFile in bmpFiles)
                {
                    // 构建完整的目标文件路径
                    var jpgFilePath = GenerateTargetJpgPath(bmpFile, sourceDirectory, targetDirectory);

                    // 如果文件已存在且不覆盖，则跳过
                    if (File.Exists(jpgFilePath) && !overwrite)
                    {
                        result.SkippedFiles++;
                        continue;
                    }

                    // 执行单个文件转换
                    var singleResult = ConvertSingleFile(bmpFile, jpgFilePath, quality);

                    if (singleResult.Success)
                    {
                        result.SuccessfulConversions++;
                        result.IndividualResults.Add(singleResult);

                        // 累加文件大小
                        result.TotalOriginalSize += singleResult.OriginalSize;
                        result.TotalNewSize += singleResult.NewSize;
                    }
                    else
                    {
                        result.FailedConversions++;
                        result.IndividualResults.Add(singleResult);
                    }
                }

                result.Success = true;
                result.Message = $"批量转换完成。成功: {result.SuccessfulConversions}, 失败: {result.FailedConversions}, 跳过: {result.SkippedFiles}";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"批量转换失败: {ex.Message}";
                return result;
            }
        }
        /// <summary>
        /// 获取相对路径
        /// </summary>
        private static string GetRelativePath(string fullPath, string basePath)
        {
            // 使用Path类的相对路径方法
            return Path.GetRelativePath(basePath, fullPath);
        }
        /// <summary>
        /// 获取图片编码器
        /// </summary>
        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }
        /// <summary>
        /// 生成目标JPG文件路径
        /// </summary>
        /// <param name="sourceBmpPath">源端BMP文件全路径</param>
        /// <param name="sourceDirectory">源端BMP文件夹</param>
        /// <param name="targetDirectory">目的端JPG文件夹</param>
        /// <returns></returns>
        public static string GenerateTargetJpgPath(string sourceBmpPath, string sourceDirectory, string targetDirectory)
        {
            // 获取相对于源目录的相对路径
            var relativePath = GetRelativePath(sourceBmpPath, sourceDirectory);

            // 将相对路径的文件扩展名改为.jpg
            var jpgRelativePath = Path.ChangeExtension(relativePath, ".jpg");

            // 构建完整的目标文件路径
            var jpgFilePath = Path.Combine(targetDirectory, jpgRelativePath);

            return jpgFilePath;
        }
    }

}
