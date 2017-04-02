using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeCauseOfInitialization : InformationElement
    {
        private readonly bool _initAfterParameterChange;
        private readonly int _value;

        public IeCauseOfInitialization(int value, bool initAfterParameterChange)
        {
            if (value < 0 || value > 127)
            {
                throw new ArgumentException("Value has to be in the range 0..127");
            }

            _value = value;
            _initAfterParameterChange = initAfterParameterChange;
        }

        public IeCauseOfInitialization(BinaryReader reader)
        {
            int b1 = reader.ReadByte();

            _initAfterParameterChange = (b1 & 0x80) == 0x80;

            _value = b1 & 0x7f;
        }

        public override int Encode(byte[] buffer, int i)
        {
            if (_initAfterParameterChange)
            {
                buffer[i] = (byte) (_value | 0x80);
            }
            else
            {
                buffer[i] = (byte) _value;
            }

            return 1;
        }

        public int GetValue()
        {
            return _value;
        }

        public bool IsInitAfterParameterChange()
        {
            return _initAfterParameterChange;
        }

        public override string ToString()
        {
            return "Cause of initialization: " + _value + ", init after parameter change: " + _initAfterParameterChange;
        }
    }
}