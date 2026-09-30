using System;
using System.Collections.Generic;
using System.Linq;

namespace GtsTest.Modbus
{
    public static class ModbusFormatter
    {
        public static string Format(object? value, DisplayFormat format, ByteOrder byteOrder = ByteOrder.BigEndian)
        {
            if (value == null) return "null";

            // ---- 1. 处理 ASCII 格式的数组 ----
            if (format == DisplayFormat.Ascii && value is Array asciiArray)
            {
                var byteList = new List<byte>();
                foreach (var elem in asciiArray)
                {
                    ushort tempVal;
                    if (elem is ushort usItem)
                        tempVal = usItem;
                    else if (elem is short sItem)
                        tempVal = (ushort)sItem;
                    else
                        continue;

                    if (byteOrder == ByteOrder.BigEndian)
                    {
                        byteList.Add((byte)(tempVal >> 8));
                        byteList.Add((byte)(tempVal & 0xFF));
                    }
                    else // LittleEndian
                    {
                        byteList.Add((byte)(tempVal & 0xFF));
                        byteList.Add((byte)(tempVal >> 8));
                    }
                }

                var chars = byteList.Select(b => (b >= 0x20 && b <= 0x7E) ? (char)b : '.').ToArray();
                return new string(chars);
            }

            // ---- 2. 其他格式的数组 ----
            if (value is Array generalArray)
            {
                var items = new List<string>();
                foreach (var elem in generalArray)
                {
                    if (elem is short shortVal)
                        items.Add(FormatNumber(shortVal, format));
                    else if (elem is ushort ushortVal)
                        items.Add(FormatNumber(ushortVal, format));
                    else if (elem is int intVal)
                        items.Add(FormatNumber(intVal, format));
                    else if (elem is uint uintVal)
                        items.Add(FormatNumber(uintVal, format));
                    else if (elem is float floatVal)
                        items.Add(floatVal.ToString("F6"));
                    else if (elem is double doubleVal)
                        items.Add(doubleVal.ToString("F6"));
                    else
                        items.Add(elem?.ToString() ?? "");
                }
                return string.Join(", ", items);
            }

            // ---- 3. 单值处理（单寄存器不受字节序影响，因为不涉及跨寄存器组合） ----
            if (value is short singleShort)
                return FormatNumber(singleShort, format);
            if (value is ushort singleUShort)
                return FormatNumber(singleUShort, format);
            if (value is int singleInt)
                return FormatNumber(singleInt, format);
            if (value is uint singleUInt)
                return FormatNumber(singleUInt, format);
            if (value is float singleFloat)
                return singleFloat.ToString("F6");
            if (value is double singleDouble)
                return singleDouble.ToString("F6");

            return value.ToString() ?? "";
        }

        private static string FormatNumber(long num, DisplayFormat format)
        {
            return format switch
            {
                DisplayFormat.Decimal => num.ToString(),
                DisplayFormat.HexWithPrefix => $"0x{num:X}",
                DisplayFormat.HexNoPrefix => $"{num:X}",
                DisplayFormat.Octal => Convert.ToString(num, 8),
                DisplayFormat.Binary => Convert.ToString(num, 2),
                DisplayFormat.Ascii => AsciiFromShort((ushort)num),
                _ => num.ToString()
            };
        }

        private static string AsciiFromShort(ushort val)
        {
            char high = (val >> 8) >= 0x20 && (val >> 8) <= 0x7E ? (char)(val >> 8) : '.';
            char low = (val & 0xFF) >= 0x20 && (val & 0xFF) <= 0x7E ? (char)(val & 0xFF) : '.';
            return $"{high}{low}";
        }
    }
}