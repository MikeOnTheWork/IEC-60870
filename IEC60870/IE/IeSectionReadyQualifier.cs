using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeSectionReadyQualifier : InformationElement
    {
        private readonly bool _sectionNotReady;
        private readonly int _value;

        public IeSectionReadyQualifier(int value, bool sectionNotReady)
        {
            _value = value;
            _sectionNotReady = sectionNotReady;
        }

        public IeSectionReadyQualifier(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _value = b1 & 0x7f;
            _sectionNotReady = (b1 & 0x80) == 0x80;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _value;
            if (_sectionNotReady)
            {
                buffer[i] |= 0x80;
            }
            return 1;
        }

        public int GetValue()
        {
            return _value;
        }

        public bool IsSectionNotReady()
        {
            return _sectionNotReady;
        }

        public override string ToString()
        {
            return "Section ready qualifier: " + _value + ", section not ready: " + _sectionNotReady;
        }
    }
}