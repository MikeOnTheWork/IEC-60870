using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeValueWithTransientState : InformationElement
    {
        private readonly bool _transientState;
        private readonly long _value;

        public IeValueWithTransientState(int value, bool transientState)
        {
            if (value < -64 || value > 63)
            {
                throw new ArgumentException("Value has to be in the range -64..63");
            }

            _value = value;
            _transientState = transientState;
        }

        public IeValueWithTransientState(BinaryReader reader)
        {
            int b1 = reader.ReadByte();

            _transientState = (b1 & 0x80) == 0x80;

            if ((b1 & 0x40) == 0x40)
            {
                _value = b1 | 0xffffff80;
            }
            else
            {
                _value = b1 & 0x3f;
            }
        }

        public override int Encode(byte[] buffer, int i)
        {
            if (_transientState)
            {
                buffer[i] = (byte) (_value | 0x80);
            }
            else
            {
                buffer[i] = (byte) (_value & 0x7f);
            }

            return 1;
        }

        public long GetValue()
        {
            return _value;
        }

        public bool IsTransientState()
        {
            return _transientState;
        }

        public override string ToString()
        {
            return "Value with transient state, value: " + GetValue() + ", transient state: " + IsTransientState();
        }
    }
}