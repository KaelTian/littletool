namespace ExcelToPlcJson
{
    /// <summary>
    /// 解析配置
    /// </summary>
    public class ParserConfig
    {
        /// <summary>
        /// 数据起始行（1-based，默认2表示从第二行开始，第一行为表头）
        /// </summary>
        public int StartRow { get; set; } = 2;

        /// <summary>
        /// 列索引映射（1-based）
        /// </summary>
        public int NameColumnIndex { get; set; } = 1;      // 数据名称列
        public int AddressColumnIndex { get; set; } = 4;   // 点位名列（DBD120等）

        /// <summary>
        /// M区基准偏移量（默认0）
        /// </summary>
        public int MAreaBaseOffset { get; set; } = 0;
    }
}
