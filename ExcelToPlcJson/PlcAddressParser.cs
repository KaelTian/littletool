using System.Text.RegularExpressions;

namespace ExcelToPlcJson
{
    /// <summary>
    /// PLC地址解析器
    /// </summary>
    public class PlcAddressParser
    {
        private readonly ParserConfig _config;

        // 地址格式正则：DBD123, DBW52, DBB10, DBX1228.0 等
        private readonly Regex _dbRegex = new Regex(@"^(DB)([DWB])(\d+)$", RegexOptions.IgnoreCase);
        private readonly Regex _dbxRegex = new Regex(@"^DBX(\d+)\.(\d+)$", RegexOptions.IgnoreCase); // 新增：DBX 位地址
        private readonly Regex _mRegex = new Regex(@"^M(\d+)\.(\d+)$", RegexOptions.IgnoreCase);
        private readonly Regex _simpleMRegex = new Regex(@"^M(\d+)$", RegexOptions.IgnoreCase);

        public PlcAddressParser(ParserConfig? config = null)
        {
            _config = config ?? new ParserConfig();
        }

        /// <summary>
        /// 解析PLC地址字符串
        /// </summary>
        /// <param name="address">原始地址如 DBD120, DBX1228.0, M20.6</param>
        /// <returns>(Offset, Type) 元组</returns>
        public (string offset, string type) Parse(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("地址不能为空");

            address = address.Trim().ToUpper();

            // 1. 解析 DBX 区 (DBX1228.0 -> BOOL，位地址)
            var dbxMatch = _dbxRegex.Match(address);
            if (dbxMatch.Success)
            {
                string byteAddr = dbxMatch.Groups[1].Value;  // 1228
                string bitAddr = dbxMatch.Groups[2].Value;   // 0
                return ($"{byteAddr}.{bitAddr}", "BOOL");
            }

            // 2. 解析 DB 区 (DBD120, DBW52, DBB10 等)
            var dbMatch = _dbRegex.Match(address);
            if (dbMatch.Success)
            {
                string dataType = dbMatch.Groups[2].Value; // D, W, B
                string offsetNum = dbMatch.Groups[3].Value;

                string type = dataType switch
                {
                    "D" => "REAL",  // DBD -> Double Word -> REAL
                    "W" => "WORD",  // DBW -> Word -> WORD
                    "B" => "BYTE",  // DBB -> Byte -> BYTE
                    _ => "UNKNOWN"
                };

                // DBD是4字节，DBW是2字节，但Offset格式都是 "地址.0"
                return ($"{offsetNum}.0", type);
            }

            // 3. 解析 M 区 (M20.6 格式)
            var mMatch = _mRegex.Match(address);
            if (mMatch.Success)
            {
                int baseAddr = int.Parse(mMatch.Groups[1].Value);
                int bitAddr = int.Parse(mMatch.Groups[2].Value);
                int finalAddr = baseAddr + _config.MAreaBaseOffset;

                return ($"{finalAddr}.{bitAddr}", "BOOL");
            }

            // 4. 解析 M 区 (M100 格式，无小数点，默认为 .0)
            var simpleMMatch = _simpleMRegex.Match(address);
            if (simpleMMatch.Success)
            {
                int baseAddr = int.Parse(simpleMMatch.Groups[1].Value);
                int finalAddr = baseAddr + _config.MAreaBaseOffset;

                return ($"{finalAddr}.0", "BOOL");
            }

            throw new FormatException($"不支持的地址格式: {address}");
        }
    }
}
