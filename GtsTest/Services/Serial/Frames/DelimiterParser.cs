using System;
using System.Collections.Generic;
using System.Linq;

namespace GtsTest.Services.Serial.Frames
{
    /// <summary>
    /// 分隔符帧解析器（如以 \r\n 结尾）
    /// </summary>
    public class DelimiterParser : ISerialFrameParser
    {
        private readonly byte[] _delimiter;
        private readonly int _maxFrameLength;
        private readonly List<byte> _buffer = new();

        public DelimiterParser(byte[] delimiter, int maxFrameLength = 4096)
        {
            _delimiter = delimiter ?? throw new ArgumentNullException(nameof(delimiter));
            _maxFrameLength = maxFrameLength;
        }

        public List<byte[]> Push(byte[] newData)
        {
            var result = new List<byte[]>();
            _buffer.AddRange(newData);

            while (true)
            {
                int delimIdx = FindDelimiter(_buffer, out int matchLen);
                if (delimIdx < 0)
                {
                    // 缓冲区过大，防止内存泄漏
                    if (_buffer.Count > _maxFrameLength)
                        _buffer.RemoveRange(0, _buffer.Count - _maxFrameLength);
                    return result;
                }

                int frameLen = delimIdx + matchLen;
                byte[] frame = _buffer.GetRange(0, frameLen).ToArray();
                _buffer.RemoveRange(0, frameLen);
                result.Add(frame);
            }
        }

        private int FindDelimiter(List<byte> buf, out int matchLen)
        {
            matchLen = 0;
            for (int i = 0; i <= buf.Count - _delimiter.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < _delimiter.Length; j++)
                {
                    if (buf[i + j] != _delimiter[j]) { match = false; break; }
                }
                if (match) { matchLen = _delimiter.Length; return i; }
            }
            return -1;
        }

        public void Reset() => _buffer.Clear();
    }
}