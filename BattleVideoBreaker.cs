using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KeySAV2.Structures;

namespace KeySAV2
{
    static class BattleVideoBreaker
    {
        private static byte[] LoadRaw(string file)
        {
            FileInfo info = new FileInfo(file);
            if (info.Length != 28256)
                throw new Exceptions.NoBattleVideoException();
            return File.ReadAllBytes(file);
        }

        public static BattleVideoReader Load(string file)
        {
            return new BattleVideoReader(LoadRaw(file));
        }

        public static byte[] Break(string file1, string file2, out string result)
        {
            byte[] video1, video2;
            byte[] ezeros = PKX.encrypt(new Byte[260]);
            byte[] xorstream = new Byte[260 * 6];
            byte[] breakstream = new Byte[260 * 6];
            byte[] bvkey = new Byte[0x1000];
            result = "";

            video1 = LoadRaw(file1);
            video2 = LoadRaw(file2);

            #region Old Exploit to ensure that the usage is correct
            // Validity Check to see what all is participating...

            Array.Copy(video1, 0x4E18, breakstream, 0, 260 * 6);
            // XOR them together at party offset
            for (int i = 0; i < (260 * 6); i++)
                xorstream[i] = (byte)(breakstream[i] ^ video2[i + 0x4E18]);

            // Retrieve EKX_1's data
            byte[] ekx1 = new Byte[260];
            for (int i = 0; i < (260); i++)
                ekx1[i] = (byte)(xorstream[i + 260] ^ ezeros[i]);
            for (int i = 0; i < 260; i++)
                xorstream[i] ^= ekx1[i];

            #endregion
            // If old exploit does not properly decrypt slot1...
            byte[] pkx = PKX.decrypt(ekx1);
            if (!PKX.verifyCHK(pkx))
            {
                result = "Improperly set up Battle Videos. Please follow directions and try again";
                return null;
            }

            // Start filling up our key...
            #region Key Filling (bvkey)
            // Copy in the unique CTR encryption data to ID the video...
            Array.Copy(video1, 0x10, bvkey, 0, 0x10);

            // Copy unlocking data
            byte[] key1 = new Byte[260]; Array.Copy(video1, 0x4E18, key1, 0, 260);
            Utility.xor(ekx1, 0, key1, 0, bvkey, 0x100, 260);
            Array.Copy(video1, 0x4E18 + 260, bvkey, 0x100 + 260, 260*5); // XORstream from save1 has just keystream.
            
            // See if Opponent first slot can be decrypted...

            Array.Copy(video1, 0x5438, breakstream, 0, 260 * 6);
            // XOR them together at party offset
            for (int i = 0; i < (260 * 6); i++)
                xorstream[i] = (byte)(breakstream[i] ^ video2[i + 0x5438]);
            // XOR through the empty data for the encrypted zero data.
            for (int i = 0; i < (260 * 5); i++)
                bvkey[0x100 + 260 + i] ^= ezeros[i % 260];

            // Retrieve EKX_2's data
            byte[] ekx2 = new Byte[260];
            for (int i = 0; i < (260); i++)
                ekx2[i] = (byte)(xorstream[i + 260] ^ ezeros[i]);
            for (int i = 0; i < 260; i++)
                xorstream[i] ^= ekx2[i];
            byte[] key2 = new Byte[260]; Array.Copy(video1,0x5438,key2,0,260);
            byte[] pkx2 = PKX.decrypt(ekx2);
            if (PKX.verifyCHK(PKX.decrypt(ekx2)) && (BitConverter.ToUInt16(pkx2,0x8) != 0))
            {
                Utility.xor(ekx2, 0, key2, 0, bvkey, 0x800, 260);
                Array.Copy(video1, 0x5438 + 260, bvkey, 0x800 + 260, 260 * 5); // XORstream from save1 has just keystream.

                for (int i = 0; i < (260 * 5); i++)
                    bvkey[0x800 + 260 + i] ^= ezeros[i % 260];

                result = "Can dump from Opponent Data on this key too!" + System.Environment.NewLine;
            }
            #endregion

            string ot = Encoding.Unicode.GetString(pkx, 0xB0, 24).TrimCString();
            ushort tid = BitConverter.ToUInt16(pkx, 0xC);
            ushort sid = BitConverter.ToUInt16(pkx, 0xE);
            ushort tsv = (ushort)((tid ^ sid) >> 4);
            // Finished, allow dumping of breakstream
            result += String.Format("Success!\nYour first Pokemon's TSV: {0}\nOT: {1}\n\nPlease save your keystream.", tsv.ToString("0000"),ot);

            return bvkey;
        }
    }
}
