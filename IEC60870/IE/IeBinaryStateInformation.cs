using System;
using System.IO;
using System.Text;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeBinaryStateInformation : InformationElement
    {
        private readonly int _value;

        public IeBinaryStateInformation(int value)
        {
            _value = value;
        }

        public IeBinaryStateInformation(BinaryReader reader)
        {
            _value = reader.ReadInt32();
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i++] = (byte) (_value >> 24);
            buffer[i++] = (byte) (_value >> 16);
            buffer[i++] = (byte) (_value >> 8);
            buffer[i] = (byte) _value;
            return 4;
        }

        public int GetValue()
        {
            return _value;
        }

        public bool GetBinaryState(int position)
        {
            if (position < 1 || position > 32)
            {
                throw new ArgumentException("Position out of bound. Should be between 1 and 32.");
            }

            return ((_value >> (position - 1)) & 0x01) == 0x01;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(_value.ToString("X"));
            while (sb.Length < 8)
            {
                sb.Insert(0, '0'); // pad with leading zero if needed
            }

            return "Binary state information (first bit = LSB): " + sb;
        }
    }
}