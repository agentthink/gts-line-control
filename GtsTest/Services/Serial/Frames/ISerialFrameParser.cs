using System.Collections.Generic;

namespace GtsTest.Services.Serial.Frames
{
    /// <summary>
    /// 帧解析器：负责从字节缓冲区中切分完整帧
    /// </summary>
    public interface ISerialFrameParser
    {
        /// <summary>
        /// 把新到达的字节加入缓冲区，返回所有已完成的帧
        /// </summary>
        List<byte[]> Push(byte[] newData);

        /// <summary>清空缓冲区</summary>
        void Reset();
    }
}