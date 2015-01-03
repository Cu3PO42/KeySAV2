using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KeySAV2.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SaveKey
    {
        public UInt64 stamp;
        UInt32 magic;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        byte[] dummy;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        public byte[] location;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        byte[] dummy2;
        public UInt32 boxOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xE0)]
        byte[] dummy3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x34AD0)]
        public byte[] boxKey1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 232)]
        public byte[] blank;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 930)]
        public bool[] slotsUnlocked;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xAFA6)]
        byte[] dummy4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x34AD0)]
        public byte[] boxKey2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xB530)]
        byte[] dummy5;
        public UInt32 slot1Flag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x34AD0)]
        public byte[] slot1Key;

        public static SaveKey Load(string file)
        {
            return Load(File.ReadAllBytes(file));
        }

        public static SaveKey Load(byte[] key)
        {
            SaveKey savkey = new SaveKey();
            int size = Marshal.SizeOf(savkey);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(key, 0, ptr, size);

            savkey = (SaveKey)Marshal.PtrToStructure(ptr, savkey.GetType());
            Marshal.FreeHGlobal(ptr);

            savkey.Upgrade();

            return savkey;
        }

        public void Save(string file)
        {
            int size = Marshal.SizeOf(this);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            File.WriteAllBytes(file, arr);
        }

        public void Upgrade()
        {
            if (magic == 0x42454546)
                return;
            magic = 0x42454546;
            blank = new byte[232];
            Array.Copy(location, 0, blank, 0xE0, 0x4);
            byte[] nicknamebytes = Encoding.Unicode.GetBytes(SaveBreaker.eggnames[blank[0xE3] - 1]);
            Array.Copy(nicknamebytes, 0, blank, 0x40, nicknamebytes.Length > 24 ? 24 : nicknamebytes.Length);

            uint chk = 0;
            for (byte i = 8; i < 232; i += 2)
                chk += BitConverter.ToUInt16(blank, i);

            Array.Copy(BitConverter.GetBytes(chk), 0, blank, 0x6, 2);
            blank = PKX.encrypt(blank);

            for (uint i = 0; i < 0x34AD0; i += 232)
                slotsUnlocked[i] = Utility.Empty(boxKey1, i, 232);
        }
    }
}
