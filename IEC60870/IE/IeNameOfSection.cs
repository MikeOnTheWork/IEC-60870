using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeNameOfSection : InformationElement
    {
        private readonly int _value;

        public IeNameOfSection(int value)
        {
            _value = value;
        }

        public IeNameOfSection(BinaryReader reader)
        {
            _value = reader.ReadByte();
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _value;

            return 1;
        }

        public int GetValue()
        {
            return _value;
        }

        public override string ToString()
        {
            return "Name of section: " + _value;
        }
    }
}