using System.Collections.Concurrent;
using Timer = System.Timers.Timer;

namespace Tool.Service
{
    public class RandomFileSynchronizer
    {
        private readonly string _sourceRootPath;
        private readonly string _targetRootPath;
        private readonly Timer _timer;
        private readonly Random _random = new Random();
        private readonly ConcurrentDictionary<string, DateTime> _lastSyncTimes = new ConcurrentDictionary<string, DateTime>();

        // 配置参数
        public int SyncIntervalSeconds { get; set; } = 30; // 同步间隔（秒）
        public int MinFilesPerCycle { get; set; } = 1;     // 每周期最少同步文件数
        public int MaxFilesPerCycle { get; set; } = 5;     // 每周期最多同步文件数
        public bool CreateDirectories { get; set; } = true; // 是否同步目录结构
        public bool OverwriteExisting { get; set; } = false; // 是否覆盖已存在文件
        public string FilePattern { get; set; } = "*.bmp"; // 文件匹配模式

        public RandomFileSynchronizer(string sourceRootPath, string targetRootPath)
        {
            if (!Directory.Exists(sourceRootPath))
                throw new DirectoryNotFoundException($"源目录不存在: {sourceRootPath}");

            _sourceRootPath = sourceRootPath;
            _targetRootPath = targetRootPath;

            // 确保目标目录存在
            Directory.CreateDirectory(targetRootPath);

            // 初始化定时器
            _timer = new Timer(SyncIntervalSeconds * 1000);
            _timer.Elapsed += (sender, e) => SyncCycle();
        }

        /// <summary>
        /// 开始同步任务
        /// </summary>
        public void Start()
        {
            Console.WriteLine($"[{DateTime.Now}] 开始随机文件同步任务");
            Console.WriteLine($"源目录: {_sourceRootPath}");
            Console.WriteLine($"目标目录: {_targetRootPath}");
            Console.WriteLine($"同步间隔: {SyncIntervalSeconds}秒");
            Console.WriteLine($"每周期文件数: {MinFilesPerCycle}-{MaxFilesPerCycle}");

            _timer.Start();

            // 立即执行一次初始同步
            Task.Run(() => InitialSync());
        }

        /// <summary>
        /// 停止同步任务
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
            Console.WriteLine($"[{DateTime.Now}] 停止文件同步任务");
        }

        /// <summary>
        /// 初始同步（可选）
        /// </summary>
        private void InitialSync()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] 执行初始同步...");
                PerformSync(initialSync: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] 初始同步失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步周期
        /// </summary>
        private void SyncCycle()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] 开始新的同步周期");
                PerformSync();
                Console.WriteLine($"[{DateTime.Now}] 同步周期完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] 同步周期失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行同步操作
        /// </summary>
        private void PerformSync(bool initialSync = false)
        {
            // 获取源目录所有bmp文件
            var allSourceFiles = GetAllSourceFiles();

            if (allSourceFiles.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] 源目录中没有找到BMP文件");
                return;
            }

            // 随机选择要同步的文件数量
            int filesToSyncCount = initialSync
                ? Math.Min(10, allSourceFiles.Count) // 初始同步最多10个文件
                : _random.Next(MinFilesPerCycle, Math.Min(MaxFilesPerCycle, allSourceFiles.Count) + 1);

            // 随机选择文件（加权随机：优先选择长时间未同步的文件）
            var filesToSync = SelectFilesRandomly(allSourceFiles, filesToSyncCount);

            // 创建目标目录（如果需要）
            if (CreateDirectories)
            {
                CreateTargetDirectories(filesToSync);
            }

            // 同步文件
            int syncedCount = SyncFiles(filesToSync);

            // 更新同步时间记录
            UpdateSyncTimes(filesToSync.Take(syncedCount));

            Console.WriteLine($"[{DateTime.Now}] 本周期同步了 {syncedCount}/{filesToSyncCount} 个文件");
        }

        /// <summary>
        /// 获取所有源文件
        /// </summary>
        private List<FileInfo> GetAllSourceFiles()
        {
            var files = new List<FileInfo>();

            try
            {
                // 递归搜索所有子目录中的bmp文件
                var allDirectories = Directory.GetDirectories(_sourceRootPath, "*", SearchOption.AllDirectories)
                    .Append(_sourceRootPath);

                foreach (var directory in allDirectories)
                {
                    var bmpFiles = Directory.GetFiles(directory, FilePattern);
                    files.AddRange(bmpFiles.Select(f => new FileInfo(f)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] 扫描源文件失败: {ex.Message}");
            }

            return files;
        }

        /// <summary>
        /// 随机选择文件（加权：长时间未同步的文件有更高优先级）
        /// </summary>
        private List<FileInfo> SelectFilesRandomly(List<FileInfo> allFiles, int count)
        {
            if (count >= allFiles.Count)
                return allFiles.ToList();

            var selected = new HashSet<FileInfo>();

            // 计算权重：最近同步时间越早，权重越高
            var weightedFiles = allFiles.Select(file =>
            {
                double weight = 1.0;
                if (_lastSyncTimes.TryGetValue(file.FullName, out var lastSync))
                {
                    var hoursSinceSync = (DateTime.Now - lastSync).TotalHours;
                    weight = Math.Max(1.0, hoursSinceSync); // 每1小时增加1点权重
                }
                return new { File = file, Weight = weight };
            }).ToList();

            while (selected.Count < count && weightedFiles.Count > 0)
            {
                // 计算总权重
                double totalWeight = weightedFiles.Sum(f => f.Weight);

                // 随机选择
                double randomValue = _random.NextDouble() * totalWeight;
                double cumulative = 0;

                foreach (var item in weightedFiles)
                {
                    cumulative += item.Weight;
                    if (randomValue <= cumulative)
                    {
                        selected.Add(item.File);
                        weightedFiles.Remove(item);
                        break;
                    }
                }
            }

            return selected.ToList();
        }

        /// <summary>
        /// 创建目标目录结构
        /// </summary>
        private void CreateTargetDirectories(List<FileInfo> filesToSync)
        {
            var directories = new HashSet<string>();

            foreach (var file in filesToSync)
            {
                // 获取相对于源根目录的相对路径
                var relativePath = Path.GetRelativePath(_sourceRootPath, file.DirectoryName!);
                var targetDirectory = Path.Combine(_targetRootPath, relativePath);

                directories.Add(targetDirectory);
            }

            foreach (var dir in directories)
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        Console.WriteLine($"[{DateTime.Now}] 创建目录: {dir}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] 创建目录失败 {dir}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 同步文件（同步版本）
        /// </summary>
        private int SyncFiles(List<FileInfo> filesToSync)
        {
            int syncedCount = 0;

            foreach (var sourceFile in filesToSync)
            {
                // 计算目标文件路径
                var relativePath = Path.GetRelativePath(_sourceRootPath, sourceFile.FullName);
                var targetFile = Path.Combine(_targetRootPath, relativePath);

                // 检查目标文件是否已存在
                if (File.Exists(targetFile) && !OverwriteExisting)
                {
                    Console.WriteLine($"[{DateTime.Now}] 跳过已存在文件: {relativePath}");
                    continue;
                }

                try
                {
                    File.Copy(sourceFile.FullName, targetFile, OverwriteExisting);
                    syncedCount++;
                    Console.WriteLine($"[{DateTime.Now}] 同步文件: {relativePath} ({FormatFileSize(sourceFile.Length)})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] 同步文件失败 {relativePath}: {ex.Message}");
                }
            }

            return syncedCount;
        }

        /// <summary>
        /// 更新同步时间记录
        /// </summary>
        private void UpdateSyncTimes(IEnumerable<FileInfo> syncedFiles)
        {
            var now = DateTime.Now;
            foreach (var file in syncedFiles)
            {
                _lastSyncTimes[file.FullName] = now;
            }
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 获取同步统计信息
        /// </summary>
        public void PrintStatistics()
        {
            var allSourceFiles = GetAllSourceFiles();
            Console.WriteLine("=== 同步统计 ===");
            Console.WriteLine($"源目录文件总数: {allSourceFiles.Count}");
            Console.WriteLine($"已记录同步时间的文件数: {_lastSyncTimes.Count}");

            if (_lastSyncTimes.Count > 0)
            {
                Console.WriteLine($"最近同步周期: {_lastSyncTimes.Values.Max():yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                Console.WriteLine($"最近同步周期: 无记录");
            }
        }
    }
}