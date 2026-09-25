using System;
using System.Text;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Little-endian bytes for one message. Reused across messages with <see cref="Reset"/>, so a
    /// 30 Hz snapshot does not allocate a buffer every time it is written.
    /// </summary>
    public sealed class NetWriter
    {
        private byte[] _buffer;

        public NetWriter(int capacity = 256)
        {
            _buffer = new byte[Math.Max(16, capacity)];
        }

        public int Length { get; private set; }

        /// <summary>The bytes written so far live in [0, <see cref="Length"/>).</summary>
        public byte[] Buffer => _buffer;

        public void Reset() => Length = 0;

        public byte[] ToArray()
        {
            var copy = new byte[Length];
            Array.Copy(_buffer, copy, Length);
            return copy;
        }

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buffer[Length++] = value;
        }

        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteShort(short value) => WriteUShort(unchecked((ushort)value));

        public void WriteUShort(ushort value)
        {
            Ensure(2);
            _buffer[Length++] = (byte)value;
            _buffer[Length++] = (byte)(value >> 8);
        }

        public void WriteInt(int value) => WriteUInt(unchecked((uint)value));

        public void WriteUInt(uint value)
        {
            Ensure(4);
            _buffer[Length++] = (byte)value;
            _buffer[Length++] = (byte)(value >> 8);
            _buffer[Length++] = (byte)(value >> 16);
            _buffer[Length++] = (byte)(value >> 24);
        }

        public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

        public void WriteVector2(Vector2 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
        }

        public void WriteVector3(Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }

        /// <summary>UTF-8 behind a 16-bit length. Null writes as empty.</summary>
        public void WriteString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new NetFormatException($"A {bytes.Length}-byte string does not fit a message.");
            }

            WriteUShort((ushort)bytes.Length);
            Ensure(bytes.Length);
            Array.Copy(bytes, 0, _buffer, Length, bytes.Length);
            Length += bytes.Length;
        }

        /// <summary>A list length, refused here if the reader would refuse it there.</summary>
        public void WriteCount(int count, int max)
        {
            if (count < 0 || count > max || max > ushort.MaxValue)
            {
                throw new NetFormatException($"Count {count} is outside 0..{max}.");
            }

            WriteUShort((ushort)count);
        }

        private void Ensure(int extra)
        {
            int needed = Length + extra;
            if (needed <= _buffer.Length)
            {
                return;
            }

            int size = _buffer.Length;
            while (size < needed)
            {
                size *= 2;
            }

            Array.Resize(ref _buffer, size);
        }
    }
}
