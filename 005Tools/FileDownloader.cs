namespace _005Tools
{
    internal static class FileDownloader
    {

        // 推荐：创建静态的HttpClient实例（避免频繁创建释放连接）
        private static readonly HttpClient _httpClient = new HttpClient
        {
            // 设置超时时间（根据文件大小调整，示例为30秒）
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// 从指定URL下载文件并保存到本地
        /// </summary>
        /// <param name="fileUrl">文件的URL（IIS发布的文件地址）</param>
        /// <param name="savePath">本地保存路径（包含文件名）</param>
        /// <returns></returns>
        public static async Task DownloadFileAsync(string fileUrl, string savePath)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentNullException(nameof(fileUrl), "文件URL不能为空");
            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentNullException(nameof(savePath), "保存路径不能为空");
            try
            {
                // 1. 发送GET请求获取文件流（使用HttpCompletionOption.ResponseHeadersRead优化大文件下载）
                using (var response = await _httpClient.GetAsync(
                    fileUrl,
                    HttpCompletionOption.ResponseHeadersRead)) {
                    // 确保请求成功
                    response.EnsureSuccessStatusCode();

                    // 2. 创建本地文件流 (FileMode.Create会覆盖已存在的文件)
                    using (var fileStream=new FileStream(
                        savePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        // 缓冲区大小 (可根据需求调整，默认4096)
                        bufferSize: 81920,
                        // 异步写入（提升性能）
                        useAsync: true))
                    {
                        // 3. 将响应流复制到本地文件流
                        await response.Content.CopyToAsync(fileStream);

                        // 确保所有数据写入磁盘
                        await fileStream.FlushAsync();
                    }

                    Console.WriteLine($"文件下载完成！保存路径：{savePath}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP请求异常：{ex.Message}");
                // 可根据需要抛出异常或处理
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"文件操作异常：{ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载文件时发生未知异常：{ex.Message}");
                throw;
            }
        }
    }
}
