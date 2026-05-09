using SqlSugar;

namespace SyncAccessDB
{
    // Access层实体（保持原样）
    [SugarTable("Record1")]  // 假设Access表名
    public class AccessRecord
    {
        [SugarColumn(ColumnName = "序号", IsPrimaryKey = true, IsIdentity = true)]
        public int 序号 { get; set; }

        [SugarColumn(ColumnName = "日期")]
        public string? 日期 { get; set; }

        [SugarColumn(ColumnName = "时间")]
        public string? 时间 { get; set; }

        [SugarColumn(ColumnName = "数量")]
        public short 数量 { get; set; }

        [SugarColumn(ColumnName = "载板")]
        public string? 载板 { get; set; }

        [SugarColumn(ColumnName = "玻璃ID")]
        public string? 玻璃ID { get; set; }

        [SugarColumn(ColumnName = "配方")]
        public string? 配方 { get; set; }
    }

    // MySQL层实体
    [SugarTable("device_records")]
    public class MesRecord
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(ColumnName = "source_seq")]
        public int SourceSeq { get; set; }

        [SugarColumn(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }

        [SugarColumn(ColumnName = "quantity")]
        public short Quantity { get; set; }

        [SugarColumn(ColumnName = "carrier_plate")]
        public string? CarrierPlate { get; set; }

        [SugarColumn(ColumnName = "glass_id")]
        public string? GlassId { get; set; }

        [SugarColumn(ColumnName = "recipe")]
        public string? Recipe { get; set; }

        [SugarColumn(ColumnName = "sync_time")]
        public DateTime SyncTime { get; set; }
    }
}
