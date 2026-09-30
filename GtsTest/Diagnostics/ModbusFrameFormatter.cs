using System;
using System.Linq;
using System.Text;

namespace GtsTest.Diagnostics
{
    /// <summary>
    /// Modbus 报文格式化工具
    /// 把"从机地址 + 功能码 + 参数 + 返回数据"格式化为可读文本
    /// </summary>
    public static class ModbusFrameFormatter
    {
        /// <summary>格式化字节数组为十六进制字符串（大写，空格分隔）</summary>
        public static string Hex(byte[]? data)
        {
            if (data == null || data.Length == 0) return "";
            return BitConverter.ToString(data).Replace("-", " ");
        }

        /// <summary>格式化 ushort[] 为十六进制字符串</summary>
        public static string Hex(ushort[]? data)
        {
            if (data == null || data.Length == 0) return "";
            return string.Join(" ", data.Select(v => v.ToString("X4")));
        }

        // ================================================================
        // 请求帧摘要 + 近似报文
        // ================================================================

        /// <summary>读保持寄存器请求（0x03）</summary>
        public static (string summary, string payload) FormatReadHoldingRequest(byte slave, ushort addr, ushort count)
        {
            byte[] pdu =
            {
                slave, 0x03,
                (byte)(addr >> 8), (byte)(addr & 0xFF),
                (byte)(count >> 8), (byte)(count & 0xFF)
            };
            string summary = $"从机={slave:D2} 读保持寄存器 起始=0x{addr:X4} 数量={count}";
            return (summary, Hex(pdu));
        }

        /// <summary>读保持寄存器响应（0x03）</summary>
        public static (string summary, string payload) FormatReadHoldingResponse(byte slave, ushort[] data)
        {
            if (data == null || data.Length == 0)
                return ($"从机={slave:D2} 读保持响应 空", "");

            // 近似帧：slave + 0x03 + byteCount + data
            var bytes = new byte[2 + data.Length * 2];
            bytes[0] = slave;
            bytes[1] = 0x03;
            for (int i = 0; i < data.Length; i++)
            {
                bytes[2 + i * 2] = (byte)(data[i] >> 8);
                bytes[3 + i * 2] = (byte)(data[i] & 0xFF);
            }

            string summary = $"从机={slave:D2} 读保持响应 {data.Length} 个寄存器 [{Hex(data)}]";
            return (summary, Hex(bytes));
        }

        /// <summary>写单寄存器请求（0x06）</summary>
        public static (string summary, string payload) FormatWriteSingleRegister(byte slave, ushort addr, ushort value)
        {
            byte[] pdu =
            {
                slave, 0x06,
                (byte)(addr >> 8), (byte)(addr & 0xFF),
                (byte)(value >> 8), (byte)(value & 0xFF)
            };
            string summary = $"从机={slave:D2} 写单寄存器 地址=0x{addr:X4} 值=0x{value:X4}";
            return (summary, Hex(pdu));
        }

        /// <summary>写多寄存器请求（0x10）</summary>
        public static (string summary, string payload) FormatWriteMultipleRegisters(byte slave, ushort addr, ushort[] values)
        {
            int byteCount = values.Length * 2;
            var pdu = new byte[7 + byteCount];
            pdu[0] = slave;
            pdu[1] = 0x10;
            pdu[2] = (byte)(addr >> 8);
            pdu[3] = (byte)(addr & 0xFF);
            pdu[4] = (byte)(values.Length >> 8);
            pdu[5] = (byte)(values.Length & 0xFF);
            pdu[6] = (byte)byteCount;
            for (int i = 0; i < values.Length; i++)
            {
                pdu[7 + i * 2] = (byte)(values[i] >> 8);
                pdu[8 + i * 2] = (byte)(values[i] & 0xFF);
            }
            string summary = $"从机={slave:D2} 写多寄存器 起始=0x{addr:X4} 数量={values.Length} 值=[{Hex(values)}]";
            return (summary, Hex(pdu));
        }

        /// <summary>写单线圈请求（0x05）</summary>
        public static (string summary, string payload) FormatWriteSingleCoil(byte slave, ushort addr, bool value)
        {
            ushort val = (ushort)(value ? 0xFF00 : 0x0000);
            byte[] pdu =
            {
                slave, 0x05,
                (byte)(addr >> 8), (byte)(addr & 0xFF),
                (byte)(val >> 8), (byte)(val & 0xFF)
            };
            string summary = $"从机={slave:D2} 写单线圈 地址=0x{addr:X4} 值={(value ? "ON" : "OFF")}";
            return (summary, Hex(pdu));
        }

        /// <summary>写多线圈请求（0x0F）</summary>
        public static (string summary, string payload) FormatWriteMultipleCoils(byte slave, ushort addr, bool[] values)
        {
            int byteCount = (values.Length + 7) / 8;
            var pdu = new byte[7 + byteCount];
            pdu[0] = slave;
            pdu[1] = 0x0F;
            pdu[2] = (byte)(addr >> 8);
            pdu[3] = (byte)(addr & 0xFF);
            pdu[4] = (byte)(values.Length >> 8);
            pdu[5] = (byte)(values.Length & 0xFF);
            pdu[6] = (byte)byteCount;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                    pdu[7 + (i / 8)] |= (byte)(1 << (i % 8));
            }
            string summary = $"从机={slave:D2} 写多线圈 起始=0x{addr:X4} 数量={values.Length}";
            return (summary, Hex(pdu));
        }

        /// <summary>读线圈请求（0x01）</summary>
        public static (string summary, string payload) FormatReadCoilsRequest(byte slave, ushort addr, ushort count)
        {
            byte[] pdu =
            {
                slave, 0x01,
                (byte)(addr >> 8), (byte)(addr & 0xFF),
                (byte)(count >> 8), (byte)(count & 0xFF)
            };
            return ($"从机={slave:D2} 读线圈 起始=0x{addr:X4} 数量={count}", Hex(pdu));
        }

        /// <summary>读输入寄存器请求（0x04）</summary>
        public static (string summary, string payload) FormatReadInputRequest(byte slave, ushort addr, ushort count)
        {
            byte[] pdu =
            {
                slave, 0x04,
                (byte)(addr >> 8), (byte)(addr & 0xFF),
                (byte)(count >> 8), (byte)(count & 0xFF)
            };
            return ($"从机={slave:D2} 读输入寄存器 起始=0x{addr:X4} 数量={count}", Hex(pdu));
        }

        /// <summary>读离散输入请求（0x02）</summary>
        public static (string summary, string payload) FormatReadDiscreteRequest(byte slave, ushort addr, ushort count)
        {
            byte[] pdu =
            {
                slave, 0x02,
                (byte)(addr >> 8), (byte)(addr & 0xFF),
                (byte)(count >> 8), (byte)(count & 0xFF)
            };
            return ($"从机={slave:D2} 读离散输入 起始=0x{addr:X4} 数量={count}", Hex(pdu));
        }

        /// <summary>异常响应</summary>
        public static (string summary, string payload) FormatException(byte slave, byte functionCode, string errorMsg)
        {
            byte[] pdu = { slave, (byte)(functionCode | 0x80), 0x03 };
            return ($"从机={slave:D2} 异常响应 FC=0x{functionCode:X2} 错误: {errorMsg}", Hex(pdu));
        }

        /// <summary>通用错误</summary>
        public static (string summary, string payload) FormatError(string operation, string errorMsg)
        {
            return ($"{operation} 失败: {errorMsg}", "");
        }

        /// <summary>连接/断开事件</summary>
        public static string FormatConnectionEvent(bool connected, string info)
        {
            return connected ? $"✅ 连接成功: {info}" : $"❌ 连接断开: {info}";
        }
    }
}