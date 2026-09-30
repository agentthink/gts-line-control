using System.Collections.Generic;

namespace GtsTest.Services.Serial.Frames
{
    /// <summary>
    /// 定长帧解析器
    /// </summary>
    public class FixedLengthParser : ISerialFrameParser
    {
        private readonly int _frameLength;
        private readonly List<byte> _buffer = new();

        public FixedLengthParser(int frameLength)
        {
            _frameLength = frameLength > 0 ? frameLength : 1;
        }

        public List<byte[]> Push(byte[] newData)
        {
            var result = new List<byte[]>();
            _buffer.AddRange(newData);

            while (_buffer.Count >= _frameLength)
            {
                var frame = _buffer.GetRange(0, _frameLength).ToArray();
                _buffer.RemoveRange(0, _frameLength);
                result.Add(frame);
            }
            return result;
        }

        public void Reset() => _buffer.Clear();
    }
}