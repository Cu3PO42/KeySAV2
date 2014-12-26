using System;
using System.Collections.Generic;
using System.IO;
using KeySAV2.Structures;
using KeySAV2.Exceptions;

namespace KeySAV2
{
    public static class SaveKeyStore
    {
        private static Dictionary<UInt64, Tuple<string, Lazy<Tuple<SaveKey, byte[]>>>> keys;
        private static string path;

        static SaveKeyStore()
        {
            keys = new Dictionary<UInt64, Tuple<string, Lazy<Tuple<SaveKey, byte[]>>>>();
            path = Path.Combine(System.Windows.Forms.Application.StartupPath, "data");

            ScanSaveDirectory();

            AppDomain.CurrentDomain.ProcessExit += Save;

            FileSystemWatcher watcher = new FileSystemWatcher(path, "*.bin");
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Created += (object sender, FileSystemEventArgs e) => {
                UpdateFile(e.FullPath);
            };
        }

        private static void ScanSaveDirectory()
        {
            string[] files = Directory.GetFiles(path, "*.bin", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                if (info.Length == 0xB4AD4)
                {
                    UpdateFile(file);
                }
            }
        }

        public static Tuple<SaveKey, byte[]> GetKey(ulong stamp)
        {
            if (keys.ContainsKey(stamp))
                return keys[stamp].Item2.Value;

            throw new NoKeyException();
        }

        public static void Save(object sender, EventArgs e)
        {
            foreach (var key in keys)
            {
                if (key.Value.Item2.IsValueCreated)
                    key.Value.Item2.Value.Item1.Save(key.Value.Item1);
            }
        }

        public static void UpdateFile(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    byte[] stamp = new byte[8];
                    fs.Read(stamp, 0, 8);
                    UpdateFile(file, BitConverter.ToUInt64(stamp, 0));
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public static void UpdateFile(string file, UInt64 stamp)
        {
            keys[stamp] = new Tuple<string,Lazy<Tuple<SaveKey,byte[]>>>(file, new Lazy<Tuple<SaveKey,byte[]>>(() => {
                SaveKey savkey = SaveKey.Load(file);
                return new Tuple<SaveKey,byte[]>(savkey, savkey.Blank);
            }));
        }

        public static void UpdateFile(string filename, SaveKey key, ulong stamp)
        {
            keys[stamp] = new Tuple<string, Lazy<Tuple<SaveKey, byte[]>>>(filename, new Lazy<Tuple<SaveKey, byte[]>>(
                () => new Tuple<SaveKey, byte[]>(key, key.Blank)));
        }
    }
}
