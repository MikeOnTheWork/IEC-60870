﻿using System;
using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeShortFloat : InformationElement
    {
        private readonly float _value;

        public IeShortFloat(float value)
        {
            _value = value;
        }

        public IeShortFloat(BinaryReader reader)
        {
            var data = reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 24);
            var bytes = BitConverter.GetBytes(data);
            _value = BitConverter.ToSingle(bytes, 0);
        }

        public override int Encode(byte[] buffer, int i)
        {
            var tempVal = BitConverter.ToInt32(BitConverter.GetBytes(_value), 0);
            buffer[i++] = (byte) tempVal;
            buffer[i++] = (byte) (tempVal >> 8);
            buffer[i++] = (byte) (tempVal >> 16);
            buffer[i] = (byte) (tempVal >> 24);

            return 4;
        }

        public float GetValue()
        {
            return _value;
        }

        public override string ToString()
        {
            return "Short float value: " + _value;
        }
    }
}