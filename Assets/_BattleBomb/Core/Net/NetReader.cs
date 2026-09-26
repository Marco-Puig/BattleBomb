using System;
using System.Text;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Reads what <see cref="NetWriter"/> wrote. Every read checks the bytes are there and every
    /// count is checked against a bound the caller names, so a malformed or hostile message is a
    /// <see cref="NetFormatException"/> at the first bad byte.
    /// </summary>
    public sealed class NetReader
    {
        private readonly byte[] _data;
        private readonly int _end;

        public NetReader(byte[] data) : this(data, data != null ? data.Length : 0)
        {
        }

        public NetReader(byte[] data, int length)
        {
            _data = data ?? Array.Empty<byte>();
            _end = Math.Max(0, Math.Min(length, _data.Length));
        }

        public int Position { get; private set; }

        public int Remaining => _end - Position;

        public byte ReadByte()
        {
            Need(1);
            return _data[Position++];
        }

        public bool ReadBool()
        {
            byte value = ReadByte();
            if (value > 1)
            {
                throw new NetFormatException($"A bool byte of {value}.");
            }

            return value == 1;
        }

        public short ReadShort() => unchecked((short)ReadUShort());

        public ushort ReadUShort()
        {
            Need(2);
            int at = Position;
            Position += 2;
            return (ushort)(_data[at] | (_data[at + 1] << 8));
        }

        public int ReadInt() => unchecked((int)ReadUInt());

        public uint ReadUInt()
        {
            Need(4);
            int at = Position;
            Position += 4;
            return unchecked((uint)(_data[at] | (_data[at + 1] << 8) | (_data[at + 2] << 16) | (_data[at + 3] << 24)));
        }

        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        public Vector2 ReadVector2() => new Vector2(ReadFloat(), ReadFloat());

        public Vector3 ReadVector3() => new Vector3(ReadFloat(), ReadFloat(), ReadFloat());

        public string ReadString(int maxBytes = 4096)
        {
            int length = ReadUShort();
            if (length > maxBytes)
            {
                throw new NetFormatException($"A {length}-byte string where at most {maxBytes} is allowed.");
            }

            Need(length);
            string value = Encoding.UTF8.GetString(_data, Position, length);
            Position += length;
            return value;
        }

        public int ReadCount(int max)
        {
            int count = ReadUShort();
            if (count > max)
            {
                throw new NetFormatException($"A count of {count} where at most {max} is allowed.");
            }

            return count;
        }

        /// <summary>Raw bytes behind a 32-bit length, refused before a byte is copied if longer than
        /// <paramref name="maxBytes"/>.</summary>
        public byte[] ReadBlob(int maxBytes)
        {
            int length = ReadInt();
            if (length < 0 || length > maxBytes)
            {
                throw new NetFormatException($"A {length}-byte blob where at most {maxBytes} is allowed.");
            }

            Need(length);
            var value = new byte[length];
            Array.Copy(_data, Position, value, 0, length);
            Position += length;
            return value;
        }

        private void Need(int bytes)
        {
            if (bytes < 0 || Position + bytes > _end)
            {
                throw new NetFormatException(
                    $"Wanted {bytes} byte(s) at {Position} of a {_end}-byte message.");
            }
        }
    }
}
