using Microsoft.Extensions.Configuration;

namespace SyncAccessDB
{
    public class SyncStateService
    {
        private readonly string _stateFile;
        private long _lastId;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public SyncStateService(IConfiguration config)
        {
            // 状态文件路径，可配置
            _stateFile = config.GetValue("StateFilePath", "sync_state.json");
            LoadState();
        }

        public long LastSyncedId => _lastId;

        private void LoadState()
        {
            if (File.Exists(_stateFile))
            {
                var json = File.ReadAllText(_stateFile);
                var state = System.Text.Json.JsonSerializer.Deserialize<SyncState>(json);
                _lastId = state?.LastSyncedId ?? 0;
            }
            else
            {
                _lastId = 0;
            }
        }

        public async Task UpdateLastSyncedIdAsync(long id)
        {
            await _lock.WaitAsync();
            try
            {
                if (id > _lastId)  // 只更新更大的
                {
                    _lastId = id;
                    var state = new SyncState { LastSyncedId = id, UpdateTime = DateTime.Now };
                    var json = System.Text.Json.JsonSerializer.Serialize(state);
                    await File.WriteAllTextAsync(_stateFile, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private class SyncState
        {
            public long LastSyncedId { get; set; }
            public DateTime UpdateTime { get; set; }
        }
    }
}
