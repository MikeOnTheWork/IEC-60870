using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeQualifierOfParameterOfMeasuredValues : InformationElement
    {
        private readonly bool _change;
        private readonly int _kindOfParameter;
        private readonly bool _notInOperation;

        public IeQualifierOfParameterOfMeasuredValues(int kindOfParameter, bool change, bool notInOperation)
        {
            _kindOfParameter = kindOfParameter;
            _change = change;
            _notInOperation = notInOperation;
        }

        public IeQualifierOfParameterOfMeasuredValues(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _kindOfParameter = b1 & 0x3f;
            _change = (b1 & 0x40) == 0x40;
            _notInOperation = (b1 & 0x80) == 0x80;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _kindOfParameter;
            if (_change)
            {
                buffer[i] |= 0x40;
            }
            if (_notInOperation)
            {
                buffer[i] |= 0x80;
            }
            return 1;
        }

        public int GetKindOfParameter()
        {
            return _kindOfParameter;
        }

        public bool IsChange()
        {
            return _change;
        }

        public bool IsNotInOperation()
        {
            return _notInOperation;
        }

        public override string ToString()
        {
            return "Qualifier of parameter of measured values, kind of parameter: " + _kindOfParameter + ", change: "
                   + _change + ", not in operation: " + _notInOperation;
        }
    }
}