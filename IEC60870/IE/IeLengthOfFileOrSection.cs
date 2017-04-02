using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeLengthOfFileOrSection : InformationElement
    {
        private readonly int _value;

        public IeLengthOfFileOrSection(int value)
        {
            _value = value;
        }

        public IeLengthOfFileOrSection(BinaryReader reader)
        {
            _value = reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16);
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i++] = (byte) _value;
            buffer[i++] = (byte) (_value >> 8);
            buffer[i] = (byte) (_value >> 16);

            return 3;
        }

        public int GetValue()
        {
            return _value;
        }

        public override string ToString()
        {
            return "Length of file or section: " + _value;
        }
    }
}