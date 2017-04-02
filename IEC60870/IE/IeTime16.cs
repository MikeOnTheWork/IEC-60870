using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeTime16 : InformationElement
    {
        private readonly byte[] _value = new byte[2];

        public IeTime16(long timestamp)
        {
            var datetime = new DateTime(timestamp);
            var ms = datetime.Millisecond + 1000*datetime.Second;

            _value[0] = (byte) ms;
            _value[1] = (byte) (ms >> 8);
        }

        public IeTime16(int timeInMs)
        {
            var ms = timeInMs%60000;
            _value[0] = (byte) ms;
            _value[1] = (byte) (ms >> 8);
        }

        public IeTime16(BinaryReader reader)
        {
            _value = reader.ReadBytes(2);
        }

        public override int Encode(byte[] buffer, int i)
        {
            Array.Copy(_value, 0, buffer, i, 2);
            return 2;
        }

        public int GetTimeInMs()
        {
            return (_value[0] & 0xff) + ((_value[1] & 0xff) << 8);
        }

        public override string ToString()
        {
            return "Time16, time in ms: " + GetTimeInMs();
        }
    }
}