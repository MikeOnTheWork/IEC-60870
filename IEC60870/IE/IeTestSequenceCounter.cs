using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeTestSequenceCounter : InformationElement
    {
        private readonly int _value;

        public IeTestSequenceCounter(int value)
        {
            if (value < 0 || value > 65535)
            {
                throw new ArgumentException("Value has to be in the range 0..65535");
            }
            _value = value;
        }

        public IeTestSequenceCounter(BinaryReader reader)
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
            return "Test sequence counter: " + _value;
        }
    }
}