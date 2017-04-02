using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeQualifierOfParameterActivation : InformationElement
    {
        private readonly int _value;

        public IeQualifierOfParameterActivation(int value)
        {
            _value = value;
        }

        public IeQualifierOfParameterActivation(BinaryReader reader)
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
            return "Qualifier of parameter activation: " + _value;
        }
    }
}