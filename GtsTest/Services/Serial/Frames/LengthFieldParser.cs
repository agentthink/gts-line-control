using System;
using System.Collections.Generic;
using System.Linq;

namespace GtsTest.Services.Serial.Frames
{
    /// <summary>
    /// 变长帧解析器：帧头 + 长度字段 + 载荷 + (可选 CRC16)
    /// </summary>
    public class LengthFieldParser : ISerialFrameParser
    {
        private readonly byte[] _header;
        private readonly int _lengthFieldOffset;
        private readonly int _lengthFieldSize;
        private readonly bool _lengthIncludesHeader;
        private readonly bool _useCrc16;
        private readonly List<byte> _buffer = new();

        public LengthFieldParser(byte[] header, int lengthFieldOffset, int lengthFieldSize,
            bool lengthIncludesHeader, bool useCrc16)
        {
            _header = header ?? throw new ArgumentNullException(nameof(header));
            _lengthFieldOffset = lengthFieldOffset;
            _lengthFieldSize = lengthFieldSize == 1 || lengthFieldSize == 2 ? lengthFieldSize : 1;
            _lengthIncludesHeader = lengthIncludesHeader;
            _useCrc16 = useCrc16;
        }

        public List<byte[]> Push(byte[] newData)
        {
            var result = new List<byte[]>();
            _buffer.AddRange(newData);

            while (true)
            {
                // 1. 找帧头
                int headerIdx = FindHeader(_buffer);
                if (headerIdx < 0)
                {
                    // 没找到，保留末尾可能是不完整帧头的一部分
                    if (_buffer.Count > _header.Length)
                        _buffer.RemoveRange(0, _buffer.Count - _header.Length);
                    return result;
                }

                // 丢弃帧头之前的垃圾数据
                if (headerIdx > 0) _buffer.RemoveRange(0, headerIdx);

                // 2. 至少需要帧头 + 长度字段
                int minLen = Math.Max(_header.Length, _lengthFieldOffset + _lengthFieldSize);
                if (_buffer.Count < minLen) return result;

                // 3. 读取长度字段
                int lenValue = 0;
                for (int i = 0; i < _lengthFieldSize; i++)
                {
                    lenValue = (lenValue << 8) | _buffer[_lengthFieldOffset + i];
                }

                // 4. 计算整帧长度
                int frameLen = _lengthIncludesHeader ? lenValue : lenValue + _lengthFieldOffset + _lengthFieldSize;

                if (_useCrc16) frameLen += 2;  // CRC 占 2 字节

                // 5. 帧不完整，等下一批数据
                if (frameLen <= 0 || frameLen > 65535)
                {
                    // 长度字段明显异常，丢弃 1 字节，继续搜
                    _buffer.RemoveAt(0);
                    continue;
                }

                if (_buffer.Count < frameLen) return result;

                // 6. 提取完整帧
                byte[] frame = _buffer.GetRange(0, frameLen).ToArray();

                // 7. CRC 校验
                if (_useCrc16 && !VerifyCrc16(frame))
                {
                    // CRC 错，丢弃 1 字节重搜
                    _buffer.RemoveAt(0);
                    continue;
                }

                _buffer.RemoveRange(0, frameLen);
                result.Add(frame);
            }
        }

        private int FindHeader(List<byte> buf)
        {
            for (int i = 0; i <= buf.Count - _header.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < _header.Length; j++)
                {
                    if (buf[i + j] != _header[j]) { match = false; break; }
                }
                if (match) return i;
            }
            return -1;
        }

        private static bool VerifyCrc16(byte[] frame)
        {
            if (frame.Length < 3) return false;
            ushort expected = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));
            ushort actual = Crc16Modbus(frame, 0, frame.Length - 2);
            return expected == actual;
        }

        /// <summary>Modbus CRC16 校验</summary>
        public static ushort Crc16Modbus(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0) crc = (ushort)((crc >> 1) ^ 0xA001);
                    else crc >>= 1;
                }
            }
            return crc;
        }

        public void Reset() => _buffer.Clear();
    }
}