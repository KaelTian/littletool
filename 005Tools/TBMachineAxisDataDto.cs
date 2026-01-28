using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _005Tools
{
    /// <summary>
    /// 涂布机设备状态枚举（对应API接口MES_DeviceStatus字段）
    /// 状态码映射：0=未初始化 1=初始化中 2=待机中 3=模板切换中 4=手动中 5=报警中 6=待料中 7=自动运行中 8=暂停 9=停止 10=周期停止 11=维护
    /// </summary>
    public enum TBMachineDeviceStatus
    {
        /// <summary>未初始化</summary>
        [Description("未初始化")]
        Uninitialized = 0,

        /// <summary>初始化中</summary>
        [Description("初始化中")]
        Initializing = 1,

        /// <summary>待机中</summary>
        [Description("待机中")]
        Standby = 2,

        /// <summary>模板切换中</summary>
        [Description("模板切换中")]
        TemplateSwitching = 3,

        /// <summary>手动中</summary>
        [Description("手动中")]
        ManualMode = 4,

        /// <summary>报警中</summary>
        [Description("报警中")]
        Alarming = 5,

        /// <summary>待料中</summary>
        [Description("待料中")]
        MaterialWaiting = 6,

        /// <summary>自动运行中</summary>
        [Description("自动运行中")]
        AutoRunning = 7,

        /// <summary>暂停</summary>
        [Description("暂停")]
        Paused = 8,

        /// <summary>停止</summary>
        [Description("停止")]
        Stopped = 9,

        /// <summary>周期停止</summary>
        [Description("周期停止")]
        CycleStopped = 10,

        /// <summary>维护</summary>
        [Description("维护")]
        Maintenance = 11
    }
    /// <summary>
    /// 涂布设备载台及多轴状态总DTO类
    /// 包含载台真空/气缸/传感器信号 + 多根涂布轴的状态信息
    /// </summary>
    public class TBMachineAxisDataDto
    {
        #region 轴
        #region 载台核心信号
        /// <summary>
        /// 载台吸真空判定OK信号（true=真空正常，false=真空异常）
        /// </summary>
        public bool? IsVacuumOK { get; set; }

        /// <summary>
        /// 载台吸真空电磁阀信号（true=电磁阀开启，false=电磁阀关闭）
        /// </summary>
        public bool? VacuumOn { get; set; }

        /// <summary>
        /// 载台破真空电磁阀信号（true=电磁阀开启，false=电磁阀关闭）
        /// </summary>
        public bool? VacuumOff { get; set; }

        /// <summary>
        /// 定位气缸顶升信号（true=顶升，false=回落）
        /// </summary>
        public bool? AlignTop { get; set; }

        /// <summary>
        /// 定位气缸夹紧信号（true=夹紧，false=松开）
        /// </summary>
        public bool? Alignclamp { get; set; }

        /// <summary>
        /// 载台玻璃检测信号（true=检测到玻璃，false=无玻璃）
        /// </summary>
        public bool? GlassSensor { get; set; }

        /// <summary>
        /// 异物检测结果（数值型，不同区间代表不同检测状态）
        /// </summary>
        public float? OtherTestValue1 { get; set; }

        /// <summary>
        /// 左基板厚度实时值（单位：mm）
        /// </summary>
        public float? LeftActThickness { get; set; }

        /// <summary>
        /// 右基板厚度实时值（单位：mm）
        /// </summary>
        public float? RightActThickness { get; set; }
        #endregion

        #region 多轴状态列表
        /// <summary>
        /// 涂布设备所有轴的状态列表（包含多根轴的详情信息）
        /// </summary>
        public List<TBMachineAxisDetailDto>? AxisDetails { get; set; } = new List<TBMachineAxisDetailDto>();
        #endregion
        #endregion
    }
    /// <summary>
    /// 涂布设备轴详情DTO类
    /// 封装单根轴的状态、速度、位置等核心属性
    /// </summary>
    public class TBMachineAxisDetailDto
    {
        /// <summary>
        /// 轴名称（如X轴/Y轴/龙门轴等）
        /// </summary>
        public string? AxisName { get; set; }

        /// <summary>
        /// 电源状态（true=开启，false=关闭）
        /// </summary>
        public bool? PowerStatus { get; set; }

        /// <summary>
        /// 伺服ON状态（true=伺服使能，false=伺服未使能）
        /// </summary>
        public bool? EnableStatus { get; set; }

        /// <summary>
        /// 准备好状态（true=就绪，false=未就绪）
        /// </summary>
        public bool? Readly { get; set; }

        /// <summary>
        /// 回零中状态（true=正在回零，false=未回零）
        /// </summary>
        public bool? Homing { get; set; }

        /// <summary>
        /// 工作中状态（true=运行中，false=停止）
        /// </summary>
        public bool? Working { get; set; }

        /// <summary>
        /// 已到位状态（true=到达目标位置，false=未到位）
        /// </summary>
        public bool? Worked { get; set; }

        /// <summary>
        /// 正限位触发状态（true=触发，false=未触发）
        /// </summary>
        public bool? Phmt { get; set; }

        /// <summary>
        /// 负限位触发状态（true=触发，false=未触发）
        /// </summary>
        public bool? Nhmt { get; set; }

        /// <summary>
        /// 原点状态（true=在原点，false=偏离原点）
        /// </summary>
        public bool? Origin { get; set; }

        /// <summary>
        /// 报警中状态（true=报警，false=正常）
        /// </summary>
        public bool? Alarm { get; set; }

        /// <summary>
        /// 同步中状态（true=同步运行，false=非同步）
        /// </summary>
        public bool? Sysc { get; set; }

        /// <summary>
        /// 轴运行速度（单位：mm/s）
        /// </summary>
        public float? Speed { get; set; }

        /// <summary>
        /// 轴当前位置（单位：mm）
        /// </summary>
        public float? Pos { get; set; }
    }
}
