using System;
using System.IO;
using KeySAV2.Structures;

namespace KeySAV2
{
    class SaveReaderDecrypted : ISaveReader
    {
        private const uint orasOffset = 0x38400;
        private const uint xyOffset = 0x27A00;

        private readonly byte[] sav;
        private readonly uint offset;

        public SaveReaderDecrypted(byte[] file, string type)
        {
            sav = file;
            offset = string.Equals(type, "XY", StringComparison.Ordinal) ? xyOffset : orasOffset;
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
            if (PKX.verifyCHK(pkx))
            {
                PKX tmp = new PKX(pkx, (byte)(pos/30), (byte)(pos%30), false);

            }
            return null;
        }
    }
}
