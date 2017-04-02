using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeQualifierOfCounterInterrogation : InformationElement
    {
        private readonly int _freeze;
        private readonly int _request;

        public IeQualifierOfCounterInterrogation(int request, int freeze)
        {
            _request = request;
            _freeze = freeze;
        }

        public IeQualifierOfCounterInterrogation(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _request = b1 & 0x3f;
            _freeze = (b1 >> 6) & 0x03;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) (_request | (_freeze << 6));
            return 1;
        }

        public int GetRequest()
        {
            return _request;
        }

        public int GetFreeze()
        {
            return _freeze;
        }

        public override string ToString()
        {
            return "Qualifier of counter interrogation, request: " + _request + ", freeze: " + _freeze;
        }
    }
}