using System;
using System.IO;
using KeySAV2.Structures;

namespace KeySAV2
{
    public class BattleVideoReader
    {
        private const ushort offset = 0x4E18;
        private const ushort keyoff = 0x100;

        private static string path;

        private byte[] video;
        private byte[] key;
        public readonly string KeyName;

        static BattleVideoReader()
        {
            path = Path.Combine(System.Windows.Forms.Application.StartupPath, "data");
        }

        internal BattleVideoReader(byte[] file)
        {
            video = file;
            ulong stamp = BitConverter.ToUInt64(video, 0x10);

            string[] files = Directory.GetFiles("data", "*.bin", SearchOption.AllDirectories);
            byte[] data = new Byte[0x1000];
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fi = new FileInfo(files[i]);
                {
                    if (fi.Length == 0x1000)
                    {
                        data = File.ReadAllBytes(files[i]);
                        ulong newstamp = BitConverter.ToUInt64(data, 0x0);
                        if (newstamp == stamp)
                        {
                            key = data;
                            KeyName = fi.Name;
                        }
                    }
                }
            }
            if (key == null)
                throw new Exceptions.NoKeyException();
        }
        
        public PKX getPkx(byte slot, byte opponent)
        {
            byte[] ekx;
            byte[] pkx;
            ekx = Utility.xor(video, (uint)(offset + 260 * slot + opponent * 620), key, (uint)(keyoff + 260 * slot + opponent * 0x700), 260);
            pkx = PKX.decrypt(ekx);
            return new PKX(PKX.verifyCHK(pkx) ? pkx : ekx, -1, slot, false);
        }

        public bool DumpsEnemy
        {
            get { return BitConverter.ToUInt64(key, 0x800) != 0; }
        }
    }
}
