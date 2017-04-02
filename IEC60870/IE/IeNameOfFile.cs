using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeNameOfFile : InformationElement
    {
        private readonly int _value;

        public IeNameOfFile(int value)
        {
            _value = value;
        }

        public IeNameOfFile(BinaryReader reader)
        {
            _value = reader.ReadByte() | (reader.ReadByte() << 8);
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i++] = (byte) _value;
            buffer[i] = (byte) (_value >> 8);

            return 2;
        }

        public int GetValue()
        {
            return _value;
        }

        public override string ToString()
        {
            return "Name of file: " + _value;
        }
    }
}