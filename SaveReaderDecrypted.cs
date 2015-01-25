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

        public string KeyName
        {
            get { return "Decrypted. No Key needed"; }
        }

        public ushort UnlockedSlots
        {
            get { return 930;  }
        }

        internal SaveReaderDecrypted(byte[] file, string type)
        {
            sav = file;
            switch (type)
            {
                case "XY":
                    offset = xyOffset;
                    break;
                case "ORAS":
                    offset = orasOffset;
                    break;
                case "RAW":
                    offset = 4;
                    byte[] ekx = new byte[232];
                    Array.Copy(sav, 4, ekx, 0, 232);
                    if (!PKX.verifyCHK(PKX.decrypt(ekx)))
                        offset = 8;
                    break;
            }
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
