using System.IO;
using IEC60870.IE.Base;

namespace IEC60870.IE
{
    public class IeStatusOfFile : InformationElement
    {
        private readonly bool _lastFileOfDirectory;
        private readonly bool _nameDefinesDirectory;
        private readonly int _status;
        private readonly bool _transferIsActive;

        public IeStatusOfFile(int status, bool lastFileOfDirectory, bool nameDefinesDirectory,
            bool transferIsActive)
        {
            _status = status;
            _lastFileOfDirectory = lastFileOfDirectory;
            _nameDefinesDirectory = nameDefinesDirectory;
            _transferIsActive = transferIsActive;
        }

        public IeStatusOfFile(BinaryReader reader)
        {
            int b1 = reader.ReadByte();
            _status = b1 & 0x1f;
            _lastFileOfDirectory = (b1 & 0x20) == 0x20;
            _nameDefinesDirectory = (b1 & 0x40) == 0x40;
            _transferIsActive = (b1 & 0x80) == 0x80;
        }

        public override int Encode(byte[] buffer, int i)
        {
            buffer[i] = (byte) _status;
            if (_lastFileOfDirectory)
            {
                buffer[i] |= 0x20;
            }
            if (_nameDefinesDirectory)
            {
                buffer[i] |= 0x40;
            }
            if (_transferIsActive)
            {
                buffer[i] |= 0x80;
            }
            return 1;
        }

        public int GetStatus()
        {
            return _status;
        }

        public bool IsLastFileOfDirectory()
        {
            return _lastFileOfDirectory;
        }

        public bool IsNameDefinesDirectory()
        {
            return _nameDefinesDirectory;
        }

        public bool IsTransferIsActive()
        {
            return _transferIsActive;
        }

        public override string ToString()
        {
            return "Status of file: " + _status + ", last file of directory: " + _lastFileOfDirectory
                   + ", name defines directory: " + _nameDefinesDirectory + ", transfer is active: " + _transferIsActive;
        }
    }
}