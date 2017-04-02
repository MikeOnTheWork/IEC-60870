using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeSelectAndCallQualifier : InformationElement
    {
        private readonly int _action;
        private readonly int _notice;

        public IeSelectAndCallQualifier(int action, int notice)
        {
            _action = action;
            _notice = notice;
        }

        public IeSelectAndCallQualifier(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _action = b1 & 0x0f;
            _notice = (b1 >> 4) & 0x0f;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) (_action | (_notice << 4));
            return 1;
        }

        public int GetRequest()
        {
            return _action;
        }

        public int GetFreeze()
        {
            return _notice;
        }

        public override string ToString()
        {
            return "Select and call qualifier, action: " + _action + ", notice: " + _notice;
        }
    }
}