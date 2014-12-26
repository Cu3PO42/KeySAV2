using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeySAV2
{
    public static class Utility
    {
        public static byte[] xor(byte[] one, byte[] two)
        {
            if (one.Length != two.Length)
                return null;
            int length = one.Length;
            byte[] res = new byte[length];
            for (uint i = 0; i < length; ++i)
            {
                res[i] = (byte)(one[i] ^ two[i]);
            }
            return res;
        }

        public static byte[] xor(byte[] first, byte[] second, uint secondoffset)
        {
            return xor(first, 0, second, secondoffset, (uint)first.Length);
        }

        public static byte[] xor(byte[] first, uint firstOffset, byte[] second, uint secondOffset, uint length)
        {
            byte[] res = new byte[length];
            for (uint i = 0; i < length; ++i)
            {
                res[i] = (byte)(first[firstOffset + i] ^ second[secondOffset + i]);
            }
            return res;
        }

        public static void xor(byte[] first, uint firstOffset, byte[] second, uint secondOffset, byte[] target,
            uint targetOffset, uint length)
        {
            for (uint i = 0; i < length; ++i)
                target[i + targetOffset] = (byte)(first[i + firstOffset] ^ second[i + secondOffset]);
        }

        public static void XorInPlace(this byte[] self, uint offset, byte[] other, uint otherOffset, uint length)
        {
            for (uint i = 0; i < length; ++i)
                self[i+offset] = (byte)(self[i+offset] ^ other[i+otherOffset]);
        }

        public static bool SequenceEqual(this byte[] self, byte[] other, uint offset)
        {
            for (uint i = 0; i < self.Length; ++i)
            {
                if (self[i] != other[offset+i])
                    return false;
            }
            return true;
        }

        public static bool SequenceEqual(byte[] one, uint oneOffset, byte[] two, uint twoOffset, uint length)
        {
            for (uint i = 0; i < length; ++i)
            {
                if (one[i + oneOffset] != two[i + twoOffset])
                    return false;
            }
            return true;
        }

        public static String TrimCString(this String str)
        {
            int index = str.IndexOf('\0');
            if (index < 0)
                return str;

            return str.Substring(0, index);
        }
    }
}
