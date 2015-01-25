using System;
using System.IO;
using System.Linq;
using System.Text;
using KeySAV2.Exceptions;
using KeySAV2.Structures;

namespace KeySAV2
{
    public static class SaveBreaker
    {
        private const uint Magic = 0x42454546;
        public static readonly string[] eggnames;

        static SaveBreaker()
        {
            eggnames = new string[] {"タマゴ", "Egg", "Œuf", "Uovo", "Ei", "", "Huevo", "알"};
        }

        public static ISaveReader Load(string file)
        {
            return LoadBase<ISaveReader>(file, (input => new SaveReaderEncrypted(input)),
                (input =>
                {
                    if (input.Length == 0x76000 && BitConverter.ToUInt32(input, 0x75E10) == Magic)
                        return new SaveReaderDecrypted(input, "ORAS");
                    if (input.Length == 0x65600 && BitConverter.ToUInt32(input, 0x65410) == Magic)
                        return new SaveReaderDecrypted(input, "XY");
                    if (input.Length == 232 * 30 * 32)
                        return new SaveReaderDecrypted(input, "RAW");
                    throw new NoSaveException();
                }));
        }

        private static byte[] LoadRaw(string file)
        {
            return LoadBase(file, (x => x), (x => { throw new NoSaveException(); }));
        }

        private static T LoadBase<T>(string file, Func<byte[], T> fn1, Func<byte[], T> fn2)
        {
            FileInfo info = new FileInfo(file);
            if (info.Length != 0x100000 && info.Length != 0x10009C && info.Length != 0x65600 && info.Length != 0x76000 && info.Length != 232*30*32)
                throw new NoSaveException();
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                byte[] input;
                switch (info.Length)
                {
                    case 0x10009C:
                        fs.Seek(0x9C, SeekOrigin.Begin);
                        goto case 0x100000;
                    case 0x10019A:
                        fs.Seek(0x19A, SeekOrigin.Begin);
                        goto case 0x100000;
                    case 0x100000:
                        input = new byte[0x100000];
                        fs.Read(input, 0, 0x100000);
                        return fn1(input);
                    default:
                        input = new byte[info.Length];
                        fs.Read(input, 0, (int)info.Length);
                        return fn2(input);
                }
            }
        }

        public static SaveKey? Break(string file1, string file2, out string result, out byte[] respkx)
        {
            int[] offset = new int[2];
            byte[] empty = new Byte[232];
            byte[] emptyekx = new Byte[232];
            byte[] pkx = new Byte[232];
            byte[] break1, break2;
            byte[] savkey;
            byte[] save1Save;
            savkey = new byte[0xB4AD4];

            break1 = LoadRaw(file1);
            break2 = LoadRaw(file2);

            result = "";

            if (!Utility.SequenceEqual(break1, 0x10, break2, 0x10, 8))
            {
                result = "Saves are not from the same game!\nPlease follow the instructions.";
                respkx = new byte[232];
                return null;
            }

            foreach (string file in Directory.GetFiles(SaveKeyStore.path, "*.bin", SearchOption.AllDirectories))
            {
                FileInfo info = new FileInfo(file);
                if (info.Length == 0x80000)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            byte[] oldstamp = new byte[8];
                            fs.Read(oldstamp, 0, 8);
                            if (Utility.SequenceEqual(oldstamp, 0, break1, 0x10, 8))
                            {
                                fs.Seek(0, SeekOrigin.Begin);
                                fs.Read(savkey, 0, 0x80000);

                                // Copy the key for the slot selector
                                Array.Copy(Utility.SequenceEqual(break1, 0x80000, break2, 0x80000, 0x7F000) ? break2 : break1, 0x168, savkey, 0x80000, 4);

                                // Copy the key for the other save slot
                                Utility.xor(break1, BitConverter.ToUInt32(savkey, 0x1C), break1, (uint)(BitConverter.ToUInt32(savkey, 0x1C)-0x7F000), savkey, 0x80004, 232*30*31);

                                result = "Found old key. Based new keystream on that.\n\nPlease save new Keystream.";

                                SaveKey tmp = SaveKey.Load(savkey);
                                tmp.Upgrade();

                                SaveReaderEncrypted readerFile1, readerFile2;
                                readerFile1 = new SaveReaderEncrypted(break1, tmp);
                                readerFile1.scanSlots();
                                readerFile2 = new SaveReaderEncrypted(break2, tmp);
                                readerFile2.scanSlots();

                                respkx = readerFile1.getPkx(0).Value.data;

                                return tmp;
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            if (Utility.SequenceEqual(break1, 0x80000, break2, 0x80000, 0x7F000))
            {
                save1Save = break2;
                for (uint i = 0x27A00; i < 0x6CED0; ++i)
                    break2[i + 0x7F000] = (byte) (break2[i] ^ break1[i] ^ break1[i + 0x7F000]);
            }
            else if (Utility.SequenceEqual(break1, 0x1000, break2, 0x1000, 0x7F000))
            {
                save1Save = break1;
            }
            else
            {
                result = "The saves are seperated by more than one save.\nPlease follow the instructions.";
                respkx = new byte[232];
                return null;
            }

            Utility.Switch(ref break1, ref break2);

            #region Finding the User Specific Data: Using Valid to keep track of progress...
            // Do Break. Let's first do some sanity checking to find out the 2 offsets we're dumping from.
            // Loop through save file to find
            int fo = 0xA6A00; // Initial Offset, can tweak later.
            int success = 0;

            for (int d = 0; d < 2; d++)
            {
                // Do this twice to get both box offsets.
                for (int i = fo; i <= 0xB8F30; i += 0x10A00) 
                {
                    int err = 0;
                    // Start at findoffset and see if it matches pattern
                    if ((break1[i + 4] == break2[i + 4]) && (break1[i + 4 + 232] == break2[i + 4 + 232]))
                    {
                        // Sanity Placeholders are the same
                        for (int j = 0; j < 4; j++)
                            if (break1[i + j] == break2[i + j])
                                err++;

                        if (err < 4)
                        {
                            // Keystream ^ PID doesn't match entirely. Keep checking.
                            for (int j = 8; j < 232; j++)
                                if (break1[i + j] == break2[i + j])
                                    err++;

                            if (err < 20)
                            {
                                // Tolerable amount of difference between offsets. We have a result.
                                offset[d] = i;
                                break;
                            }
                        }
                    }
                }
                fo = offset[d] + 232 * 30;  // Fast forward out of this box to find the next.
            }

            // Now that we have our two box offsets...
            // Check to see if we actually have them.

            if ((offset[0] == 0) || (offset[1] == 0))
            {
                // We have a problem. Don't continue.
                result = "Unable to Find Box.\n";
            }
            else
            {
                // Let's go deeper. We have the two box offsets.
                // Chunk up the base streams.
                byte[,] estream1 = new Byte[30, 232];
                byte[,] estream2 = new Byte[30, 232];
                // Stuff 'em.
                for (int i = 0; i < 30; i++)    // Times we're iterating
                {
                    for (int j = 0; j < 232; j++)   // Stuff the Data
                    {
                        estream1[i, j] = break1[offset[0] + 232 * i + j];
                        estream2[i, j] = break2[offset[1] + 232 * i + j];
                    }
                }

                // Okay, now that we have the encrypted streams, formulate our EKX.
                string nick = eggnames[1];
                // Stuff in the nickname to our blank EKX.
                byte[] nicknamebytes = Encoding.Unicode.GetBytes(nick);
                Array.Resize(ref nicknamebytes, 24);
                Array.Copy(nicknamebytes, 0, empty, 0x40, nicknamebytes.Length);

                // Encrypt the Empty PKX to EKX.
                Array.Copy(empty, emptyekx, 232);
                emptyekx = PKX.decrypt(emptyekx);
                // Not gonna bother with the checksum, as this empty file is temporary.

                // Sweet. Now we just have to find the E0-E3 values. Let's get our polluted streams from each.
                // Save file 1 has empty box 1. Save file 2 has empty box 2.
                byte[,] pstream1 = new Byte[30, 232]; // Polluted Keystream 1
                byte[,] pstream2 = new Byte[30, 232]; // Polluted Keystream 2
                for (int i = 0; i < 30; i++)    // Times we're iterating
                {
                    for (int j = 0; j < 232; j++)   // Stuff the Data
                    {
                        pstream1[i, j] = (byte)(estream1[i, j] ^ emptyekx[j]);
                        pstream2[i, j] = (byte)(estream2[i, j] ^ emptyekx[j]);
                    }
                }

                // Cool. So we have a fairly decent keystream to roll with. We now need to find what the E0-E3 region is.
                // 0x00000000 Encryption Constant has the D block last. 
                // We need to make sure our Supplied Encryption Constant Pokemon have the D block somewhere else (Pref in 1 or 3).

                // First, let's get out our polluted EKX's.
                byte[,] polekx = new Byte[6, 232];
                for (int i = 0; i < 6; i++)
                    for (int j = 0; j < 232; j++) // Save file 1 has them in the second box. XOR them out with the Box2 Polluted Stream
                        polekx[i, j] = (byte)(break1[offset[1] + 232 * i + j] ^ pstream2[i, j]);

                uint[] encryptionconstants = new uint[6]; // Array for all 6 Encryption Constants. 
                int valid = 0;
                for (int i = 0; i < 6; i++)
                {
                    encryptionconstants[i] = (uint)polekx[i, 0];
                    encryptionconstants[i] += (uint)polekx[i, 1] * 0x100;
                    encryptionconstants[i] += (uint)polekx[i, 2] * 0x10000;
                    encryptionconstants[i] += (uint)polekx[i, 3] * 0x1000000;
                    // EC Obtained. Check to see if Block D is not last.
                    if (PKX.getDloc(encryptionconstants[i]) != 3)
                    {
                        valid++;
                        // Find the Origin/Region data.
                        byte[] encryptedekx = new Byte[232];
                        byte[] decryptedpkx = new Byte[232];
                        for (int z = 0; z < 232; z++)
                            encryptedekx[z] = polekx[i, z];

                        decryptedpkx = PKX.decrypt(encryptedekx);

                        // finalize data

                        // Okay, now that we have the encrypted streams, formulate our EKX.
                        nick = eggnames[decryptedpkx[0xE3] - 1];
                        // Stuff in the nickname to our blank EKX.
                        nicknamebytes = Encoding.Unicode.GetBytes(nick);
                        Array.Resize(ref nicknamebytes, 24);
                        Array.Copy(nicknamebytes, 0, empty, 0x40, nicknamebytes.Length);

                        // Dump it into our Blank EKX. We have won!
                        empty[0xE0] = decryptedpkx[0xE0];
                        empty[0xE1] = decryptedpkx[0xE1];
                        empty[0xE2] = decryptedpkx[0xE2];
                        empty[0xE3] = decryptedpkx[0xE3];
                        break;
                    }
                }
            #endregion

                if (valid == 0) // We didn't get any valid EC's where D was not in last. Tell the user to try again with different specimens.
                    result = "The 6 supplied Pokemon are not suitable. \nRip new saves with 6 different ones that originated from your save file.\n";

                else
                {
                    #region Fix up our Empty File
                    // We can continue to get our actual keystream.
                    // Let's calculate the actual checksum of our empty pkx.
                    uint chk = 0;
                    for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                        chk += BitConverter.ToUInt16(empty, i);

                    // Apply New Checksum
                    Array.Copy(BitConverter.GetBytes(chk), 0, empty, 06, 2);

                    // Okay. So we're now fixed with the proper blank PKX. Encrypt it!
                    Array.Copy(empty, emptyekx, 232);
                    emptyekx = PKX.encrypt(emptyekx);
                    Array.Resize(ref emptyekx, 232); // ensure it's 232 bytes.

                    // Copy over 0x10-0x1F (Save Encryption Unused Data so we can track data).
                    Array.Copy(break1, 0x10, savkey, 0, 0x8);
                    // Include empty data
                    savkey[0x10] = empty[0xE0]; savkey[0x11] = empty[0xE1]; savkey[0x12] = empty[0xE2]; savkey[0x13] = empty[0xE3];
                    // Copy over the scan offsets.
                    Array.Copy(BitConverter.GetBytes(offset[0]), 0, savkey, 0x1C, 4);

                    for (int i = 0; i < 30; i++)    // Times we're iterating
                    {
                        for (int j = 0; j < 232; j++)   // Stuff the Data temporarily...
                        {
                            savkey[0x100 + i * 232 + j] = (byte)(estream1[i, j] ^ emptyekx[j]);
                            savkey[0x100 + (30 * 232) + i * 232 + j] = (byte)(estream2[i, j] ^ emptyekx[j]);
                        }
                    }
                    #endregion
                    // Let's extract some of the information now for when we set the Keystream filename.
                    #region Keystream Naming
                    byte[] data1 = new Byte[232];
                    byte[] data2 = new Byte[232];
                    for (int i = 0; i < 232; i++)
                    {
                        data1[i] = (byte)(savkey[0x100 + i] ^ break1[offset[0] + i]);
                        data2[i] = (byte)(savkey[0x100 + i] ^ break2[offset[0] + i]);
                    }
                    byte[] data1a = new Byte[232]; byte[] data2a = new Byte[232];
                    Array.Copy(data1, data1a, 232); Array.Copy(data2, data2a, 232);
                    byte[] pkx1 = PKX.decrypt(data1);
                    byte[] pkx2 = PKX.decrypt(data2);
                    ushort chk1 = 0;
                    ushort chk2 = 0;
                    for (int i = 8; i < 232; i += 2)
                    {
                        chk1 += BitConverter.ToUInt16(pkx1, i);
                        chk2 += BitConverter.ToUInt16(pkx2, i);
                    }
                    if (PKX.verifyCHK(pkx1) && Convert.ToBoolean(BitConverter.ToUInt16(pkx1, 8)))
                    {
                        // Save 1 has the box1 data
                        pkx = pkx1;
                        success = 1;
                    }
                    else if (PKX.verifyCHK(pkx2) && Convert.ToBoolean(BitConverter.ToUInt16(pkx2, 8)))
                    {
                        // Save 2 has the box1 data
                        pkx = pkx2;
                        success = 1;
                    }
                    else
                    {
                        // Data isn't decrypting right...
                        for (int i = 0; i < 232; i++)
                        {
                            data1a[i] ^= empty[i];
                            data2a[i] ^= empty[i];
                        }
                        pkx1 = PKX.decrypt(data1a); pkx2 = PKX.decrypt(data2a);
                        if (PKX.verifyCHK(pkx1) && Convert.ToBoolean(BitConverter.ToUInt16(pkx1, 8)))
                        {
                            // Save 1 has the box1 data
                            pkx = pkx1;
                            success = 1;
                        }
                        else if (PKX.verifyCHK(pkx2) && Convert.ToBoolean(BitConverter.ToUInt16(pkx2, 8)))
                        {
                            // Save 2 has the box1 data
                            pkx = pkx2;
                            success = 1;
                        }
                        else
                        {
                            // Sigh...
                        }
                    }
                    #endregion
                }
            }
            
            if (success == 1)
            {
                // Clear the keystream file...
                Array.Clear(savkey, 0x100, 232*30*31);
                Array.Clear(savkey, 0x40000, 232*30*31);

                // Copy the key for the slot selector
                Array.Copy(save1Save, 0x168, savkey, 0x80000, 4);

                // Copy the key for the other save slot
                Utility.xor(break2, (uint)offset[0], break2, (uint)(offset[0]-0x7F000), savkey, 0x80004, 232*30*31);

                // Since we don't know if the user put them in in the wrong order, let's just markup our keystream with data.
                byte[] data1 = new Byte[232];
                byte[] data2 = new Byte[232];
                for (int i = 0; i < 31; i++)
                {
                    for (int j = 0; j < 30; j++)
                    {
                        Array.Copy(break1, offset[0] + i * (232 * 30) + j * 232, data1, 0, 232);
                        Array.Copy(break2, offset[0] + i * (232 * 30) + j * 232, data2, 0, 232);
                        if (data1.SequenceEqual(data2))
                        {
                            // Just copy data1 into the key file.
                            Array.Copy(data1, 0, savkey, 0x00100 + i * (232 * 30) + j * 232, 232);
                        }
                        else
                        {
                            // Copy both datas into their keystream spots.
                            Array.Copy(data1, 0, savkey, 0x00100 + i * (232 * 30) + j * 232, 232);
                            Array.Copy(data2, 0, savkey, 0x40000 + i * (232 * 30) + j * 232, 232);
                        }
                    }
                }

                // Save file diff is done, now we're essentially done. Save the keystream.

                // Success
                result = "Keystreams were successfully bruteforced!\n\n";
                result += "Save your keystream now...";
                respkx = pkx;
                SaveKey tmp = SaveKey.Load(savkey);
                tmp.Upgrade();
                return tmp;
            }
            else // Failed
                result += "Keystreams were NOT bruteforced!\n\nStart over and try again :(";
            respkx = null;
            return null;
        }
    }
}
