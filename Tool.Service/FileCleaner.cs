namespace Tool.Service
{
    public class FileCleaner
    {
        /// <summary>
        /// 目标文件夹
        /// </summary>
        private string? targetFolder;
        /// <summary>
        /// 过期天数(默认30天)
        /// </summary>
        private double expireDays = 30;
        /// <summary>
        /// 是否是最后访问时间
        /// </summary>
        private bool useLastAccessTime = false;
        /// <summary>
        /// 是否删除空文件夹
        /// </summary>
        private bool deleteEmptyFolders = true;

        public FileCleaner(string? targetFolder, double expireDays = 30, bool useLastAccessTime = false, bool deleteEmptyFolders = true)
        {
            this.targetFolder = targetFolder;
            this.expireDays = expireDays;
            this.useLastAccessTime = useLastAccessTime;
            this.deleteEmptyFolders = deleteEmptyFolders;
        }

        public void StartCleaning()
        {
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
            {
                Console.WriteLine("目标文件夹无效或不存在，清理操作终止。");
                return;
            }
            DateTime expireTime = DateTime.Now.AddDays(-expireDays);
            Console.WriteLine($"开始清理文件夹：{targetFolder}");
            Console.WriteLine($"过期时间点：{expireTime:yyyy-MM-dd HH:mm:ss}（根据{GetTimeType()}判断）");
            CleanFolder(targetFolder, expireTime);
            Console.WriteLine("文件夹清理操作完成。");
        }

        /// <summary>
        /// 递归清理文件夹（先删文件，再删子文件夹，最后处理当前文件夹）
        /// </summary>
        /// <param name="folderPath">当前文件夹路径</param>
        /// <param name="expireTime">过期时间点</param>
        private void CleanFolder(string folderPath, DateTime expireTime)
        {
            try
            {
                // 1. 先处理当前文件夹下的所有文件
                var files = Directory.GetFiles(folderPath);
                foreach (var file in files)
                {
                    try
                    {
                        // 获取文件信息
                        var fileInfo = new FileInfo(file);
                        // 判断是否过期（根据配置选择访问时间或修改时间）
                        DateTime compareTime = useLastAccessTime ? fileInfo.LastAccessTime : fileInfo.LastWriteTime;

                        if (compareTime < expireTime)
                        {
                            // 处理只读文件（移除只读属性）
                            if (fileInfo.IsReadOnly)
                            {
                                fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                            }

                            // 删除文件
                            fileInfo.Delete();
                            Console.WriteLine($"已删除文件：{file}");
                        }
                        else
                        {
                            Console.WriteLine($"文件未过期：{file}（最后{GetTimeType()}：{compareTime:yyyy-MM-dd HH:mm:ss}）");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除文件失败：{file}，原因：{ex.Message}");
                    }
                }

                // 2. 递归处理所有子文件夹
                var subFolders = Directory.GetDirectories(folderPath);
                foreach (var subFolder in subFolders)
                {
                    CleanFolder(subFolder, expireTime); // 递归清理子文件夹
                }

                // 3. 处理当前文件夹（满足以下条件则删除）
                // - 配置允许删除空文件夹 OR 文件夹本身过期
                var folderInfo = new DirectoryInfo(folderPath);
                DateTime folderCompareTime = useLastAccessTime ? folderInfo.LastAccessTime : folderInfo.LastWriteTime;
                bool isFolderExpired = folderCompareTime < expireTime;
                bool isFolderEmpty = Directory.GetFileSystemEntries(folderPath).Length == 0;

                if ((deleteEmptyFolders && isFolderEmpty) || isFolderExpired)
                {
                    try
                    {
                        // 删除文件夹（必须确保文件夹为空，递归处理后已满足）
                        folderInfo.Delete();
                        Console.WriteLine($"已删除文件夹：{folderPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除文件夹失败：{folderPath}，原因：{ex.Message}");
                    }
                }
                else
                {
                    if (!isFolderEmpty)
                        Console.WriteLine($"文件夹非空，不删除：{folderPath}");
                    else
                        Console.WriteLine($"文件夹未过期，不删除：{folderPath}（最后{GetTimeType()}：{folderCompareTime:yyyy-MM-dd HH:mm:ss}）");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"访问文件夹失败：{folderPath}，原因：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前使用的时间类型描述（用于日志）
        /// </summary>
        private string GetTimeType()
        {
            return useLastAccessTime ? "访问时间" : "修改时间";
        }

    }
}
