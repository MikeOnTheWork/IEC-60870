using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeFileSegment : InformationElement
    {
        private readonly int _length;
        private readonly int _offset;
        private readonly byte[] _segment;

        public IeFileSegment(byte[] segment, int offset, int length)
        {
            _segment = segment;
            _offset = offset;
            _length = length;
        }

        public IeFileSegment(BinaryReader reader)
        {
            _length = reader.ReadByte();
            _segment = reader.ReadBytes(_length);
            _offset = 0;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i++] = (byte) _length;

            Array.Copy(_segment, _offset, buffer, i, _length);

            return _length + 1;
        }

        public byte[] GetSegment()
        {
            return _segment;
        }

        public override string ToString()
        {
            return "File segment of length: " + _length;
        }
    }
}