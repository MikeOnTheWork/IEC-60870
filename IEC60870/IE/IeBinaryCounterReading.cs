using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeBinaryCounterReading : InformationElement
    {
        private readonly bool _carry;
        private readonly bool _counterAdjusted;
        private readonly int _counterReading;
        private readonly bool _invalid;
        private readonly int _sequenceNumber;

        public IeBinaryCounterReading(int counterReading, int sequenceNumber, bool carry, bool counterAdjusted,
            bool invalid)
        {
            _counterReading = counterReading;
            _sequenceNumber = sequenceNumber;
            _carry = carry;
            _counterAdjusted = counterAdjusted;
            _invalid = invalid;
        }

        public IeBinaryCounterReading(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            int b2 = reader.ReadByte();
            int b3 = reader.ReadByte();
            int b4 = reader.ReadByte();
            int b5 = reader.ReadByte();

            _carry = (b5 & 0x20) == 0x20;
            _counterAdjusted = (b5 & 0x40) == 0x40;
            _invalid = (b5 & 0x80) == 0x80;

            _sequenceNumber = b5 & 0x1f;

            _counterReading = (b4 << 24) | (b3 << 16) | (b2 << 8) | b1;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i++] = (byte) _counterReading;
            buffer[i++] = (byte) (_counterReading >> 8);
            buffer[i++] = (byte) (_counterReading >> 16);
            buffer[i++] = (byte) (_counterReading >> 24);

            buffer[i] = (byte) _sequenceNumber;
            if (_carry)
            {
                buffer[i] |= 0x20;
            }
            if (_counterAdjusted)
            {
                buffer[i] |= 0x40;
            }
            if (_invalid)
            {
                buffer[i] |= 0x80;
            }

            return 5;
        }

        public int GetCounterReading()
        {
            return _counterReading;
        }

        public int GetSequenceNumber()
        {
            return _sequenceNumber;
        }

        public bool IsCarry()
        {
            return _carry;
        }

        public bool IsCounterAdjusted()
        {
            return _counterAdjusted;
        }

        public bool IsInvalid()
        {
            return _invalid;
        }

        public override string ToString()
        {
            return "Binary counter reading: " + _counterReading + ", seq num: " + _sequenceNumber + ", carry: " + _carry
                   + ", counter adjusted: " + _counterAdjusted + ", invalid: " + _invalid;
        }
    }
}