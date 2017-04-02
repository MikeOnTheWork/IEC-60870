using System;
using System.IO;
using System.Text;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeTime56 : InformationElement
    {
        private readonly byte[] _value = new byte[7];

        public IeTime56(long timestamp, TimeZone timeZone, bool invalid)
        {
            var datetime = new DateTime(timestamp);
            var ms = datetime.Millisecond + 1000 * datetime.Second;

            _value[0] = (byte)ms;
            _value[1] = (byte)(ms >> 8);
            _value[2] = (byte)datetime.Minute;

            if (invalid)
            {
                _value[2] |= 0x80;
            }
            _value[3] = (byte)datetime.Hour;
            if (datetime.IsDaylightSavingTime())
            {
                _value[3] |= 0x80;
            }
            _value[4] = (byte)(datetime.Day + ((((int)datetime.DayOfWeek + 5) % 7 + 1) << 5));
            _value[5] = (byte)(datetime.Month + 1);
            _value[6] = (byte)(datetime.Year % 100);
        }

        public IeTime56(long timestamp) : this(timestamp, TimeZone.CurrentTimeZone, false)
        {
        }

        public IeTime56(byte[] value)
        {
            for (var i = 0; i < 7; i++)
            {
                _value[i] = value[i];
            }
        }

        public IeTime56(BinaryReader reader)
        {
            _value = reader.ReadBytes(7);
        }

        public override int Encode(byte[] buffer, int i)
        {
            Array.Copy(_value, 0, buffer, i, 7);
            return 7;
        }

        public long GetTimestamp(int startOfCentury, TimeZone timeZone)
        {
            //TODO implement GetTimestamp

            return -1;
        }

        public long GetTimestamp(int startOfCentury)
        {
            return GetTimestamp(startOfCentury, TimeZone.CurrentTimeZone);
        }

        public long GetTimestamp()
        {
            return GetTimestamp(1970, TimeZone.CurrentTimeZone);
        }

        public int GetMillisecond()
        {
            return ((_value[0] & 0xff) + ((_value[1] & 0xff) << 8)) % 1000;
        }

        public int GetSecond()
        {
            return ((_value[0] & 0xff) + ((_value[1] & 0xff) << 8)) / 1000;
        }

        public int GetMinute()
        {
            return _value[2] & 0x3f;
        }

        public int GetHour()
        {
            return _value[3] & 0x1f;
        }

        public int GetDayOfWeek()
        {
            return (_value[4] & 0xe0) >> 5;
        }

        public int GetDayOfMonth()
        {
            return _value[4] & 0x1f;
        }

        public int GetMonth()
        {
            return _value[5] & 0x0f;
        }

        public int GetYear()
        {
            return _value[6] & 0x7f;
        }

        public bool IsSummerTime()
        {
            return (_value[3] & 0x80) == 0x80;
        }

        public bool IsInvalid()
        {
            return (_value[2] & 0x80) == 0x80;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("Time56: ");
            AppendWithNumDigits(builder, GetDayOfMonth(), 2);
            builder.Append("-");
            AppendWithNumDigits(builder, GetMonth(), 2);
            builder.Append("-");
            AppendWithNumDigits(builder, GetYear(), 2);
            builder.Append(" ");
            AppendWithNumDigits(builder, GetHour(), 2);
            builder.Append(":");
            AppendWithNumDigits(builder, GetMinute(), 2);
            builder.Append(":");
            AppendWithNumDigits(builder, GetSecond(), 2);
            builder.Append(":");
            AppendWithNumDigits(builder, GetMillisecond(), 3);

            if (IsSummerTime())
            {
                builder.Append(" DST");
            }

            if (IsInvalid())
            {
                builder.Append(", invalid");
            }

            return builder.ToString();
        }

        private void AppendWithNumDigits(StringBuilder builder, int value, int numDigits)
        {
            var i = numDigits - 1;
            while (i < numDigits && value < Math.Pow(10, i))
            {
                builder.Append("0");
                i--;
            }
            builder.Append(value);
        }
    }
}