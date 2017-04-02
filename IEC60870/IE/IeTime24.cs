using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeTime24 : InformationElement
    {
        private readonly byte[] _value = new byte[3];

        public IeTime24(long timestamp)
        {
            var datetime = new DateTime(timestamp);
            var ms = datetime.Millisecond + 1000*datetime.Second;

            _value[0] = (byte) ms;
            _value[1] = (byte) (ms >> 8);
            _value[2] = (byte) datetime.Minute;
        }

        public IeTime24(int timeInMs)
        {
            var ms = timeInMs%60000;
            _value[0] = (byte) ms;
            _value[1] = (byte) (ms >> 8);
            _value[2] = (byte) (ms >> 8);
        }

        public IeTime24(BinaryReader reader)
        {
            _value = reader.ReadBytes(3);
        }

        public override int Encode(byte[] buffer, int i)
        {
            Array.Copy(_value, 0, buffer, i, 3);
            return 3;
        }

        public int GetTimeInMs()
        {
            return (_value[0] & 0xff) + ((_value[1] & 0xff) << 8) + _value[2]*6000;
        }

        public override string ToString()
        {
            return "Time24, time in ms: " + GetTimeInMs();
        }
    }
}