using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    internal class IeProtectionOutputCircuitInformation : InformationElement
    {
        private readonly int _value;

        public IeProtectionOutputCircuitInformation(bool generalCommand, bool commandToL1, bool commandToL2,
            bool commandToL3)
        {
            _value = 0;

            if (generalCommand)
            {
                _value |= 0x01;
            }
            if (commandToL1)
            {
                _value |= 0x02;
            }
            if (commandToL2)
            {
                _value |= 0x04;
            }
            if (commandToL3)
            {
                _value |= 0x08;
            }
        }

        public IeProtectionOutputCircuitInformation(BinaryReader reader)
        {
            _value = reader.ReadByte();
        }


        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _value;
            return 1;
        }

        public bool IsGeneralCommand()
        {
            return (_value & 0x01) == 0x01;
        }

        public bool IsCommandToL1()
        {
            return (_value & 0x02) == 0x02;
        }

        public bool IsCommandToL2()
        {
            return (_value & 0x04) == 0x04;
        }

        public bool IsCommandToL3()
        {
            return (_value & 0x08) == 0x08;
        }

        public override string ToString()
        {
            return "Protection output circuit information, general command: " + IsGeneralCommand() + ", command to L1: "
                   + IsCommandToL1() + ", command to L2: " + IsCommandToL2() + ", command to L3: " + IsCommandToL3();
        }
    }
}