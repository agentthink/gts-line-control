using System;
using GtsTest.Models; // 确保引用了 DeviceRuntime
using GtsTest.gts;


namespace GtsTest.Core
{
    /// <summary>
    /// 模型层：封装对 gts.mc 的调用，支持模拟模式
    /// </summary>
    public class GtsModel
    {
        // 模拟模式开关
        public static bool UseSimulation { get; set; } = false;

        // 模拟数据存储（按轴号索引，1~8）
        private static double[] _simPos = new double[9];
        private static double[] _simVel = new double[9];
        private static double[] _simAcc = new double[9];
        private static int[] _simStatus = new int[9];
        private static int[] _simMode = new int[9];
        private static bool _simInitialized = false;
        private static uint _simClock = 0;
        private static double[] _simTargetPos = new double[9];
        private static bool[] _simIsMoving = new bool[9];

        // 🆕 急停输入口索引（默认 GPI 第 0 位，可根据硬件接线修改）
        public int EmergencyStopInputIndex { get; set; } = 0;

        /// <summary>
        /// 实时检测硬件环境是否就绪
        /// </summary>
        public static bool CheckHardwareAvailable()
        {
            try
            {
                short result = mc.GT_GetCardNo(out short cardNo);
                if (result == 0) return true;
                else return true; // 能执行到这里说明 DLL 已加载
                // 实际检测应尝试打开卡
                // 简化版本：若 DLL 存在且无异常，认为硬件可用（实际需根据现场调整）
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void InitializeSimulation()
        {
            if (_simInitialized) return;
            for (int i = 1; i <= 8; i++)
            {
                _simPos[i] = 0;
                _simVel[i] = 0;
                _simAcc[i] = 0;
                _simStatus[i] = 0x200;   // 默认使能
                _simMode[i] = 0;
            }
            _simInitialized = true;
        }

        // ---------- 模拟辅助方法 ----------
        private static short SimulateOpen(short card, short mode)
        {
            InitializeSimulation();
            return 0;
        }

        private static short SimulateReset() => 0;

        private static short SimulateGetSts(short axis, out int status, short count, out uint clk)
        {
            status = axis >= 1 && axis <= 8 ? _simStatus[axis] : 0;
            clk = _simClock++;
            return 0;
        }

        private static short SimulateGetPrfPos(short axis, out double pos, short count, out uint clk)
        {
            if (axis >= 1 && axis <= 8)
            {
                if (_simIsMoving[axis])
                {
                    double current = _simPos[axis];
                    double target = _simTargetPos[axis];
                    double step = 50.0;
                    if (Math.Abs(target - current) <= step)
                    {
                        _simPos[axis] = target;
                        _simIsMoving[axis] = false;
                    }
                    else
                    {
                        _simPos[axis] += Math.Sign(target - current) * step;
                    }
                }
                pos = _simPos[axis];
            }
            else pos = 0;
            clk = _simClock++;
            return 0;
        }

        private static short SimulateGetPrfVel(short axis, out double vel, short count, out uint clk)
        {
            if (axis < 1 || axis > 8) { vel = 0; clk = _simClock++; return -1; }
            vel = 50 + 30 * Math.Sin(_simClock * 0.01);
            clk = _simClock++;
            return 0;
        }

        private static short SimulateGetPrfAcc(short axis, out double acc, short count, out uint clk)
        {
            if (axis < 1 || axis > 8) { acc = 0; clk = _simClock++; return -1; }
            acc = 2.5 + Math.Sin(count * 0.1) * 1.5;
            clk = _simClock++;
            return 0;
        }

        private static short SimulateGetPrfMode(short axis, out int mode, short count, out uint clk)
        {
            if (axis < 1 || axis > 8) { mode = 0; clk = _simClock++; return -1; }
            mode = count / 10 % 3;
            clk = _simClock++;
            return 0;
        }

        // ---------- 公开方法（原接口） ----------
        public short OpenDevice(short card, short mode)
        {
            if (UseSimulation) return SimulateOpen(card, mode);
            return mc.GT_Open(card, mode);
        }

        public short GT_Reset()
        {
            if (UseSimulation) return SimulateReset();
            return mc.GT_Reset();
        }

        public short CloseDevice()
        {
            if (UseSimulation) return 0;
            return mc.GT_Close();
        }

        public short GT_Stop(int mask, int option)
        {
            if (UseSimulation) return 0;
            return mc.GT_Stop(mask, option);
        }

        public short GT_AxisOn(short axis)
        {
            if (UseSimulation) return 0;
            return mc.GT_AxisOn(axis);
        }

        public short GT_AxisOff(short axis)
        {
            if (UseSimulation) return 0;
            return mc.GT_AxisOff(axis);
        }

        public short GT_ClrSts(short axis, short count)
        {
            if (UseSimulation) return 0;
            return mc.GT_ClrSts(axis, count);
        }

        /// <summary>
        /// 启动点动（Jog）运动
        /// </summary>
        public short StartJog(short axis, double speed, bool positive)
        {
            if (UseSimulation)
            {
                _simVel[axis] = positive ? speed : -speed;
                return 0;
            }

            short rt = mc.GT_PrfJog(axis);
            if (rt != 0) return rt;

            mc.TJogPrm jogPrm = new mc.TJogPrm
            {
                acc = 10,
                dec = 10,
                smooth = 0
            };
            rt = mc.GT_SetJogPrm(axis, ref jogPrm);
            if (rt != 0) return rt;

            double finalSpeed = positive ? speed : -speed;
            rt = mc.GT_SetVel(axis, finalSpeed);
            if (rt != 0) return rt;

            return mc.GT_Update(1 << axis - 1);
        }

        public short GetAxisStatus(short axis, out int status, out uint clk)
        {
            if (UseSimulation)
                return SimulateGetSts(axis, out status, 1, out clk);
            return mc.GT_GetSts(axis, out status, 1, out clk);
        }

        public short GetPrfPos(short axis, out double pos, out uint clk)
        {
            if (UseSimulation)
                return SimulateGetPrfPos(axis, out pos, 1, out clk);
            return mc.GT_GetPrfPos(axis, out pos, 1, out clk);
        }

        public short GetPrfVel(short axis, out double vel, out uint clk)
        {
            if (UseSimulation)
                return SimulateGetPrfVel(axis, out vel, 1, out clk);
            return mc.GT_GetPrfVel(axis, out vel, 1, out clk);
        }

        public short GetPrfAcc(short axis, out double acc, out uint clk)
        {
            if (UseSimulation)
                return SimulateGetPrfAcc(axis, out acc, 1, out clk);
            return mc.GT_GetPrfAcc(axis, out acc, 1, out clk);
        }

        public short GetPrfMode(short axis, out int mode, out uint clk)
        {
            if (UseSimulation)
                return SimulateGetPrfMode(axis, out mode, 1, out clk);
            return mc.GT_GetPrfMode(axis, out mode, 1, out clk);
        }

        public short GT_EncSns(ushort sValue)
        {
            if (UseSimulation) return 0;
            return mc.GT_EncSns(sValue);
        }

        public short HomeAxis(short axis, int pos, double vel = 20, double acc = 10, int offset = 0)
        {
            if (UseSimulation) return 0;
            return mc.GT_Home(axis, pos, vel, acc, offset);
        }

        public bool CheckHomeDone(short axis)
        {
            if (UseSimulation) return true;
            ushort status = 0;
            short rt = mc.GT_HomeSts(axis, out status);
            if (rt != 0) return false;
            return status == 1;
        }

        public short MoveAbs(short axis, int targetPos, double vel = 10, double acc = 5)
        {
            if (UseSimulation)
            {
                _simTargetPos[axis] = targetPos;
                _simIsMoving[axis] = true;
                return 0;
            }
            short rt = mc.GT_PrfTrap(axis);
            if (rt != 0) return rt;
            rt = mc.GT_SetPos(axis, targetPos);
            if (rt != 0) return rt;
            return mc.GT_Update(1 << axis - 1);
        }

        public bool ReadDI(int ioIndex)
        {
            if (UseSimulation) return ioIndex == 0;
            int value = 0;
            short rt = mc.GT_GetDi(mc.MC_GPI, out value);
            if (rt != 0) return false;
            return (value & (1 << ioIndex)) != 0;
        }


/// <summary>
/// 读取 DI，优先读取强制模拟值（用于现场调试）
/// </summary>
    public bool ReadDIWithForce(int ioIndex, DeviceRuntime? runtime)
    {
        // 优先检查强制模拟
        if (runtime != null && runtime.ForcedIOs.TryGetValue(ioIndex, out bool forcedValue))
        {
            return forcedValue;
        }

        // 没有强制则读取真实硬件
        return ReadDI(ioIndex);
    }
    // ================================================================
    // 🆕 硬件急停检测方法（新增）
    // ================================================================
    /// <summary>
    /// 检测硬件急停按钮是否被按下。
    /// 假设急停按钮连接到 GPI 的指定索引（默认为 0），常闭触点（按下为 0）。
    /// 若使用常开触点，请修改返回值判断逻辑。
    /// </summary>
    /// <returns>true 表示急停被按下，false 表示未按下</returns>
    public bool IsEmergencyStopPressed()
        {
            if (UseSimulation)
            {
                // 在模拟模式下，提供一个虚拟开关（可通过代码模拟）
                // 此处直接返回 false，即模拟模式下不触发硬件急停
                return false;
            }

            try
            {
                int diValue = 0;
                // 读取所有 GPI 输入（MC_GPI 类型）
                short result = mc.GT_GetDi(mc.MC_GPI, out diValue);
                if (result != 0)
                {
                    // 如果读取失败，认为急停未按下（避免误触发）
                    AppLogger.Warn($"读取 GPI 失败，错误码: {result}，急停检测跳过", "GtsModel");
                    return false;
                }

                // 根据配置的索引位检测
                bool isPressed = (diValue & (1 << EmergencyStopInputIndex)) == 0;
                return isPressed;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"急停检测异常: {ex.Message}", "GtsModel");
                return false;
            }
        }
    }
}