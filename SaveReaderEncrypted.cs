using System;
using System.IO;
using System.Linq;
using KeySAV2.Structures;

namespace KeySAV2
{
    public class SaveReaderEncrypted : ISaveReader
    {
        private readonly byte[] sav;
        private readonly SaveKey key;
        private readonly byte[] blank;
        private readonly byte activeSlot;
        private readonly string _KeyName;

        public string KeyName
        {
            get { return _KeyName; }
        }

        private readonly static byte[] zeros;
        private readonly static byte[] ezeros;

        internal SaveReaderEncrypted(byte[] file)
        {
            sav = file;
            ulong stamp = BitConverter.ToUInt64(sav, 0x10);

            Tuple<SaveKey, byte[]> tmp = SaveKeyStore.GetKey(stamp, out _KeyName);
            key = tmp.Item1;
            blank = tmp.Item2;

            if (key.slot1Flag == BitConverter.ToInt32(sav, 0x168))
                activeSlot = 0;
            else
                activeSlot = 1;

            sav.XorInPlace(key.boxOffset-0x7F000, key.slot1Key, 0, 232*30*31);
        }
        
        static SaveReaderEncrypted()
        {
            zeros = new Byte[232];
            ezeros = PKX.encrypt(zeros);
            Array.Resize(ref ezeros, 0xE8);

        }

        public void scanSlots()
        {
            bool ghost;
            for(ushort i = 0; i < 30*31; ++i)
            {
                getPkxRaw(i, 0, out ghost);
                getPkxRaw(i, 1, out ghost);
            }
        }

        public void scanSlots(ushort pos)
        {
            bool ghost;
            getPkxRaw(pos, 0, out ghost);
            getPkxRaw(pos, 1, out ghost);
        }

        public void scanSlots(ushort start, ushort end)
        {
            for (ushort i = start; i <= end; ++i)
            {
                bool ghost;
                getPkxRaw(i, 0, out ghost);
                getPkxRaw(i, 1, out ghost);
            }
        }

        public PKX? getPkx(ushort pos)
        {
            bool ghost;
            byte[] data = getPkxRaw(pos, activeSlot, out ghost);
            if (data == null)
                return null;
            return new PKX(data, (byte)(pos/30), (byte)(pos%30), ghost);
        }

        private byte[] getPkxRaw(ushort pos, byte slot, out bool ghost)
        {
            // Auto updates the keystream when it dumps important data!
            byte[] ekx = new Byte[232];

            ghost = true;

            uint keyOffset = (uint)pos * 232;
            uint savOffset = (uint)(keyOffset + key.boxOffset - (1 - slot) * 0x7F000);

            if (zeros.SequenceEqual(key.boxKey1, keyOffset) && zeros.SequenceEqual(key.boxKey2, keyOffset))
                return null;
            else if (zeros.SequenceEqual(key.boxKey1, keyOffset))
            {
                // Key2 is confirmed to dump the data.
                ekx = Utility.xor(key.boxKey2, keyOffset, sav, savOffset, 232);
                ghost = false;
            }
            else if (zeros.SequenceEqual(key.boxKey2, keyOffset))
            {
                // Haven't dumped from this slot yet.
                if (Utility.SequenceEqual(key.boxKey1, keyOffset, sav, savOffset, 232))
                {
                    // Slot hasn't changed.
                    return null;
                }
                else
                {
                    // Try and decrypt the data...
                    ekx = Utility.xor(key.boxKey1, keyOffset, sav, savOffset, 232);
                    if (PKX.verifyCHK(PKX.decrypt(ekx)))
                    {
                        // Data has been dumped!
                        // Fill keystream data with our log.
                        Array.Copy(sav, savOffset, key.boxKey2, keyOffset, 232);
                    }
                    else
                    {
                        // Try xoring with the empty data.
                        if (PKX.verifyCHK(PKX.decrypt(Utility.xor(ekx, blank))))
                        {
                            ekx = Utility.xor(ekx, blank);
                            Utility.xor(blank, 0, sav, savOffset, key.boxKey2, keyOffset, 232);
                        }
                        else if (PKX.verifyCHK(PKX.decrypt(Utility.xor(ekx, ezeros))))
                        {
                            ekx = Utility.xor(ekx, ezeros);
                            Utility.xor(ezeros, 0, sav, savOffset, key.boxKey2, keyOffset, 232);
                        }
                        else return null; // Not a failed decryption; we just haven't seen new data here yet.
                    }
                }
            }
            else
            {
                // We've dumped data at least once.
                if (Utility.SequenceEqual(key.boxKey1, keyOffset, sav, savOffset, 232) ||
                    Utility.SequenceEqual(key.boxKey1, keyOffset, Utility.xor(blank, sav, savOffset), 0, 232) ||
                    Utility.SequenceEqual(key.boxKey1, keyOffset, Utility.xor(ezeros, sav, savOffset), 0, 232))
                {
                    // Data is back to break state, but we can still dump with the other key.
                    ekx = Utility.xor(key.boxKey2, keyOffset, sav, savOffset, 232);
                    if (!PKX.verifyCHK(PKX.decrypt(ekx)))
                    {
                        if (PKX.verifyCHK(PKX.decrypt(Utility.xor(ekx, blank))))
                        {
                            ekx = Utility.xor(ekx, blank);
                            Utility.xor(blank, 0, key.boxKey2, keyOffset, key.boxKey2, keyOffset, 232);
                        }
                        else if (PKX.verifyCHK(PKX.decrypt(Utility.xor(ekx, ezeros))))
                        {
                            // Key1 decrypts our data after we remove encrypted zeros.
                            // Copy Key1 to Key2, then zero out Key1.
                            ekx = Utility.xor(ekx, ezeros);
                            Utility.xor(ezeros, 0, key.boxKey2, keyOffset, key.boxKey2, keyOffset, 232);
                        }
                        else return null; // Decryption Error
                    }
                }
                else if (Utility.SequenceEqual(key.boxKey2, keyOffset, sav, savOffset, 232) ||
                    Utility.SequenceEqual(key.boxKey2, keyOffset, Utility.xor(blank, sav, savOffset), 0, 232) ||
                    Utility.SequenceEqual(key.boxKey2, keyOffset, Utility.xor(ezeros, sav, savOffset), 0, 232))
                {
                    // Data is changed only once to a dumpable, but we can still dump with the other key.
                    ekx = Utility.xor(key.boxKey1, keyOffset, sav, savOffset, 232); 
                    if (!PKX.verifyCHK(PKX.decrypt(ekx)))
                    {
                        if (PKX.verifyCHK(PKX.decrypt(Utility.xor(ekx, blank))))
                        {
                            ekx = Utility.xor(ekx, blank);
                            Utility.xor(blank, 0, key.boxKey1, keyOffset, key.boxKey1, keyOffset, 232);
                        }
                        else if (PKX.verifyCHK(PKX.decrypt(Utility.xor(ekx, ezeros))))
                        {
                            ekx = Utility.xor(ekx, ezeros);
                            Utility.xor(ezeros, 0, key.boxKey1, keyOffset, key.boxKey1, keyOffset, 232);
                        }
                        else return null; // Decryption Error
                    }
                }
                else
                {
                    // Data has been observed to change twice! We can get our exact keystream now!
                    // Either Key1 or Key2 or Save is empty. Whichever one decrypts properly is the empty data.
                    // Oh boy... here we go...
                    ghost = false;
                    bool keydata1, keydata2 = false;
                    byte[] data1 = Utility.xor(sav, savOffset, key.boxKey1, keyOffset, 232);
                    byte[] data2 = Utility.xor(sav, savOffset, key.boxKey2, keyOffset, 232);

                    keydata1 = 
                        (PKX.verifyCHK(PKX.decrypt(data1))
                        ||
                        PKX.verifyCHK(PKX.decrypt(Utility.xor(data1, ezeros)))
                        ||
                        PKX.verifyCHK(PKX.decrypt(Utility.xor(data1, blank)))
                        );
                    keydata2 = 
                        (PKX.verifyCHK(PKX.decrypt(data2))
                        ||
                        PKX.verifyCHK(PKX.decrypt(Utility.xor(data2, ezeros)))
                        ||
                        PKX.verifyCHK(PKX.decrypt(Utility.xor(data2, blank)))
                        );

                    byte[] emptyKey, emptyKeyData;
                    uint emptyOffset;

                    if (keydata1 && keydata2)
                    {
                        // Save file is currently empty...
                        // Copy key data from save file if it decrypts with Key1 data properly.
                        emptyKey = sav;
                        emptyOffset = savOffset;
                        emptyKeyData = data1;
                    }
                    else if (keydata1) // Key 1 is empty
                    {
                        emptyKey = key.boxKey1;
                        emptyOffset = keyOffset;
                        emptyKeyData = data1;
                    }
                    else if (keydata2) // Key 2 is emtpy
                    {
                        emptyKey = key.boxKey2;
                        emptyOffset = keyOffset;
                        emptyKeyData = data2;
                    }
                    else return null; // All three are occupied

                    if (PKX.verifyCHK(PKX.decrypt(emptyKeyData)))
                    {
                        // No modifications necessary.
                        ekx = emptyKeyData;
                        Array.Copy(emptyKey, emptyOffset, key.boxKey2, keyOffset, 232);
                        Array.Copy(zeros, 0, key.boxKey1, keyOffset, 232);
                    }
                    else if (PKX.verifyCHK(PKX.decrypt(Utility.xor(emptyKeyData, ezeros))))
                    {
                        ekx = ezeros;
                        Utility.xor(ezeros, 0, emptyKey, emptyOffset, key.boxKey2, keyOffset, 232);
                        Array.Copy(zeros, 0, key.boxKey1, keyOffset, 232);
                    }
                    else if (PKX.verifyCHK(PKX.decrypt(Utility.xor(emptyKeyData, blank))))
                    {
                        ekx = ezeros;
                        Utility.xor(blank, 0, emptyKey, emptyOffset, key.boxKey2, keyOffset, 232);
                        Array.Copy(zeros, 0, key.boxKey1, keyOffset, 232);
                    }
                }
            }
            byte[] pkx = PKX.decrypt(ekx);
            if (PKX.verifyCHK(pkx))
            {
                return pkx;
            }
            else 
                return null; // Slot Decryption error?!

        }
    }
}
