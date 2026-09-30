using System;

namespace GtsTest.Diagnostics
{
    /// <summary>
    /// 线程安全的环形缓冲：固定容量，满了覆盖最旧数据
    /// </summary>
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly object _lock = new object();
        private int _head = 0;      // 下一个写入位置
        private int _count = 0;     // 当前元素数

        public int Capacity => _buffer.Length;
        public int Count { get { lock (_lock) return _count; } }

        public RingBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = new T[capacity];
        }

        /// <summary>添加一个元素（若满则覆盖最旧）</summary>
        public void Add(T item)
        {
            lock (_lock)
            {
                _buffer[_head] = item;
                _head = (_head + 1) % _buffer.Length;
                if (_count < _buffer.Length) _count++;
            }
        }

        /// <summary>清空</summary>
        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _head = 0;
                _count = 0;
            }
        }

        /// <summary>
        /// 快照：按加入顺序返回当前所有元素
        /// maxCount > 0 时只返回最近 maxCount 条
        /// </summary>
        public T[] Snapshot(int maxCount = 0)
        {
            lock (_lock)
            {
                int take = maxCount > 0 && maxCount < _count ? maxCount : _count;
                var result = new T[take];

                // 起始位置：head - count（环形回绕）
                int start = (_head - _count + _buffer.Length) % _buffer.Length;
                // 需要返回的是"最后 take 条"
                int skip = _count - take;
                start = (start + skip) % _buffer.Length;

                for (int i = 0; i < take; i++)
                    result[i] = _buffer[(start + i) % _buffer.Length];

                return result;
            }
        }
    }
}