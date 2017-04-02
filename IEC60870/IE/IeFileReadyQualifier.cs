using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeFileReadyQualifier : InformationElement
    {
        private readonly bool _negativeConfirm;
        private readonly int _value;

        public IeFileReadyQualifier(int value, bool negativeConfirm)
        {
            _value = value;
            _negativeConfirm = negativeConfirm;
        }

        public IeFileReadyQualifier(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _value = b1 & 0x7f;
            _negativeConfirm = (b1 & 0x80) == 0x80;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _value;
            if (_negativeConfirm)
            {
                buffer[i] |= 0x80;
            }
            return 1;
        }

        public int GetValue()
        {
            return _value;
        }

        public bool IsNegativeConfirm()
        {
            return _negativeConfirm;
        }

        public override string ToString()
        {
            return "File ready qualifier: " + _value + ", negative confirm: " + _negativeConfirm;
        }
    }
}