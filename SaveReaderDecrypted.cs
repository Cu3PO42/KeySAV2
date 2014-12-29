using System;
using System.IO;
using KeySAV2.Structures;

namespace KeySAV2
{
    class SaveReaderDecrypted : ISaveReader
    {
        private const uint orasOffset = 0x33000;
        private const uint xyOffset = 0x22600;

        private readonly byte[] sav;
        private readonly uint offset;
        private const string _KeyName = "Decrypted. No Key needed";

        public string KeyName
        {
            get { return _KeyName; }
        }

        internal SaveReaderDecrypted(byte[] file, string type)
        {
            sav = file;
            offset = type == "XY" ? xyOffset : orasOffset;
        }

        public void scanSlots() {}
        public void scanSlots(ushort pos) {}
        public void scanSlots(ushort from, ushort to) {}

        public PKX? getPkx(ushort pos)
        {
            byte[] pkx = new byte[232];
            uint pkxOffset = (uint) (offset + pos*232);
            if (Utility.SequenceEqual(pkx, 0, sav, pkxOffset, 232))
                return null;
            Array.Copy(sav, pkxOffset, pkx, 0, 232);
            pkx = PKX.decrypt(pkx);
            if (PKX.verifyCHK(pkx) && !pkx.Empty())
            {
                return new PKX(pkx, (byte)(pos/30), (byte)(pos%30), false);

            }
            return null;
        }
    }
}
