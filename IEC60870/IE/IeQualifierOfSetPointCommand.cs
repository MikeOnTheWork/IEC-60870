using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeQualifierOfSetPointCommand : InformationElement
    {
        private readonly int _ql;
        private readonly bool _select;

        public IeQualifierOfSetPointCommand(int ql, bool select)
        {
            _ql = ql;
            _select = select;
        }

        public IeQualifierOfSetPointCommand(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _ql = b1 & 0x7f;
            _select = (b1 & 0x80) == 0x80;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _ql;
            if (_select)
            {
                buffer[i] |= 0x80;
            }
            return 1;
        }

        public int GetQl()
        {
            return _ql;
        }

        public bool IsSelect()
        {
            return _select;
        }

        public override string ToString()
        {
            return "Qualifier of set point command, QL: " + _ql + ", select: " + _select;
        }
    }
}