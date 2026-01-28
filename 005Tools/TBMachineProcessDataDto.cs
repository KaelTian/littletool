using System.ComponentModel;

namespace _005Tools
{
    /// <summary>
    /// 涂布设备工艺数据DTO类
    /// 包含涂布、预涂、清洗、风刀等全流程工艺参数
    /// 所有值类型属性均为可空，适配参数未配置/空值场景
    /// </summary>
    public class TBMachineProcessDataDto
    {
        #region 配方

        #region 基础配方信息
        /// <summary>
        /// 配方名称
        /// </summary>
        public string? ProcessDataRecipeName { get; set; }

        /// <summary>
        /// 玻璃长度
        /// </summary>
        public float? ProcessDataGlassLength { get; set; }

        /// <summary>
        /// 玻璃宽度
        /// </summary>
        public float? ProcessDataGlassWidth { get; set; }

        /// <summary>
        /// 玻璃厚度
        /// </summary>
        public float? ProcessDataGlassThickness { get; set; }

        /// <summary>
        /// 起点位置
        /// </summary>
        public float? ProcessDataGlassDisStart { get; set; }

        /// <summary>
        /// 终点位置
        /// </summary>
        public float? ProcessDataGlassDisEnd { get; set; }

        /// <summary>
        /// 配方号
        /// </summary>
        public short? ProcessDataRecipeID { get; set; }
        #endregion

        #region 涂布柱塞泵参数
        /// <summary>
        /// 柱塞泵时间1
        /// </summary>
        public float? CoatPumpTime1 { get; set; }

        /// <summary>
        /// 柱塞泵时间2
        /// </summary>
        public float? CoatPumpTime2 { get; set; }

        /// <summary>
        /// 柱塞泵时间3
        /// </summary>
        public float? CoatPumpTime3 { get; set; }

        /// <summary>
        /// 柱塞泵时间4
        /// </summary>
        public float? CoatPumpTime4 { get; set; }

        /// <summary>
        /// 柱塞泵时间5
        /// </summary>
        public float? CoatPumpTime5 { get; set; }

        /// <summary>
        /// 柱塞泵时间6
        /// </summary>
        public float? CoatPumpTime6 { get; set; }

        /// <summary>
        /// 柱塞泵时间7
        /// </summary>
        public float? CoatPumpTime7 { get; set; }

        /// <summary>
        /// 柱塞泵时间8
        /// </summary>
        public float? CoatPumpTime8 { get; set; }

        /// <summary>
        /// 柱塞泵时间9
        /// </summary>
        public float? CoatPumpTime9 { get; set; }

        /// <summary>
        /// 柱塞泵时间10
        /// </summary>
        public float? CoatPumpTime10 { get; set; }

        /// <summary>
        /// 柱塞泵时间11
        /// </summary>
        public float? CoatPumpTime11 { get; set; }

        /// <summary>
        /// 柱塞泵时间12
        /// </summary>
        public float? CoatPumpTime12 { get; set; }

        /// <summary>
        /// 柱塞泵时间13
        /// </summary>
        public float? CoatPumpTime13 { get; set; }

        /// <summary>
        /// 柱塞泵时间14
        /// </summary>
        public float? CoatPumpTime14 { get; set; }

        /// <summary>
        /// 柱塞泵时间15
        /// </summary>
        public float? CoatPumpTime15 { get; set; }

        /// <summary>
        /// 柱塞泵时间16
        /// </summary>
        public float? CoatPumpTime16 { get; set; }

        /// <summary>
        /// 柱塞泵时间17
        /// </summary>
        public float? CoatPumpTime17 { get; set; }

        /// <summary>
        /// 柱塞泵时间18
        /// </summary>
        public float? CoatPumpTime18 { get; set; }

        /// <summary>
        /// 柱塞泵时间19
        /// </summary>
        public float? CoatPumpTime19 { get; set; }

        /// <summary>
        /// 柱塞泵时间20
        /// </summary>
        public float? CoatPumpTime20 { get; set; }

        /// <summary>
        /// 补液后排泡时间(秒)
        /// </summary>
        public float? CoatPumpTime21 { get; set; }

        /// <summary>
        /// 柱塞泵速度1
        /// </summary>
        public float? CoatPumpSpeed1 { get; set; }

        /// <summary>
        /// 柱塞泵速度2
        /// </summary>
        public float? CoatPumpSpeed2 { get; set; }

        /// <summary>
        /// 柱塞泵速度3
        /// </summary>
        public float? CoatPumpSpeed3 { get; set; }

        /// <summary>
        /// 柱塞泵速度4
        /// </summary>
        public float? CoatPumpSpeed4 { get; set; }

        /// <summary>
        /// 柱塞泵速度5
        /// </summary>
        public float? CoatPumpSpeed5 { get; set; }

        /// <summary>
        /// 柱塞泵速度6
        /// </summary>
        public float? CoatPumpSpeed6 { get; set; }

        /// <summary>
        /// 补液速度
        /// </summary>
        public float? CoatPumpSpeed7 { get; set; }
        #endregion

        #region 涂布龙门参数
        /// <summary>
        /// 龙门时间1
        /// </summary>
        public float? CoatGantryTime1 { get; set; }

        /// <summary>
        /// 龙门时间2
        /// </summary>
        public float? CoatGantryTime2 { get; set; }

        /// <summary>
        /// 龙门时间3
        /// </summary>
        public float? CoatGantryTime3 { get; set; }

        /// <summary>
        /// 龙门时间4
        /// </summary>
        public float? CoatGantryTime4 { get; set; }

        /// <summary>
        /// 龙门时间5
        /// </summary>
        public float? CoatGantryTime5 { get; set; }

        /// <summary>
        /// 龙门时间6
        /// </summary>
        public float? CoatGantryTime6 { get; set; }

        /// <summary>
        /// 龙门时间7
        /// </summary>
        public float? CoatGantryTime7 { get; set; }

        /// <summary>
        /// 龙门时间8
        /// </summary>
        public float? CoatGantryTime8 { get; set; }

        /// <summary>
        /// 龙门时间9
        /// </summary>
        public float? CoatGantryTime9 { get; set; }

        /// <summary>
        /// 龙门速度1
        /// </summary>
        public float? CoatGantrySpeed1 { get; set; }

        /// <summary>
        /// 龙门速度2
        /// </summary>
        public float? CoatGantrySpeed2 { get; set; }

        /// <summary>
        /// 龙门速度3
        /// </summary>
        public float? CoatGantrySpeed3 { get; set; }
        #endregion

        #region 涂布模头参数
        /// <summary>
        /// 模头时间1
        /// </summary>
        public float? CoatNozzleTime1 { get; set; }

        /// <summary>
        /// 模头时间2
        /// </summary>
        public float? CoatNozzleTime2 { get; set; }

        /// <summary>
        /// 模头时间3
        /// </summary>
        public float? CoatNozzleTime3 { get; set; }

        /// <summary>
        /// 模头时间4
        /// </summary>
        public float? CoatNozzleTime4 { get; set; }

        /// <summary>
        /// 模头时间5
        /// </summary>
        public float? CoatNozzleTime5 { get; set; }

        /// <summary>
        /// 模头时间6
        /// </summary>
        public float? CoatNozzleTime6 { get; set; }

        /// <summary>
        /// 模头时间7
        /// </summary>
        public float? CoatNozzleTime7 { get; set; }

        /// <summary>
        /// 模头时间8
        /// </summary>
        public float? CoatNozzleTime8 { get; set; }

        /// <summary>
        /// 模头时间9
        /// </summary>
        public float? CoatNozzleTime9 { get; set; }

        /// <summary>
        /// 涂布后排泡时间(秒)
        /// </summary>
        public float? CoatNozzleTime10 { get; set; }

        /// <summary>
        /// 模头位置1
        /// </summary>
        public float? CoatNozzlePos1 { get; set; }

        /// <summary>
        /// 模头位置2
        /// </summary>
        public float? CoatNozzlePos2 { get; set; }

        /// <summary>
        /// 模头位置3
        /// </summary>
        public float? CoatNozzlePos3 { get; set; }

        /// <summary>
        /// 模头位置4
        /// </summary>
        public float? CoatNozzlePos4 { get; set; }

        /// <summary>
        /// 模头位置5
        /// </summary>
        public float? CoatNozzlePos5 { get; set; }
        #endregion

        #region 预涂柱塞泵参数
        /// <summary>
        /// 预涂-柱塞泵时间1
        /// </summary>
        public float? PreCoatPumpTime1 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵时间2
        /// </summary>
        public float? PreCoatPumpTime2 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵时间3
        /// </summary>
        public float? PreCoatPumpTime3 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵时间4
        /// </summary>
        public float? PreCoatPumpTime4 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵时间5
        /// </summary>
        public float? PreCoatPumpTime5 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵时间6
        /// </summary>
        public float? PreCoatPumpTime6 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵速度1
        /// </summary>
        public float? PreCoatPumpSpeed1 { get; set; }

        /// <summary>
        /// 预涂-柱塞泵速度2
        /// </summary>
        public float? PreCoatPumpSpeed2 { get; set; }
        #endregion

        #region 预涂模头参数
        /// <summary>
        /// 预涂-模头时间1
        /// </summary>
        public float? PreCoatDieHeadTime1 { get; set; }

        /// <summary>
        /// 预涂-模头时间2
        /// </summary>
        public float? PreCoatDieHeadTime2 { get; set; }

        /// <summary>
        /// 预涂-模头时间3
        /// </summary>
        public float? PreCoatDieHeadTime3 { get; set; }

        /// <summary>
        /// 预涂-模头位置1
        /// </summary>
        public float? PreCoatDieHeadPos1 { get; set; }

        /// <summary>
        /// 预涂-模头位置2
        /// </summary>
        public float? PreCoatDieHeadPos2 { get; set; }
        #endregion

        #region 预涂辊轮参数
        /// <summary>
        /// 预涂-辊轮时间1
        /// </summary>
        public float? PreCoatRTime1 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间2
        /// </summary>
        public float? PreCoatRTime2 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间3
        /// </summary>
        public float? PreCoatRTime3 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间4
        /// </summary>
        public float? PreCoatRTime4 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间5
        /// </summary>
        public float? PreCoatRTime5 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间6
        /// </summary>
        public float? PreCoatRTime6 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间7
        /// </summary>
        public float? PreCoatRTime7 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间8
        /// </summary>
        public float? PreCoatRTime8 { get; set; }

        /// <summary>
        /// 预涂-辊轮时间9
        /// </summary>
        public float? PreCoatRTime9 { get; set; }

        /// <summary>
        /// 辊轮预涂水刀关闭延时时间
        /// </summary>
        public float? WaterKnifeDelayTime { get; set; }

        /// <summary>
        /// 辊轮预涂气刀关闭延时时间
        /// </summary>
        public float? GasKnifeDelayTime { get; set; }

        /// <summary>
        /// 预涂-辊轮速度1
        /// </summary>
        public float? PreCoatRSpeed1 { get; set; }

        /// <summary>
        /// 预涂-辊轮速度2
        /// </summary>
        public float? PreCoatRSpeed2 { get; set; }

        /// <summary>
        /// 预涂-辊轮速度3
        /// </summary>
        public float? PreCoatRSpeed3 { get; set; }
        #endregion

        #region 清洗柱塞泵参数
        /// <summary>
        /// 清洗-柱塞泵时间1
        /// </summary>
        public float? ClearPumpTime1 { get; set; }

        /// <summary>
        /// 清洗-柱塞泵时间2
        /// </summary>
        public float? ClearPumpTime2 { get; set; }

        /// <summary>
        /// 清洗-柱塞泵时间3
        /// </summary>
        public float? ClearPumpTime3 { get; set; }

        /// <summary>
        /// 清洗-柱塞泵时间4
        /// </summary>
        public float? ClearPumpTime4 { get; set; }

        /// <summary>
        /// 清洗-柱塞泵速度1
        /// </summary>
        public float? ClearPumpSpeed1 { get; set; }
        #endregion

        #region 清洗轴参数
        /// <summary>
        /// 清洗-清洗轴时间1
        /// </summary>
        public float? ClearCleanTime1 { get; set; }

        /// <summary>
        /// 清洗-清洗轴时间2
        /// </summary>
        public float? ClearCleanTime2 { get; set; }

        /// <summary>
        /// 清洗-清洗轴时间3
        /// </summary>
        public float? ClearCleanTime3 { get; set; }

        /// <summary>
        /// 清洗-清洗轴时间4
        /// </summary>
        public float? ClearCleanTime4 { get; set; }

        /// <summary>
        /// 清洗-清洗轴时间5
        /// </summary>
        public float? ClearCleanTime5 { get; set; }

        /// <summary>
        /// 刮刀水洗次数
        /// </summary>
        public float? DrawknifeWashCount { get; set; }

        /// <summary>
        /// 刮刀吹气次数
        /// </summary>
        public float? DrawknifeBlowCount { get; set; }

        /// <summary>
        /// 清洗-清洗轴速度1
        /// </summary>
        public float? ClearCleanSpeed1 { get; set; }
        #endregion

        #region 清洗模头参数
        /// <summary>
        /// 清洗-模头时间1
        /// </summary>
        public float? ClearDieHeadTime1 { get; set; }

        /// <summary>
        /// 清洗-模头时间2
        /// </summary>
        public float? ClearDieHeadTime2 { get; set; }

        /// <summary>
        /// 清洗-模头时间3
        /// </summary>
        public float? ClearDieHeadTime3 { get; set; }

        /// <summary>
        /// 清洗-模头时间4
        /// </summary>
        public float? ClearDieHeadTime4 { get; set; }

        /// <summary>
        /// 清洗-模头位置1
        /// </summary>
        public float? ClearDieHeadPos1 { get; set; }

        /// <summary>
        /// 清洗-模头位置2
        /// </summary>
        public float? ClearDieHeadPos2 { get; set; }
        #endregion

        #region 风刀参数
        /// <summary>
        /// 风刀功能
        /// </summary>
        public float? WindKnifeMode { get; set; }

        /// <summary>
        /// 风刀延时启动时间
        /// </summary>
        public float? WindKnifeDelayStartTime { get; set; }

        /// <summary>
        /// 风刀延时停止时间
        /// </summary>
        public float? WindKnifeDelayCloseTime { get; set; }

        /// <summary>
        /// 风刀启动平台位置
        /// </summary>
        public float? WindKnifeStartPos { get; set; }

        /// <summary>
        /// 风刀关闭平台位置
        /// </summary>
        public float? WindKnifeClosePos { get; set; }

        /// <summary>
        /// 风刀运行速度
        /// </summary>
        public float? WindKnifeGoSpeed { get; set; }

        /// <summary>
        /// 风刀关闭等待时间
        /// </summary>
        public float? WindKnifePosWaitDelayTime { get; set; }
        #endregion

        #endregion

        #region 设备状态
        /// <summary>
        /// 设备状态(0：未初始化 1：初始化中 2：待机中 3：模板切换中 4：手动中 5：报警中 6：待料中 7：自动运行中 8：暂停 9：停止 10：周期停止 11：维护)
        /// </summary>
        [Description("设备状态(0：未初始化 1：初始化中 2：待机中 3：模板切换中 4：手动中 5：报警中 6：待料中 7：自动运行中 8：暂停 9：停止 10：周期停止 11：维护)")]
        public TBMachineDeviceStatus? MES_DeviceStatus { get; set; }
        #endregion
    }
}
