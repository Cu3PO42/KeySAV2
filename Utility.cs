﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public static bool Empty(this byte[] array)
        {
            return array.All(e => e == 0);
        }

        public static bool Empty(byte[] array, uint offset, uint length)
        {
            for (uint i = offset; i < offset+length; ++i)
                if (array[i] != 0)
                    return false;
            return true;
        }

        public static string TrimCString(this string str)
        {
            int index = str.IndexOf('\0');
            if (index < 0)
                return str;

            return str.Substring(0, index);
        }

        public static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public static void Switch<T>(ref T one, ref T two)
        {
            T tmp = one;
            one = two;
            two = tmp;
        }

        // Copyright for public static Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks) 2014 Microsoft Corporation
        public static Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks)
        {
            var inputTasks = tasks.ToList();

            var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
            var results = new Task<Task<T>>[buckets.Length];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TaskCompletionSource<Task<T>>();
                results[i] = buckets[i].Task;
            }

            int nextTaskIndex = -1;
            Action<Task<T>> continuation = completed =>
            {
                var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
                bucket.TrySetResult(completed);
            };

            foreach (var inputTask in inputTasks)
                inputTask.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return results;
        }
    }
}
