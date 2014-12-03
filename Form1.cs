using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Timers;

namespace KeySAV2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            FileSystemWatcher fsw = new FileSystemWatcher();
            fsw.SynchronizingObject = this; // Timer Threading Related fix to cross-access control.
            InitializeComponent();
            myTimer.Elapsed += new ElapsedEventHandler(DisplayTimeEvent);
            this.tab_Main.AllowDrop = true;
            this.DragEnter += new DragEventHandler(tabMain_DragEnter);
            this.DragDrop += new DragEventHandler(tabMain_DragDrop);
            tab_Main.DragEnter += new DragEventHandler(tabMain_DragEnter);
            tab_Main.DragDrop += new DragEventHandler(tabMain_DragDrop);

            myTimer.Interval = 400; // milliseconds per trigger interval (0.4s)
            myTimer.Start();
            CB_Game.SelectedIndex = 0;
            CB_MainLanguage.SelectedIndex = 0;
            CB_BoxStart.SelectedIndex = 1;
            changeboxsetting(null, null);
            CB_BoxEnd.SelectedIndex = 0;
            CB_BoxEnd.Enabled = false;
            CB_Team.SelectedIndex = 0;
            CB_ExportStyle.SelectedIndex = 0;
            CB_BoxColor.SelectedIndex = 0;
            CB_HP_Type.SelectedIndex = 0;
            CB_No_IVs.SelectedIndex = 0;
            toggleFilter(null, null);
            loadINI();
            this.FormClosing += onFormClose;
            InitializeStrings();
        }
        
        // Drag & Drop Events // 
        private void tabMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void tabMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string path = files[0]; // open first D&D
            long len = new FileInfo(files[0]).Length;
            if (len == 0x100000 || len == 0x10009C)
            {
                tab_Main.SelectedIndex = 1;
                openSAV(path);
            }
            else if (len == 28256)
            {
                tab_Main.SelectedIndex = 0;
                openVID(path);
            }
            else MessageBox.Show("Dropped file is not supported.", "Error");
        }
        public void DisplayTimeEvent(object source, ElapsedEventArgs e)
        {
            find3DS();
        }
        #region Global Variables
        // Finding the 3DS SD Files
        public bool pathfound = false;
        public System.Timers.Timer myTimer = new System.Timers.Timer();
        public static string path_exe = System.Windows.Forms.Application.StartupPath;
        public static string datapath = path_exe + "\\data";
        public static string dbpath = path_exe + "\\db";
        public static string bakpath = path_exe + "\\backup";
        public string path_3DS = "";
        public string path_POW = "";

        // Language
        public string[] natures;
        public string[] types;
        public string[] abilitylist;
        public string[] movelist;
        public string[] itemlist;
        public string[] specieslist;
        public string[] balls;
        public string[] formlist;
        public string[] vivlist;
        public string[] unownlist;
        // Blank File Egg Names
        public string[] eggnames = { "タマゴ", "Egg", "Œuf", "Uovo", "Ei", "", "Huevo", "알" };

        // Inputs
        public byte[] savefile = new Byte[0x10009C];
		public byte[] savkey = new Byte[0xB4AD4];
		// Legacy key - savkey is set before use, so this is just reference
		// public byte[] savkeylegacy = new Byte[0x80000];
        public byte[] batvideo = new Byte[0x100000]; // whatever
        
        private byte[] zerobox = new Byte[232 * 30];

        // Dumping Usage
        public string vidpath = "";
        public string savpath = "";
        public string savkeypath = "";
        public string vidkeypath = "";
        public string custom1 = ""; public string custom2 = ""; public string custom3 = "";
        public bool custom1b = false; public bool custom2b = false; public bool custom3b = false;
        public string[] boxcolors = new string[] { "", "###", "####", "#####", "######" };
        private string csvdata = "";
        private string csvheader = "";
        public int dumpedcounter = 0;
        private int slots = 0;
        public bool ghost = false;
        private ushort[] selectedTSVs = new ushort[0];

        // Breaking Usage
        public string file1 = "";
        public string file2 = "";
        public string file3 = "";
        public byte[] break1 = new Byte[0x10009C];
        public byte[] break2 = new Byte[0x10009C];
        public byte[] break3 = new Byte[0x10009C];
        public byte[] video1 = new Byte[28256];
        public byte[] video2 = new Byte[28256];

        #endregion

        // Utility
        private void onFormClose(object sender, FormClosingEventArgs e)
        {
            // Save the ini file
            saveINI();
        }
        private void loadINI()
        {
            try
            {
                // Detect startup path and data path.
                if (!Directory.Exists(datapath)) // Create data path if it doesn't exist.
                    Directory.CreateDirectory(datapath);
                if (!Directory.Exists(dbpath)) // Create db path if it doesn't exist.
                    Directory.CreateDirectory(dbpath);
                if (!Directory.Exists(bakpath)) // Create backup path if it doesn't exist.
                    Directory.CreateDirectory(bakpath);
            
                // Load .ini data.
                if (!File.Exists(datapath + "\\config.ini"))
                    File.Create(datapath + "\\config.ini");
                else
                {
                    TextReader tr = new StreamReader(datapath + "\\config.ini");
                    try
                    {
                        // Load the data
                        tab_Main.SelectedIndex = Convert.ToInt16(tr.ReadLine());
                        custom1 = tr.ReadLine();
                        custom2 = tr.ReadLine();
                        custom3 = tr.ReadLine();
                        custom1b = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        custom2b = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        custom3b = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        CB_ExportStyle.SelectedIndex = Convert.ToInt16(tr.ReadLine());
                        CB_MainLanguage.SelectedIndex = Convert.ToInt16(tr.ReadLine());
                        CB_Game.SelectedIndex = Convert.ToInt16(tr.ReadLine());
                        CHK_MarkFirst.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        CHK_Split.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        CHK_BoldIVs.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
						CHK_ShowESV.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
						CHK_LegacyKey.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
						CHK_NameQuotes.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        CB_BoxColor.SelectedIndex = Convert.ToInt16(tr.ReadLine());
                        CHK_ColorBox.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        CHK_HideFirst.Checked = Convert.ToBoolean(Convert.ToInt16(tr.ReadLine()));
                        this.Height = Convert.ToInt16(tr.ReadLine());
                        this.Width = Convert.ToInt16(tr.ReadLine());
                        tr.Close();
                    }
                    catch
                    {
                        tr.Close();
                    }
                }
            }
            catch (Exception e) { MessageBox.Show("Ini config file loading failed.\n\n" + e, "Error"); }
        }
        private void saveINI()
        {
            try
            {
                // Detect startup path and data path.
                if (!Directory.Exists(datapath)) // Create data path if it doesn't exist.
                    Directory.CreateDirectory(datapath);
            
                // Load .ini data.
                if (!File.Exists(datapath + "\\config.ini"))
                    File.Create(datapath + "\\config.ini");
                else
                {
                    TextWriter tr = new StreamWriter(datapath + "\\config.ini");
                    try
                    {
                        // Load the data
                        tr.WriteLine(tab_Main.SelectedIndex.ToString());
                        tr.WriteLine(custom1.ToString());
                        tr.WriteLine(custom2.ToString());
                        tr.WriteLine(custom3.ToString());
                        tr.WriteLine(Convert.ToInt16(custom1b).ToString());
                        tr.WriteLine(Convert.ToInt16(custom2b).ToString());
                        tr.WriteLine(Convert.ToInt16(custom3b).ToString());
                        tr.WriteLine(CB_ExportStyle.SelectedIndex.ToString());
                        tr.WriteLine(CB_MainLanguage.SelectedIndex.ToString());
                        tr.WriteLine(CB_Game.SelectedIndex.ToString());
                        tr.WriteLine(Convert.ToInt16(CHK_MarkFirst.Checked).ToString());
                        tr.WriteLine(Convert.ToInt16(CHK_Split.Checked).ToString());
                        tr.WriteLine(Convert.ToInt16(CHK_BoldIVs.Checked).ToString());
						tr.WriteLine(Convert.ToInt16(CHK_ShowESV.Checked).ToString());
						tr.WriteLine(Convert.ToInt16(CHK_LegacyKey.Checked).ToString());
						tr.WriteLine(Convert.ToInt16(CHK_NameQuotes.Checked).ToString());
                        tr.WriteLine(CB_BoxColor.SelectedIndex.ToString());
                        tr.WriteLine(Convert.ToInt16(CHK_ColorBox.Checked).ToString());
                        tr.WriteLine(Convert.ToInt16(CHK_HideFirst.Checked).ToString());
                        tr.WriteLine(this.Height.ToString());
                        tr.WriteLine(this.Width.ToString());
                        tr.Close();
                    }
                    catch
                    {
                        tr.Close();
                    }
                }
            }
            catch (Exception e) { MessageBox.Show("Ini config file saving failed.\n\n" + e, "Error"); }
        }
        public volatile int game;

        // RNG
        private static uint LCRNG(uint seed)
        {
            return (seed * 0x41C64E6D + 0x00006073) & 0xFFFFFFFF;
        }
        private static Random rand = new Random();
        private static uint rnd32()
        {
            return (uint)(rand.Next(1 << 30)) << 2 | (uint)(rand.Next(1 << 2));
        }

        // PKX Struct Manipulation
        private byte[] shuffleArray(byte[] pkx, uint sv)
        {
            byte[] ekx = new Byte[260]; Array.Copy(pkx, ekx, 8);

            // Now to shuffle the blocks

            // Define Shuffle Order Structure
            var aloc = new byte[] { 0, 0, 0, 0, 0, 0, 1, 1, 2, 3, 2, 3, 1, 1, 2, 3, 2, 3, 1, 1, 2, 3, 2, 3 };
            var bloc = new byte[] { 1, 1, 2, 3, 2, 3, 0, 0, 0, 0, 0, 0, 2, 3, 1, 1, 3, 2, 2, 3, 1, 1, 3, 2 };
            var cloc = new byte[] { 2, 3, 1, 1, 3, 2, 2, 3, 1, 1, 3, 2, 0, 0, 0, 0, 0, 0, 3, 2, 3, 2, 1, 1 };
            var dloc = new byte[] { 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 0, 0, 0, 0, 0, 0 };

            // Get Shuffle Order
            var shlog = new byte[] { aloc[sv], bloc[sv], cloc[sv], dloc[sv] };

            // UnShuffle Away!
            for (int b = 0; b < 4; b++)
                Array.Copy(pkx, 8 + 56 * shlog[b], ekx, 8 + 56 * b, 56);

            // Fill the Battle Stats back
            if (pkx.Length > 232)
                Array.Copy(pkx, 232, ekx, 232, 28);
            return ekx;
        }
        private byte[] decryptArray(byte[] ekx)
        {
            byte[] pkx = new Byte[0xE8]; Array.Copy(ekx, pkx, 0xE8);
            uint pv = BitConverter.ToUInt32(pkx, 0);
            uint sv = (((pv & 0x3E000) >> 0xD) % 24);

            uint seed = pv;

            // Decrypt Blocks with RNG Seed
            for (int i = 8; i < 232; i += 2)
            {
                int pre = pkx[i] + ((pkx[i + 1]) << 8);
                seed = LCRNG(seed);
                int seedxor = (int)((seed) >> 16);
                int post = (pre ^ seedxor);
                pkx[i] = (byte)((post) & 0xFF);
                pkx[i + 1] = (byte)(((post) >> 8) & 0xFF);
            }

            // Deshuffle
            pkx = shuffleArray(pkx, sv);
            return pkx;
        }
        private byte[] encryptArray(byte[] pkx)
        {
            // Shuffle
            uint pv = BitConverter.ToUInt32(pkx, 0);
            uint sv = (((pv & 0x3E000) >> 0xD) % 24);

            byte[] ekxdata = new Byte[pkx.Length]; Array.Copy(pkx, ekxdata, pkx.Length);

            // If I unshuffle 11 times, the 12th (decryption) will always decrypt to ABCD.
            // 2 x 3 x 4 = 12 (possible unshuffle loops -> total iterations)
            for (int i = 0; i < 11; i++)
                ekxdata = shuffleArray(ekxdata, sv);

            uint seed = pv;
            // Encrypt Blocks with RNG Seed
            for (int i = 8; i < 232; i += 2)
            {
                int pre = ekxdata[i] + ((ekxdata[i + 1]) << 8);
                seed = LCRNG(seed);
                int seedxor = (int)((seed) >> 16);
                int post = (pre ^ seedxor);
                ekxdata[i] = (byte)((post) & 0xFF);
                ekxdata[i + 1] = (byte)(((post) >> 8) & 0xFF);
            }

            // Encrypt the Party Stats
            seed = pv;
            for (int i = 232; i < 260; i += 2)
            {
                int pre = ekxdata[i] + ((ekxdata[i + 1]) << 8);
                seed = LCRNG(seed);
                int seedxor = (int)((seed) >> 16);
                int post = (pre ^ seedxor);
                ekxdata[i] = (byte)((post) & 0xFF);
                ekxdata[i + 1] = (byte)(((post) >> 8) & 0xFF);
            }

            // Done
            return ekxdata;
        }
        private int getDloc(uint ec)
        {
            // Define Shuffle Order Structure
            var dloc = new byte[] { 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 0, 0, 0, 0, 0, 0 };
            uint sv = (((ec & 0x3E000) >> 0xD) % 24);

            return dloc[sv];
        }
        private bool verifyCHK(byte[] pkx)
        {
            ushort chk = 0;
            for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                chk += BitConverter.ToUInt16(pkx, i);

            ushort actualsum = BitConverter.ToUInt16(pkx, 0x6);
            if ((BitConverter.ToUInt16(pkx, 0x8) > 750) || (BitConverter.ToUInt16(pkx, 0x90) != 0)) 
                return false;
            return (chk == actualsum);
        }

        // File Type Loading
        private void B_OpenSAV_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.InitialDirectory = savpath;
            ofd.RestoreDirectory = true;
            ofd.Filter = "SAV|*.sav;*.bin";
            if (ofd.ShowDialog() == DialogResult.OK)
                openSAV(ofd.FileName);
        }
        private void B_OpenVid_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.InitialDirectory = vidpath;
            ofd.RestoreDirectory = true;
            ofd.Filter = "Battle Video|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
                openVID(ofd.FileName);
        }
        private void openSAV(string path)
        {
            openSAV_(path, ref savefile, ref savkeypath, true);
        }
        private void openSAV_(string path, ref byte[] savefile, ref string savkeypath, bool showUI)
        {
            // check to see if good input file
            long len = new FileInfo(path).Length;
            if (len != 0x100000 && len != 0x10009C)
            { 
                if(showUI) MessageBox.Show("Incorrect File Size");
                return;
            }
            
            TB_SAV.Text = path;

            // Go ahead and load the save file into RAM...
            byte[] input = File.ReadAllBytes(path);
            Array.Copy(input, input.Length % 0x100000, savefile, 0, 0x100000);
            // Fetch Stamp
            ulong stamp = BitConverter.ToUInt64(savefile, 0x10);
            string keyfile = (CHK_LegacyKey.Checked) ? fetchKey(stamp, 0x80000) : fetchKey(stamp, 0xB4AD4);
            if (keyfile == "")
            {
                if (showUI)
                {
                    L_KeySAV.Text = "Key not found. Please break for this SAV first.";
                    B_GoSAV.Enabled = false;
                }
                return;
            }
            else
            {
                if (showUI)
                {
                    B_GoSAV.Enabled = true;
                    L_KeySAV.Text = new FileInfo(keyfile).Name;
                }
                savkeypath = keyfile;
            }

            if(showUI)
                B_GoSAV.Enabled = CB_BoxEnd.Enabled = CB_BoxStart.Enabled = B_BKP_SAV.Visible = !(keyfile == "");
            byte[] key = File.ReadAllBytes(keyfile);
            byte[] empty = new Byte[232];

            // Get our empty file set up.
            Array.Copy(key, 0x10, empty, 0xE0, 0x4);
            string nick = eggnames[empty[0xE3] - 1];
            // Stuff in the nickname to our blank EKX.
            byte[] nicknamebytes = Encoding.Unicode.GetBytes(nick);
            Array.Resize(ref nicknamebytes, 24);
            Array.Copy(nicknamebytes, 0, empty, 0x40, nicknamebytes.Length);
            // Fix CHK
            uint chk = 0;
            for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                chk += BitConverter.ToUInt16(empty, i);
            // Apply New Checksum
            Array.Copy(BitConverter.GetBytes(chk), 0, empty, 06, 2);
            empty = encryptArray(empty);
            Array.Resize(ref empty, 0xE8);
            // Save file is already loaded.
			// Scan save (slot 2)
			scanSAV(savefile, key, empty, showUI);
			if (!CHK_LegacyKey.Checked)
			{
				// If we need to use slot 1 later, the original state does not need to be preserved
				byte[] savefile2;
				if (BitConverter.ToUInt32(key, 0x80000) == BitConverter.ToUInt32(savefile, 0x168))
				{
					savefile2 = savefile;
				}
				// If we use slot 2 later, we need to preserve the original state
				else
				{
					savefile2 = new byte[savefile.Length];
					Array.Copy(savefile, savefile2, savefile.Length);
				}
				// If slot one was used for the last save copy the boxes to slot 2 and apply key
				int boxoffset = BitConverter.ToInt32(key, 0x1C);
				for(int i = 0, j = boxoffset; i<232*30*31; ++i, ++j)
				{
					savefile2[j] = (byte)(savefile2[j - 0x7F000] ^ key[0x80004 + i]);
				}
				// Scan slot 1
				scanSAV(savefile2, key, empty, showUI);
			}
            File.WriteAllBytes(keyfile, key); // Key has been scanned for new slots, re-save key.
        }
        private void openVID(string path)
        {
            // check to see if good input file
            B_GoBV.Enabled = CB_Team.Enabled = false;
            long len = new FileInfo(path).Length;
            if (len != 28256)
            { MessageBox.Show("Incorrect File Size"); return; }

            TB_BV.Text = path;

            // Go ahead and load the save file into RAM...
            batvideo = File.ReadAllBytes(path);
            // Fetch Stamp
            ulong stamp = BitConverter.ToUInt64(batvideo, 0x10);
            string keyfile = fetchKey(stamp, 0x1000);
            B_GoBV.Enabled = CB_Team.Enabled = B_BKP_BV.Visible = (keyfile != "");
            if (keyfile == "")
            { L_KeyBV.Text = "Key not found. Please break for this BV first."; return; }
            else
            {
                string name = new FileInfo(keyfile).Name;
                L_KeyBV.Text = "Key: " + name;
                vidkeypath = keyfile;
            }
            // Check up on the key file...
            CB_Team.Items.Clear();
            CB_Team.Items.Add("My Team");
            byte[] bvkey = File.ReadAllBytes(vidkeypath);
            if (BitConverter.ToUInt64(bvkey, 0x800) != 0)
                CB_Team.Items.Add("Enemy Team");

            CB_Team.SelectedIndex = 0;
        }
        private string fetchKey(ulong stamp, int length)
        {
            // Find the Key in the datapath (program//data folder)
            string[] files = Directory.GetFiles(datapath,"*.bin", SearchOption.AllDirectories);
            byte[] data = new Byte[length];
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fi = new FileInfo(files[i]);
                {
                    if (fi.Length == length)
                    {
                        data = File.ReadAllBytes(files[i]);
                        ulong newstamp = BitConverter.ToUInt64(data, 0x0);
                        if (newstamp == stamp)
                            return files[i];
                    }
                }
            }
            // else return nothing
            return "";
        }

        // File Dumping
        // SAV
        private byte[] fetchpkx(byte[] input, byte[] keystream, int pkxoffset, int key1off, int key2off, byte[] blank)
        {
            // Auto updates the keystream when it dumps important data!
            ghost = true;
            byte[] ekx = new Byte[232];
            byte[] key1 = new Byte[232]; Array.Copy(keystream, key1off, key1, 0, 232);
            byte[] key2 = new Byte[232]; Array.Copy(keystream, key2off, key2, 0, 232);
            byte[] encrypteddata = new Byte[232]; Array.Copy(input, pkxoffset, encrypteddata, 0, 232);

            byte[] zeros = new Byte[232];
            byte[] ezeros = encryptArray(zeros); Array.Resize(ref ezeros, 0xE8);
            if (zeros.SequenceEqual(key1) && zeros.SequenceEqual(key2))
                return null;
            else if (zeros.SequenceEqual(key1))
            {
                // Key2 is confirmed to dump the data.
                ekx = xortwos(key2, encrypteddata);
                ghost = false;
            }
            else if (zeros.SequenceEqual(key2))
            {
                // Haven't dumped from this slot yet.
                if (key1.SequenceEqual(encrypteddata))
                {
                    // Slot hasn't changed.
                    return null;
                }
                else
                {
                    // Try and decrypt the data...
                    ekx = xortwos(key1, encrypteddata);
                    if (verifyCHK(decryptArray(ekx)))
                    {
                        // Data has been dumped!
                        // Fill keystream data with our log.
                        Array.Copy(encrypteddata, 0, keystream, key2off, 232);
                    }
                    else
                    {
                        // Try xoring with the empty data.
                        if (verifyCHK(decryptArray(xortwos(ekx, blank))))
                        {
                            ekx = xortwos(ekx, blank);
                            Array.Copy(xortwos(encrypteddata, blank), 0, keystream, key2off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(ekx, ezeros))))
                        {
                            ekx = xortwos(ekx, ezeros);
                            Array.Copy(xortwos(encrypteddata, ezeros), 0, keystream, key2off, 232);
                        }
                        else return null; // Not a failed decryption; we just haven't seen new data here yet.
                    }
                }
            }
            else
            {
                // We've dumped data at least once.
                if (key1.SequenceEqual(encrypteddata) || key1.SequenceEqual(xortwos(encrypteddata,blank)) || key1.SequenceEqual(xortwos(encrypteddata,ezeros)))
                {
                    // Data is back to break state, but we can still dump with the other key.
                    ekx = xortwos(key2, encrypteddata);
                    if (!verifyCHK(decryptArray(ekx)))
                    {
                        if (verifyCHK(decryptArray(xortwos(ekx, blank))))
                        {
                            ekx = xortwos(ekx, blank);
                            Array.Copy(xortwos(key2, blank), 0, keystream, key2off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(ekx, ezeros))))
                        {
                            // Key1 decrypts our data after we remove encrypted zeros.
                            // Copy Key1 to Key2, then zero out Key1.
                            ekx = xortwos(ekx, ezeros);
                            Array.Copy(xortwos(key2, ezeros), 0, keystream, key2off, 232);
                        }
                        else return null; // Decryption Error
                    }
                }
                else if (key2.SequenceEqual(encrypteddata) || key2.SequenceEqual(xortwos(encrypteddata, blank)) || key2.SequenceEqual(xortwos(encrypteddata, ezeros)))
                {
                    // Data is changed only once to a dumpable, but we can still dump with the other key.
                    ekx = xortwos(key1, encrypteddata); 
                    if (!verifyCHK(decryptArray(ekx)))
                    {
                        if (verifyCHK(decryptArray(xortwos(ekx, blank))))
                        {
                            ekx = xortwos(ekx, blank);
                            Array.Copy(xortwos(key1, blank), 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(ekx, ezeros))))
                        {
                            ekx = xortwos(ekx, ezeros);
                            Array.Copy(xortwos(key1, ezeros), 0, keystream, key1off, 232);
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
                    byte[] data1 = xortwos(encrypteddata, key1);
                    byte[] data2 = xortwos(encrypteddata, key2);

                    keydata1 = 
                        (verifyCHK(decryptArray(data1))
                        ||
                        verifyCHK(decryptArray(xortwos(data1, ezeros)))
                        ||
                        verifyCHK(decryptArray(xortwos(data1, blank)))
                        );
                    keydata2 = 
                        (verifyCHK(decryptArray(data2))
                        ||
                        verifyCHK(decryptArray(xortwos(data2, ezeros)))
                        ||
                        verifyCHK(decryptArray(xortwos(data2, blank)))
                        );
                    if (!keydata1 && !keydata2) 
                        return null; // All 3 are occupied.
                    if (keydata1 && keydata2)
                    {
                        // Save file is currently empty...
                        // Copy key data from save file if it decrypts with Key1 data properly.

                        if (verifyCHK(decryptArray(data1)))
                        {
                            // No modifications necessary.
                            ekx = data1;
                            Array.Copy(encrypteddata, 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(data1, ezeros))))
                        {
                            ekx = ezeros;
                            Array.Copy(xortwos(encrypteddata,ezeros), 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(data1, blank))))
                        {
                            ekx = ezeros;
                            Array.Copy(xortwos(encrypteddata, blank), 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else return null; // unreachable
                    }
                    else if (keydata1) // Key 1 data is empty
                    {
                        if (verifyCHK(decryptArray(data1)))
                        {
                            ekx = data1;
                            Array.Copy(key1, 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(data1, ezeros))))
                        {
                            ekx = xortwos(data1, ezeros);
                            Array.Copy(xortwos(key1, ezeros), 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(data1, blank))))
                        {
                            ekx = xortwos(data1, blank);
                            Array.Copy(xortwos(key1, blank), 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else return null; // unreachable
                    }
                    else if (keydata2)
                    {
                        if (verifyCHK(decryptArray(data2)))
                        {
                            ekx = data2;
                            Array.Copy(key2, 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(data2, ezeros))))
                        {
                            ekx = xortwos(data2, ezeros);
                            Array.Copy(xortwos(key2, ezeros), 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else if (verifyCHK(decryptArray(xortwos(data2, blank))))
                        {
                            ekx = xortwos(data2, blank);
                            Array.Copy(xortwos(key2, blank), 0, keystream, key2off, 232);
                            Array.Copy(zeros, 0, keystream, key1off, 232);
                        }
                        else return null; // unreachable
                    }
                }
            }
            byte[] pkx = decryptArray(ekx);
            if (verifyCHK(pkx))
            {
                slots++;
                return pkx;
            }
            else 
                return null; // Slot Decryption error?!
        }
        private void scanSAV(byte[] input, byte[] keystream, byte[] blank, bool showUI = true)
        {
            slots = 0;
            int boxoffset = BitConverter.ToInt32(keystream, 0x1C);
            for (int i = 0; i < 930; i++)
                fetchpkx(input, keystream, boxoffset + i * 232, 0x100 + i * 232, 0x40000 + i * 232, blank);

            if(showUI)
                L_SAVStats.Text = String.Format("{0}/930", slots);
            //MessageBox.Show("Unlocked: " + unlockedslots + " Soft: " + softslots);
        }
        private void dumpPKX(bool isSAV, byte[] pkx, int dumpnum, int dumpstart = 0)
        {
			if (isSAV && ghost && CHK_HideFirst.Checked) return;
            if (pkx == null || !verifyCHK(pkx)) //RTB_SAV.AppendText("SLOT LOCKED\r\n");
                return;

            Structures.PKX data = new Structures.PKX(pkx);

            // Printout Parsing
            if (data.species == 0) //RTB_SAV.AppendText("SLOT EMPTY");
                return;

            string box = "";
            string slot = "";
            if (isSAV)
			{
				box = "B"+(dumpstart + (dumpnum/30)).ToString("00");
				slot = (((dumpnum%30) / 6 + 1).ToString() + "," + (dumpnum % 6 + 1).ToString());
			}
			else
			{
				box = "~";
				slot = dumpnum.ToString();
			}
            string species = specieslist[data.species];
            string gender = data.genderstring;
            string nature = natures[data.nature];
            string ability = abilitylist[data.ability];
            string hp = data.HP_IV.ToString();
            string atk = data.ATK_IV.ToString();
            string def = data.DEF_IV.ToString();
            string spa = data.SPA_IV.ToString();
            string spd = data.SPD_IV.ToString();
            string spe = data.SPE_IV.ToString();
            string hptype = types[data.hptype];
            string ESV = data.ESV.ToString("0000");
            string TSV = data.TSV.ToString("0000");
            string ball = balls[data.ball];
            string nickname = data.nicknamestr;
            string otname = data.ot;
            string TID = data.TID.ToString("00000");
            string SID = data.SID.ToString("00000");
            string move1 = movelist[data.move1];
            string move2 = movelist[data.move2];
            string move3 = movelist[data.move3];
            string move4 = movelist[data.move4];
            string ev_hp = data.HP_EV.ToString();
            string ev_at = data.ATK_EV.ToString();
            string ev_de = data.DEF_EV.ToString();
            string ev_sa = data.SPA_EV.ToString();
            string ev_sd = data.SPD_EV.ToString();
            string ev_se = data.SPE_EV.ToString();
			string number = (isSAV) ? (dumpnum % 30 + 1).ToString("00") : slot;

            // Bonus
            string relearn1 = movelist[data.eggmove1].ToString();
            string relearn2 = movelist[data.eggmove2].ToString();
            string relearn3 = movelist[data.eggmove3].ToString();
            string relearn4 = movelist[data.eggmove4].ToString();
            string isshiny = ""; if (data.isshiny) isshiny = "★";
            string isegg = ""; if (data.isegg) isegg = "★";
			
			// Extra fields for CSV custom output
			// TODO: add an option to use actual markers below instead of numbers
            // string[] markers = new string[] { "●", "▲", "■", "♥", "★", "♦" };
			int IVcounter = 0;
            string hpm = ""; if (data.HP_IV == 31) { hpm = "1"; IVcounter++; }
            string atkm = ""; if (data.ATK_IV == 31) { atkm = "2"; IVcounter++; }
            string defm = ""; if (data.DEF_IV == 31) { defm = "3"; IVcounter++; }
            string spam = ""; if (data.SPA_IV == 31) { spam = "4"; IVcounter++; }
            string spdm = ""; if (data.SPD_IV == 31) { spdm = "5"; IVcounter++; }
            string spem = ""; if (data.SPE_IV == 31) { spem = "6"; IVcounter++; }
            string IVs = IVcounter.ToString() + " IVs";
			string IVsum = (data.HP_IV + data.ATK_IV + data.DEF_IV + data.SPA_IV + data.SPD_IV + data.SPE_IV).ToString();
			string EVsum = (data.HP_EV + data.ATK_EV + data.DEF_EV + data.SPA_EV + data.SPD_EV + data.SPE_EV).ToString();
			string eggDate = (data.egg_year.ToString() == "0") ? "" : "20" + data.egg_year.ToString("00") + "-" + data.egg_month.ToString("00") + "-" + data.egg_day.ToString("00");
			string metDate = "20" + data.met_year.ToString("00") + "-" + data.met_month.ToString("00") + "-" + data.met_day.ToString("00");

            //ExtraBonus (implemented with code from PKHeX)
            int exp = Convert.ToInt32(data.exp);
            string level = getLevel(Convert.ToInt32(data.species), exp).ToString();
            string region = getregion(data.gamevers, 0);
            string game = getregion(data.gamevers, 1);
            string country = getcountry(data.countryID);
            string helditem = itemlist[data.helditem];
            string language = getlanguage(data.otlang);
			// Mark is for Gen 6 Pokemon, so X Y OR AS
            string mark = (data.gamevers >= 24 && data.gamevers <= 27) ? "⬟" : "";
            if (data.helditem == 0) helditem = "";
            if (data.isegg) level = "";
			string PID = data.PID.ToString();
			string dex = data.species.ToString();
			string form = data.altforms.ToString();
			
            bool statisfiesFilters = true;

			if (isSAV)
			{
				while (CHK_Enable_Filtering.Checked)
				{
					if (CHK_Egg.Checked && !data.isegg) { statisfiesFilters = false; break; }
					bool checkHp = false;
					if (CB_HP_Type.SelectedIndex > 0)
					{
						if (CB_HP_Type.SelectedIndex != data.hptype) { statisfiesFilters = false; break; }
						checkHp = true;
					}

					int perfects = Convert.ToInt16(CB_No_IVs.SelectedItem);
					bool ivsSelected = CHK_IV_HP.Checked || CHK_IV_Atk.Checked || CHK_IV_Def.Checked || CHK_IV_SpAtk.Checked || CHK_IV_SpDef.Checked || CHK_IV_Spe.Checked;
					if (hp == "31" ||checkHp && hp == "30") --perfects;
					else if (ivsSelected && CHK_IV_HP.Checked != RAD_IVs_Miss.Checked) { statisfiesFilters = false; break; }
					if ((atk == "31" || checkHp && atk == "30") && !CHK_Special_Attacker.Checked || (atk == "0" || checkHp && atk == "1") && CHK_Special_Attacker.Checked) --perfects;
					else if (ivsSelected && CHK_IV_Atk.Checked != RAD_IVs_Miss.Checked) { statisfiesFilters = false; break; }
					if (def == "31" || checkHp && def == "30") --perfects;
					else if (ivsSelected && CHK_IV_Def.Checked != RAD_IVs_Miss.Checked) { statisfiesFilters = false; break; }
					if (spa == "31" || checkHp && spa == "30") --perfects;
					else if (ivsSelected && CHK_IV_SpAtk.Checked != RAD_IVs_Miss.Checked) { statisfiesFilters = false; break; }
					if (spd == "31" || checkHp && spd == "30") --perfects;
					else if (ivsSelected && CHK_IV_SpDef.Checked != RAD_IVs_Miss.Checked) { statisfiesFilters = false; break; }
					if ((spe == "31" || checkHp && spe == "30") && !CHK_Trickroom.Checked || (spe == "0" || checkHp && spe == "1") && CHK_Trickroom.Checked) --perfects;
					else if (ivsSelected && CHK_IV_Spe.Checked != RAD_IVs_Miss.Checked) { statisfiesFilters = false; break; }
					if (perfects > 0) { statisfiesFilters = false; break; }

					if (CHK_Is_Shiny.Checked || CHK_Hatches_Shiny_For_Me.Checked || CHK_Hatches_Shiny_For.Checked)
					{
						// TODO: Should probably cache this somewhere...
						if (!(CHK_Is_Shiny.Checked && data.isshiny ||
							data.isegg && CHK_Hatches_Shiny_For_Me.Checked && ESV == TSV ||
							data.isegg && CHK_Hatches_Shiny_For.Checked && Array.IndexOf(selectedTSVs, data.ESV) > -1))
						{ statisfiesFilters = false; break; }
					}

					break;
				}
			}

            if (statisfiesFilters || !isSAV)
            {
                if (!data.isegg && !CHK_ShowESV.Checked) ESV = "";

                // Vivillon Forms...
                if (data.species >= 664 && data.species <= 666)
                    species += "-" + vivlist[data.altforms];

				// Unknown Forms
				if (data.species == 201)
					species += "-" + unownlist[data.altforms];

				if (((CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || (CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6)) && CHK_BoldIVs.Checked))
                {
                    if (hp == "31") hp = "**31**";
                    if (atk == "31") atk = "**31**";
                    if (def == "31") def = "**31**";
                    if (spa == "31") spa = "**31**";
                    if (spd == "31") spd = "**31**";
                    if (spe == "31") spe = "**31**";
                }

                string format = RTB_OPTIONS.Text;
                if (CB_ExportStyle.SelectedIndex >= 6)
                    format = "{0} - {1} - {2} ({3}) - {4} - {5} - {6}.{7}.{8}.{9}.{10}.{11} - {12} - {13}";

                if (CB_ExportStyle.SelectedIndex == 6)
                {
					string csvFormat = "{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}\r\n";
					if (isSAV) csvFormat = "{0}," + csvFormat;
                    csvdata += String.Format(csvFormat,
                        box, slot, species, gender, nature, ability, hp, atk, def, spa, spd, spe, hptype, ESV, TSV, nickname, otname, ball, TID, SID, ev_hp, ev_at, ev_de, ev_sa, ev_sd, ev_se, move1, move2, move3, move4, relearn1, relearn2, relearn3, relearn4, isshiny, isegg);
                }
                if (CB_ExportStyle.SelectedIndex == 7)
				{
					string csvnickname = nickname;
					string csvotname = otname;
					// Escape any quotes so we can add quotes in the CSV export to avoid errors with commas in nicknames and trainer names
					if (CHK_NameQuotes.Checked)
					{
						csvnickname.Replace("\"", "\\\"");
						csvotname.Replace("\"", "\\\"");
						csvnickname = "\"" + csvnickname + "\"";
						csvotname = "\"" + csvotname + "\"";
					}
					string csvFormat = "{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{54},{47},{48},{49},{50},{51},{52},{53},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{55},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{56},{57}\r\n";
					if (isSAV) csvFormat = "{0}," + csvFormat;
					csvdata += String.Format(csvFormat,
						box, slot, species, gender, nature, ability, hp, atk, def, spa, spd, spe, hptype, ESV, TSV, csvnickname, csvotname, ball, TID, SID, ev_hp, ev_at, ev_de, ev_sa, ev_sd, ev_se, move1, move2, move3, move4, relearn1, relearn2, relearn3, relearn4, isshiny, isegg, level, region, country, helditem, language, game, number, PID, mark, dex, form, hpm, atkm, defm, spam, spdm, spem, IVs, IVsum, EVsum, eggDate, metDate);
				}
                if (CB_ExportStyle.SelectedIndex == 8)
                {
                    isshiny = "";
                    if (data.isshiny)
                        isshiny = " ★";
                    if (data.isnick)
                        data.nicknamestr += String.Format(" ({0})", specieslist[data.species]);

                    string savedname = number + " - " + 
                        data.species.ToString("000") + isshiny + " "
                        + data.nicknamestr + " - "
                        + data.chk.ToString("X4") + data.EC.ToString("X8");
					if (isSAV) savedname = box + " " + savedname;
                    File.WriteAllBytes(dbpath + "\\" + CleanFileName(savedname) + ".pk6", pkx);
                }
                if (!(CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || (CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6 && CHK_R_Table.Checked)))
                {
                    if (ESV != "")
                        ESV = "[" + ESV + "]";
                }
				string result = String.Format(format, box, slot, species, gender, nature, ability, hp, atk, def, spa, spd, spe, hptype, ESV, TSV, nickname, otname, ball, TID, SID, ev_hp, ev_at, ev_de, ev_sa, ev_sd, ev_se, move1, move2, move3, move4, relearn1, relearn2, relearn3, relearn4, isshiny, isegg, level, region, country, helditem, language, game, number, PID, mark, dex, form, hpm, atkm, defm, spam, spdm, spem, IVs, IVsum, EVsum, eggDate, metDate);

                if (isSAV && ghost && CHK_MarkFirst.Checked) result = "~" + result;
                dumpedcounter++;
				if (isSAV) RTB_SAV.AppendText(result + "\r\n"); else RTB_VID.AppendText(result + "\r\n");
            }
        }
        private void DumpSAV(object sender, EventArgs e)
        {
            csvheader = (CB_ExportStyle.SelectedIndex == 6) ? 
			"Box,Row,Column,Species,Gender,Nature,Ability,HP IV,ATK IV,DEF IV,SPA IV,SPD IV,SPE IV,HP Type,ESV,TSV,Nickname,OT,Ball,TID,SID,HP EV,ATK EV,DEF EV,SPA EV,SPD EV,SPE EV,Move 1,Move 2,Move 3,Move 4,Relearn 1, Relearn 2, Relearn 3, Relearn 4, Shiny, Egg" : 
			"Box,Row,Column,Species,Gender,Nature,Ability,HP IV,ATK IV,DEF IV,SPA IV,SPD IV,SPE IV,IV Sum,1,2,3,4,5,6,IVs,HP Type,ESV,TSV,Nickname,OT,Ball,TID,SID,HP EV,ATK EV,DEF EV,SPA EV,SPD EV,SPE EV,EV Sum,Move 1,Move 2,Move 3,Move 4,Relearn 1,Relearn 2,Relearn 3,Relearn 4,Shiny,Egg,Level,Region,Country,Held Item,Language,Game,Number,PID,Mark,Dex Number,Form,Egg Received,Met/Hatched";
            csvdata = csvheader + "\r\n";
            RTB_SAV.Clear();
            dumpedcounter = 0;
            // Load our Keystream file.
            byte[] keystream = File.ReadAllBytes(savkeypath);
            byte[] empty = new Byte[232];
            // Save file is already loaded.

            // Get our empty file set up.
            Array.Copy(keystream, 0x10, empty, 0xE0, 0x4);
            string nick = eggnames[empty[0xE3] - 1];
            // Stuff in the nickname to our blank EKX.
            byte[] nicknamebytes = Encoding.Unicode.GetBytes(nick);
            Array.Resize(ref nicknamebytes, 24);
            Array.Copy(nicknamebytes, 0, empty, 0x40, nicknamebytes.Length);
            // Fix CHK
            uint chk = 0;
            for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                chk += BitConverter.ToUInt16(empty, i);

            // Apply New Checksum
            Array.Copy(BitConverter.GetBytes(chk), 0, empty, 06, 2);
            empty = encryptArray(empty);
            Array.Resize(ref empty, 0xE8);

            // Get our dumping parameters.
            int boxoffset = BitConverter.ToInt32(keystream, 0x1C);
            int offset = 0;
            int count = 30;
            int boxstart = 1;
            if (CB_BoxStart.Text == "All")
                count = 30 * 31;
            else
            {
                boxoffset += (Convert.ToInt16(CB_BoxStart.Text) - 1) * 30 * 232;
                offset += (Convert.ToInt16(CB_BoxStart.Text) - 1) * 30 * 232;
                count = (Convert.ToInt16(CB_BoxEnd.Text) - Convert.ToInt16(CB_BoxStart.Text) + 1) * 30;
                boxstart = Convert.ToInt16(CB_BoxStart.Text);
            }

            // Get our TSVs for filtering
            ushort tmp = 0;
            selectedTSVs = (from val in Regex.Split(TB_SVs.Text, @"\s*[\s,;.]\s*") where UInt16.TryParse(val, out tmp) select tmp).ToArray();

            string header = String.Format(RTB_OPTIONS.Text, "Box", "Slot", "Species", "Gender", "Nature", "Ability", "HP", "ATK", "DEF", "SPA", "SPD", "SPE", "HiddenPower", "ESV", "TSV", "Nick", "OT", "Ball", "TID", "SID", "HP EV", "ATK EV", "DEF EV", "SPA EV", "SPD EV", "SPE EV", "Move 1", "Move 2", "Move 3", "Move 4", "Relearn 1", "Relearn 2", "Relearn 3", "Relearn 4", "Shiny", "Egg", "Level", "Region", "Country", "Held Item", "Language", "Game", "Number", "PID", "Mark", "Dex Number", "Form", "1", "2", "3", "4", "5", "6", "IVs", "IV Sum", "EV Sum", "Egg Received", "Met/Hatched");
            if (CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || (CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6 && CHK_R_Table.Checked))
            {
                int args = Regex.Split(RTB_OPTIONS.Text, "{").Length;
                header += "\r\n|";
                for (int i = 0; i < args; i++)
                    header += ":---:|";

                if (!CHK_Split.Checked) // Still append the header if we aren't doing it for every box.
                {
                    // Add header if reddit
                    if (CHK_ColorBox.Checked)
                    {
                        if (CB_BoxColor.SelectedIndex == 0)
                            RTB_SAV.AppendText(boxcolors[1 + (rnd32() % 4)]);
                        else RTB_SAV.AppendText(boxcolors[CB_BoxColor.SelectedIndex - 1]);
                    }
                    // Append Box Name then Header
                    RTB_SAV.AppendText("B" + (boxstart).ToString("00") + "+\r\n\r\n");
                    RTB_SAV.AppendText(header + "\r\n");
                } 
            }

            for (int i = 0; i < count; i++)
            {
                if (i % 30 == 0 && CHK_Split.Checked)
                {
                    RTB_SAV.AppendText("\r\n");
                    // Add box header
                    if ((CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || ((CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6)) && CHK_R_Table.Checked))
                    {
                        if (CHK_ColorBox.Checked)
                        {
                            // Add Reddit Coloring
                            if (CB_BoxColor.SelectedIndex == 0)
                                RTB_SAV.AppendText(boxcolors[1 + ((i / 30 + boxstart) % 4)]);
                            else RTB_SAV.AppendText(boxcolors[CB_BoxColor.SelectedIndex - 1]);
                        }
                    }
                    // Append Box Name then Header
                    RTB_SAV.AppendText("B" + (i / 30 + boxstart).ToString("00") + "\r\n\r\n");
                    RTB_SAV.AppendText(header + "\r\n");
                }
                byte[] pkx = fetchpkx(savefile, keystream, boxoffset + i * 232, 0x100 + offset + i * 232, 0x40000 + offset + i * 232, empty);
                dumpPKX(true, pkx, i, boxstart);
            }

            // Copy Results to Clipboard
            try { Clipboard.SetText(RTB_SAV.Text); }
            catch { };
            RTB_SAV.AppendText("\r\nData copied to clipboard!\r\nDumped: " + dumpedcounter);
            RTB_SAV.Select(RTB_SAV.Text.Length - 1, 0);
            RTB_SAV.ScrollToCaret();

            if (CB_ExportStyle.SelectedIndex == 6 || CB_ExportStyle.SelectedIndex == 7)
            {
                SaveFileDialog savecsv = new SaveFileDialog();
                savecsv.Filter = "Spreadsheet|*.csv";
                savecsv.FileName = "KeySAV Data Dump.csv";
                if (savecsv.ShowDialog() == DialogResult.OK)
                    System.IO.File.WriteAllText(savecsv.FileName, csvdata, Encoding.UTF8);
            }
        }
        private void dumpBV(object sender, EventArgs e)
        {
            csvheader = (CB_ExportStyle.SelectedIndex == 6) ? 
			"Position,Species,Gender,Nature,Ability,HP IV,ATK IV,DEF IV,SPA IV,SPD IV,SPE IV,HP Type,ESV,TSV,Nickname,OT,Ball,TID,SID,HP EV,ATK EV,DEF EV,SPA EV,SPD EV,SPE EV,Move 1,Move 2,Move 3,Move 4,Relearn 1, Relearn 2, Relearn 3, Relearn 4, Shiny, Egg" : 
			"Position,Species,Gender,Nature,Ability,HP IV,ATK IV,DEF IV,SPA IV,SPD IV,SPE IV,1,2,3,4,5,6,IVs,HP Type,ESV,TSV,Nickname,OT,Ball,TID,SID,HP EV,ATK EV,DEF EV,SPA EV,SPD EV,SPE EV,Move 1,Move 2,Move 3,Move 4,Relearn 1,Relearn 2,Relearn 3,Relearn 4,Shiny,Egg,Level,Region,Country,Held Item,Language,Game,Number,PID,Mark,Dex Number,Form,Egg Received,Met/Hatched";
            csvdata = csvheader + "\r\n";
            RTB_VID.Clear();
            // player @ 0xX100, opponent @ 0x1800;
            byte[] keystream = File.ReadAllBytes(vidkeypath);
            byte[] key = new Byte[260];
            byte[] empty = new Byte[260];
            byte[] emptyekx = encryptArray(empty);
            byte[] ekx = new Byte[260];
            int offset = 0x4E18;
            int keyoff = 0x100;
            if (CB_Team.SelectedIndex == 1)
            {
                offset = 0x5438;
                keyoff = 0x800;
            }

			string header = String.Format(RTB_OPTIONS.Text, "Box", "Slot", "Species", "Gender", "Nature", "Ability", "HP", "ATK", "DEF", "SPA", "SPD", "SPE", "HiddenPower", "ESV", "TSV", "Nick", "OT", "Ball", "TID", "SID", "HP EV", "ATK EV", "DEF EV", "SPA EV", "SPD EV", "SPE EV", "Move 1", "Move 2", "Move 3", "Move 4", "Relearn 1", "Relearn 2", "Relearn 3", "Relearn 4", "Shiny", "Egg", "Level", "Region", "Country", "Held Item", "Language", "Game", "Number", "PID", "Mark", "Dex Number", "Form", "1", "2", "3", "4", "5", "6", "IVs", "IV Sum", "EV Sum", "Egg Received", "Met/Hatched");

            // Add header if reddit
            if (CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || ((CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6) && CHK_R_Table.Checked))
            {
                // Add Reddit Coloring
                if (CHK_ColorBox.Checked)
                {
                    if (CB_BoxColor.SelectedIndex == 0)
                    {
                        RTB_VID.AppendText(boxcolors[1 + (rnd32() % 4)]);
                    }
                    else RTB_VID.AppendText(boxcolors[CB_BoxColor.SelectedIndex - 1]);
                }
                RTB_VID.AppendText(CB_Team.Text + "\r\n\r\n");
                
                int args = Regex.Split(RTB_OPTIONS.Text, "{").Length;
                header += "\r\n|";
                for (int i = 0; i < args; i++)
                    header += ":---:|";

                RTB_VID.AppendText(header + "\r\n");
            }



            for (int i = 0; i < 6; i++)
            {
                Array.Copy(batvideo, offset + 260 * i, ekx, 0, 260);
                Array.Copy(keystream, keyoff + 260 * i, key, 0, 260);
                ekx = xortwos(ekx, key);
                if (verifyCHK(decryptArray(ekx)))
                    dumpPKX(false, decryptArray(ekx),i+1);
                else
                    dumpPKX(false, null,i);
            }

            // Copy Results to Clipboard
            try { Clipboard.SetText(RTB_VID.Text); }
            catch { };
            RTB_VID.AppendText("\r\nData copied to clipboard!"); 
            
            RTB_VID.Select(RTB_VID.Text.Length - 1, 0);
            RTB_VID.ScrollToCaret(); 
            if (CB_ExportStyle.SelectedIndex == 6 || CB_ExportStyle.SelectedIndex == 7)
            {
                SaveFileDialog savecsv = new SaveFileDialog();
                savecsv.Filter = "Spreadsheet|*.csv";
                savecsv.FileName = "KeySAV Data Dump.csv";
                if (savecsv.ShowDialog() == DialogResult.OK)
                {
                    string path = savecsv.FileName;
                    System.IO.File.WriteAllText(path, csvdata, Encoding.UTF8);
                }
            }
        }

        private void toggleFilter(object sender, EventArgs e)
        {
            CB_HP_Type.Enabled = CB_No_IVs.Enabled = CHK_Trickroom.Enabled =
            CHK_Special_Attacker.Enabled = RAD_IVs_Miss.Enabled = RAD_IVs.Enabled =
            CHK_IV_HP.Enabled = CHK_IV_Atk.Enabled = CHK_IV_Def.Enabled =
            CHK_IV_SpAtk.Enabled = CHK_IV_SpDef.Enabled = CHK_IV_Spe.Enabled =
            CHK_Is_Shiny.Enabled = CHK_Hatches_Shiny_For_Me.Enabled =
            CHK_Hatches_Shiny_For.Enabled = TB_SVs.Enabled =
            CHK_Egg.Enabled = CHK_Enable_Filtering.Checked;
        }

        // File Keystream Breaking
        private void loadBreak1(object sender, EventArgs e)
        {
            // Open Save File
            OpenFileDialog boxsave = new OpenFileDialog();
            boxsave.Filter = "Save/BV File|*.*";

            if (boxsave.ShowDialog() == DialogResult.OK)
            {
                string path = boxsave.FileName;
                byte[] input = File.ReadAllBytes(path);
                if ((input.Length == 0x10009C) || input.Length == 0x100000)
                {
                    Array.Copy(input, input.Length % 0x100000, break1, 0, 0x100000);
                    TB_File1.Text = path;
                    file1 = "SAV";
                }
                else if (input.Length == 28256)
                {
                    Array.Copy(input, video1, 28256);
                    TB_File1.Text = path;
                    file1 = "BV";
                }
                else
                {
                    file1 = "";
                    MessageBox.Show("Incorrect File Loaded: Neither a SAV (1MB) or Battle Video (~27.5KB).", "Error");
                }
            } 
            togglebreak();
        }
        private void loadBreak2(object sender, EventArgs e)
        {
            // Open Save File
            OpenFileDialog boxsave = new OpenFileDialog();
            boxsave.Filter = "Save/BV File|*.*";

            if (boxsave.ShowDialog() == DialogResult.OK)
            {
                string path = boxsave.FileName;
                byte[] input = File.ReadAllBytes(path);
                if ((input.Length == 0x10009C) || input.Length == 0x100000)
                {
                    Array.Copy(input, input.Length % 0x100000, break2, 0, 0x100000); // Force save to 0x100000
                    TB_File2.Text = path;
                    file2 = "SAV";
                }
                else if (input.Length == 28256)
                {
                    Array.Copy(input, video2, 28256);
                    TB_File2.Text = path;
                    file2 = "BV";
                }
                else
                {
                    file2 = "";
                    MessageBox.Show("Incorrect File Loaded: Neither a SAV (1MB) or Battle Video (~27.5KB).", "Error");
                }
            }
            togglebreak();
        }

        private void loadBreak3(object sender, EventArgs e)
        {
            // Open Save File
            OpenFileDialog boxsave = new OpenFileDialog();
            boxsave.Filter = "Save/BV File|*.*";

            if (boxsave.ShowDialog() == DialogResult.OK)
            {
                string path = boxsave.FileName;
                byte[] input = File.ReadAllBytes(path);
                if ((input.Length == 0x10009C) || input.Length == 0x100000)
                {
                    Array.Copy(input, input.Length % 0x100000, break3, 0, 0x100000); // Force save to 0x100000
                    TB_File3.Text = path;
                    file3 = "SAV";
                }
                else if (input.Length == 28256)
                {
                    file3 = "";
                    MessageBox.Show("Incorrect File Loaded: For breaking Battle Videos a file 3 is not needed.", "Error");
                }
                else
                {
                    file3 = "";
                    MessageBox.Show("Incorrect File Loaded: Not a SAV (1MB).", "Error");
                }
            }
            togglebreak();
        }

        private void loadBreakFolder(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                TB_Folder.Text = folder.SelectedPath;
                B_BreakFolder.Enabled = true;
            }
        }

        private void togglebreak()
        {
            B_Break.Enabled = false;
            if (TB_File1.Text != "" && TB_File2.Text != "")
                if ((file1 == "SAV" && file2 == "SAV" && file3 == "SAV" && TB_File3.Text != "" && !CHK_LegacyKey.Checked) || (file1 == "SAV" && file2 == "SAV" && CHK_LegacyKey.Checked) || (file1 == "BV" && file2 == "BV"))
                   B_Break.Enabled = true;
        }

        // Specific Breaking Branch
        private void B_Break_Click(object sender, EventArgs e)
        {
            if (file1 == file2)
            {
                if (file1 == "SAV")
                    breakSAV();
                else if (file1 == "BV")
                    breakBV();
                else
                    return;
            }
        }
        private void breakBV()
        {
            // Do Trick
            {
                byte[] ezeros = encryptArray(new Byte[260]);
                byte[] xorstream = new Byte[260 * 6];
                byte[] breakstream = new Byte[260 * 6];
                byte[] bvkey = new Byte[0x1000];
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
                byte[] pkx = decryptArray(ekx1);
                if (!verifyCHK(pkx))
                { MessageBox.Show("Improperly set up Battle Videos. Please follow directions and try again", "Error"); return; }
                // 

                // Start filling up our key...
                #region Key Filling (bvkey)
                // Copy in the unique CTR encryption data to ID the video...
                Array.Copy(video1, 0x10, bvkey, 0, 0x10);

                // Copy unlocking data
                byte[] key1 = new Byte[260]; Array.Copy(video1, 0x4E18, key1, 0, 260);
                Array.Copy(xortwos(ekx1, key1), 0, bvkey, 0x100, 260);
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
                byte[] pkx2 = decryptArray(ekx2);
                if (verifyCHK(decryptArray(ekx2)) && (BitConverter.ToUInt16(pkx2,0x8) != 0))
                {
                    Array.Copy(xortwos(ekx2,key2), 0, bvkey, 0x800, 260);
                    Array.Copy(video1, 0x5438 + 260, bvkey, 0x800 + 260, 260 * 5); // XORstream from save1 has just keystream.

                    for (int i = 0; i < (260 * 5); i++)
                        bvkey[0x800 + 260 + i] ^= ezeros[i % 260];

                    MessageBox.Show("Can dump from Opponent Data on this key too!");
                }
                #endregion

                string ot = TrimFromZero(Encoding.Unicode.GetString(pkx, 0xB0, 24));
                ushort tid = BitConverter.ToUInt16(pkx, 0xC);
                ushort sid = BitConverter.ToUInt16(pkx, 0xE);
                ushort tsv = (ushort)((tid ^ sid) >> 4);
                // Finished, allow dumping of breakstream
                MessageBox.Show(String.Format("Success!\r\nYour first Pokemon's TSV: {0}\r\nOT: {1}\r\n\r\nPlease save your keystream.", tsv.ToString("0000"),ot));


                FileInfo fi = new FileInfo(TB_File1.Text);
                string bvnumber = Regex.Split(fi.Name, "(-)")[0];
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = CleanFileName(String.Format("BV Key - {0}.bin", bvnumber));
                string ID = sfd.InitialDirectory;
                sfd.InitialDirectory = path_exe + "\\data";
                sfd.RestoreDirectory = true;
                sfd.Filter = "Video Key|*.bin";
                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, bvkey);
                else
                    MessageBox.Show("Chose not to save keystream.", "Alert");
                sfd.InitialDirectory = ID; sfd.RestoreDirectory = true;
            }
        }
        private void breakSAV()
        {
            int[] offset = new int[2];
            byte[] empty = new Byte[232];
            byte[] emptyekx = new Byte[232];
            byte[] ekxdata = new Byte[232];
            byte[] pkx = new Byte[232];
            byte[] slotsKey = new byte[0];
            byte[] save1Save = break1;
            #region Finding the User Specific Data: Using Valid to keep track of progress...
            // Do Break. Let's first do some sanity checking to find out the 2 offsets we're dumping from.
            // Loop through save file to find
            int fo = savefile.Length / 2 + 0x20000; // Initial Offset, can tweak later.
            int success = 0;

            string result = "";

            for (int d = 0; d < 2; d++)
            {
                // Do this twice to get both box offsets.
                for (int i = fo; i < 0xEE000; i++)
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
                result = "Unable to Find Box.\r\n";
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
                emptyekx = encryptArray(emptyekx);
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
                    if (getDloc(encryptionconstants[i]) != 3)
                    {
                        valid++;
                        // Find the Origin/Region data.
                        byte[] encryptedekx = new Byte[232];
                        byte[] decryptedpkx = new Byte[232];
                        for (int z = 0; z < 232; z++)
                            encryptedekx[z] = polekx[i, z];

                        decryptedpkx = decryptArray(encryptedekx);

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
                    result = "The 6 supplied Pokemon are not suitable. \r\nRip new saves with 6 different ones that originated from your save file.\r\n";

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
                    emptyekx = encryptArray(emptyekx);
                    Array.Resize(ref emptyekx, 232); // ensure it's 232 bytes.

                    // Empty EKX obtained. Time to set up our key file.
                    savkey = (CHK_LegacyKey.Checked) ? new Byte[0x80000] : new Byte[0xB4AD4];
                    // Copy over 0x10-0x1F (Save Encryption Unused Data so we can track data).
                    Array.Copy(break1, 0x10, savkey, 0, 0x10);
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
                    byte[] pkx1 = decryptArray(data1);
                    byte[] pkx2 = decryptArray(data2);
                    ushort chk1 = 0;
                    ushort chk2 = 0;
                    for (int i = 8; i < 232; i += 2)
                    {
                        chk1 += BitConverter.ToUInt16(pkx1, i);
                        chk2 += BitConverter.ToUInt16(pkx2, i);
                    }
                    if (verifyCHK(pkx1) && Convert.ToBoolean(BitConverter.ToUInt16(pkx1, 8)))
                    {
                        // Save 1 has the box1 data
                        pkx = pkx1;
                        success = 1;
                    }
                    else if (verifyCHK(pkx2) && Convert.ToBoolean(BitConverter.ToUInt16(pkx2, 8)))
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
                        pkx1 = decryptArray(data1a); pkx2 = decryptArray(data2a);
                        if (verifyCHK(pkx1) && Convert.ToBoolean(BitConverter.ToUInt16(pkx1, 8)))
                        {
                            // Save 1 has the box1 data
                            pkx = pkx1;
                            success = 1;
                        }
                        else if (verifyCHK(pkx2) && Convert.ToBoolean(BitConverter.ToUInt16(pkx2, 8)))
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
            if (success == 1 && !CHK_LegacyKey.Checked)
            {
                byte[] diff1 = new byte[31*30*232];
                byte[] diff2 = new byte[31*30*232];
                for(uint i = 0; i < 31*30*232; ++i)
                {
                    diff1[i] = (byte)(break1[offset[0] + i] ^ break1[offset[0] + i - 0x7F000]);
                }
                for(uint i = 0; i < 31*30*232; ++i)
                {
                    diff2[i] = (byte)(break2[offset[0] + i] ^ break2[offset[0] + i - 0x7F000]);
                }
                if (diff1.SequenceEqual(diff2))
                {
                    bool break3is1 = true;
                    for(uint i = (uint)offset[0]; i<offset[0] + 31*30*232; ++i)
                    {
                        if(!(break2[i] == break3[i]))
                        {
                            break3is1 = false;
                            break;
                        }
                    }
                    if (break3is1) save1Save = break3;
                    slotsKey = diff1;
                }
                else success = 0;
            }
            if (success == 1)
            {
                // Markup the save to know that boxes 1 & 2 are dumpable.
                savkey[0x20] = 3; // 00000011 (boxes 1 & 2)

                // Clear the keystream file...
                for (int i = 0; i < 31; i++)
                {
                    Array.Copy(zerobox, 0, savkey, 0x00100 + i * (232 * 30), 232 * 30);
                    Array.Copy(zerobox, 0, savkey, 0x40000 + i * (232 * 30), 232 * 30);
                }

				if (!CHK_LegacyKey.Checked)
				{
					// Copy the key for the slot selector
					Array.Copy(save1Save, 0x168, savkey, 0x80000, 4);

					// Copy the key for the other save slot
					Array.Copy(slotsKey, 0, savkey, 0x80004, 232*30*31);
				}

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
                result = "Keystreams were successfully bruteforced!\r\n\r\n";
                result += "Save your keystream now...";
                MessageBox.Show(result);

                // From our PKX data, fetch some details to name our key file...
                string ot = TrimFromZero(Encoding.Unicode.GetString(pkx, 0xB0, 24));
                ushort tid = BitConverter.ToUInt16(pkx, 0xC);
                ushort sid = BitConverter.ToUInt16(pkx, 0xE);
                ushort tsv = (ushort)((tid ^ sid) >> 4);
                SaveFileDialog sfd = new SaveFileDialog();
                string ID = sfd.InitialDirectory;
                sfd.InitialDirectory = path_exe + "\\data";
                sfd.RestoreDirectory = true;
				string preFileName = (CHK_LegacyKey.Checked) ? "SAV Key Legacy" : "SAV Key";
                sfd.FileName = CleanFileName(String.Format(preFileName + " - {0} - ({1}.{2}) - TSV {3}.bin", ot, tid.ToString("00000"), sid.ToString("00000"), tsv.ToString("0000")));
                sfd.Filter = "Save Key|*.bin";
                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, savkey);
                else
                    MessageBox.Show("Chose not to save keystream.", "Alert");

                sfd.InitialDirectory = ID; sfd.RestoreDirectory = true;
            }
            else // Failed
                MessageBox.Show(result + "Keystreams were NOT bruteforced!\r\n\r\nStart over and try again :(");
        }

        // Utility
        private byte[] xortwos(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length) return null;
            byte[] arr3 = new Byte[arr1.Length];
            for (int i = 0; i < arr1.Length; i++)
                arr3[i] = (byte)(arr1[i] ^ arr2[i]);
            return arr3;
        }
        public static string TrimFromZero(string input)
        {
            int index = input.IndexOf('\0');
            if (index < 0)
                return input;

            return input.Substring(0, index);
        }
        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
        public static FileInfo GetNewestFile(DirectoryInfo directory)
        {
            return directory.GetFiles()
                .Union(directory.GetDirectories().Select(d => GetNewestFile(d)))
                .OrderByDescending(f => (f == null ? DateTime.MinValue : f.LastWriteTime))
                .FirstOrDefault();
        }

        // SD Detection
        private void changedetectgame(object sender, EventArgs e)
        {
            game = CB_Game.SelectedIndex;
            myTimer.Start();
        }
        private void detectMostRecent()
        {
            // Fetch the selected save file and video
            //try
            {
                if (game == 0)
                {
                    // X
                    savpath = path_3DS + "\\title\\00040000\\00055d00\\";
                    vidpath = path_3DS + "\\extdata\\00000000\\0000055d\\00000000\\";
                }
                else if (game == 1)
                {
                    // Y
                    savpath = path_3DS + "\\title\\00040000\\00055e00\\";
                    vidpath = path_3DS + "\\extdata\\00000000\\0000055e\\00000000\\";
                }
                else if (game == 2) 
                {
                    // OR
                    savpath = path_3DS + "\\title\\00040000\\0011c400\\";
                    vidpath = path_3DS + "\\extdata\\00000000\\000011c4\\00000000\\";
                }
                else if (game == 3)
                {
                    // AS
                    savpath = path_3DS + "\\title\\00040000\\0011c500\\";
                    vidpath = path_3DS + "\\extdata\\00000000\\000011c5\\00000000\\";
                }

                if (Directory.Exists(savpath))
                {
                    if (File.Exists(savpath + "00000001.sav"))
                        this.Invoke(new MethodInvoker(delegate { openSAV(savpath + "00000001.sav"); }));
                }
                // Fetch the latest video
                if (Directory.Exists(vidpath))
                {
                    try
                    {
                        FileInfo BV = GetNewestFile(new DirectoryInfo(vidpath));
                        if (BV.Length == 28256)
                        { this.Invoke(new MethodInvoker(delegate { openVID(BV.FullName); })); }
                    }
                    catch { }
                }
            }
            //catch { }
        }
        private void find3DS()
        {
            // start by checking if the 3DS file path exists or not.
            string[] DriveList = Environment.GetLogicalDrives();
            for (int i = 1; i < DriveList.Length; i++)
            {
                path_3DS = DriveList[i] + "Nintendo 3DS";
                if (Directory.Exists(path_3DS))
                    break;

                path_3DS = null;
            }
            if (path_3DS == null) // No 3DS SD Card Detected
                return;
            else
            {
                // 3DS data found in SD card reader. Let's get the title folder location!
                string[] folders = Directory.GetDirectories(path_3DS, "*", System.IO.SearchOption.AllDirectories);

                // Loop through all the folders in the Nintendo 3DS folder to see if any of them contain 'title'.
                for (int i = 0; i < folders.Length; i++)
                {
                    DirectoryInfo di = new DirectoryInfo(folders[i]);
                    if (di.Name == "title" || di.Name == "extdata")
                    {
                        path_3DS = di.Parent.FullName.ToString();
                        myTimer.Stop();
                        detectMostRecent();
                        pathfound = true;
                        return;
                    }
                }
            }
        }

        // UI Prompted Updates
        private void changeboxsetting(object sender, EventArgs e)
        {
            CB_BoxEnd.Visible = CB_BoxEnd.Enabled = L_BoxThru.Visible = !(CB_BoxStart.Text == "All");
            if (CB_BoxEnd.Enabled)
            {
                int start = Convert.ToInt16(CB_BoxStart.Text);
                int oldValue = Convert.ToInt16(CB_BoxEnd.SelectedItem);
                CB_BoxEnd.Items.Clear();
                for (int i = start; i < 32; i++)
                    CB_BoxEnd.Items.Add(i.ToString());
                CB_BoxEnd.SelectedIndex = (start >= oldValue ? 0 : oldValue-start);
            }
        }
        private void B_ShowOptions_Click(object sender, EventArgs e)
        {

            Help help = new Help();
            help.Show(this);
            /*if (help.StartPosition == FormStartPosition.CenterParent)
            {
                var x = Location.X + ((Width - help.Width) / 2) + 340;
                var y = Location.Y + ((Height - help.Height) / 2) - 45;
                help.Location = new Point(Math.Max(x, 0), Math.Max(y, 0));
            } */
        }
        private void changeExportStyle(object sender, EventArgs e)
        {
            /*
                Default
                Reddit
                TSV
                Custom 1
                Custom 2
                Custom 3
                CSV default
				CSV custom
                To .PK6 File 
             */
            CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = false;
            if (CB_ExportStyle.SelectedIndex == 0) // Default
            {
                CHK_R_Table.Visible = false;
				CHK_NameQuotes.Visible = false;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                    "{0} - {1} - {2} ({3}) - {4} - {5} - {6}.{7}.{8}.{9}.{10}.{11} - {12} - {13}";
            }
            else if (CB_ExportStyle.SelectedIndex == 1) // Reddit
            {
                CHK_R_Table.Visible = false;
				CHK_NameQuotes.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "{0} | {1} | {2} ({3}) | {4} | {5} | {6}.{7}.{8}.{9}.{10}.{11} | {12} | {13} |";
            }
            else if (CB_ExportStyle.SelectedIndex == 2) // TSV
            {
                CHK_R_Table.Visible = false;
				CHK_NameQuotes.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "{0} | {1} | {16} | {18} | {14} |";
            }
            else if (CB_ExportStyle.SelectedIndex == 3) // Custom 1
            {
                CHK_R_Table.Visible = true; CHK_R_Table.Checked = custom1b;
				CHK_NameQuotes.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = false;
                RTB_OPTIONS.Text = custom1;
            }
            else if (CB_ExportStyle.SelectedIndex == 4) // Custom 2
            {
                CHK_R_Table.Visible = true; CHK_R_Table.Checked = custom2b;
				CHK_NameQuotes.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = false;
                RTB_OPTIONS.Text = custom2;
            }
            else if (CB_ExportStyle.SelectedIndex == 5) // Custom 3
            {
                CHK_R_Table.Visible = true; CHK_R_Table.Checked = custom3b;
				CHK_NameQuotes.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = false;
                RTB_OPTIONS.Text = custom3;
            }
            else if (CB_ExportStyle.SelectedIndex == 6) // CSV
            {
                CHK_R_Table.Visible = false;
				CHK_NameQuotes.Visible = true;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "CSV will output standard fields listed below (useful for other programs that process this file):\r\n{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}";
            }
            else if (CB_ExportStyle.SelectedIndex == 7) // CSV custom
            {
                CHK_R_Table.Visible = false;
				CHK_NameQuotes.Visible = true;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "CSV custom will output ALL columns:\r\n{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{54},{47},{48},{49},{50},{51},{52},{53},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{55},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{56},{57}";
            }
            else if (CB_ExportStyle.SelectedIndex == 8) // PK6
            {
                CHK_R_Table.Visible = false;
				CHK_NameQuotes.Visible = false;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "Files will be saved in .PK6 format, and the default method will display.";
            }
        }
        private void changeFormatText(object sender, EventArgs e)
        {
            if (CB_ExportStyle.SelectedIndex == 3) // Custom 1
                custom1 = RTB_OPTIONS.Text;
            else if (CB_ExportStyle.SelectedIndex == 4) // Custom 2
                custom2 = RTB_OPTIONS.Text;
            else if (CB_ExportStyle.SelectedIndex == 5) // Custom 3
                custom3 = RTB_OPTIONS.Text;
        }
        private void changeTableStatus(object sender, EventArgs e)
        {
            if (CB_ExportStyle.SelectedIndex == 3) // Custom 1
                custom1b = CHK_R_Table.Checked;
            else if (CB_ExportStyle.SelectedIndex == 4) // Custom 2
                custom2b = CHK_R_Table.Checked;
            else if (CB_ExportStyle.SelectedIndex == 5) // Custom 3
                custom3b = CHK_R_Table.Checked;
        }
        private void changeReadOnly(object sender, EventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            if (rtb.ReadOnly) rtb.BackColor = Color.FromKnownColor(KnownColor.Control);
            else rtb.BackColor = Color.FromKnownColor(KnownColor.White);
        }

        // Translation
        private void changeLanguage(object sender, EventArgs e)
        {
            InitializeStrings();
        }
        private string[] getStringList(string f, string l)
        {
            object txt = Properties.Resources.ResourceManager.GetObject("text_" + f + "_" + l); // Fetch File, \n to list.
            List<string> rawlist = ((string)txt).Split(new char[] { '\n' }).ToList();

            string[] stringdata = new string[rawlist.Count];
            for (int i = 0; i < rawlist.Count; i++)
                stringdata[i] = rawlist[i].Trim();
            return stringdata;
        }
        private void InitializeStrings()
        {
            string[] lang_val = { "en", "ja", "fr", "it", "de", "es", "ko" };
            string curlanguage = lang_val[CB_MainLanguage.SelectedIndex];

            string l = curlanguage;
            natures = getStringList("Natures", l);
            types = getStringList("Types", l);
            abilitylist = getStringList("Abilities", l);
            movelist = getStringList("Moves", l);
            itemlist = getStringList("Items", l);
            specieslist = getStringList("Species", l);
            formlist = getStringList("Forms", l);

            abilitylist[0] = itemlist[0] = movelist[0] = "(" + itemlist[0] + ")";

            int[] ballindex = {
                                  0,1,2,3,4,5,6,7,8,9,0xA,0xB,0xC,0xD,0xE,0xF,0x10,
                                  0x1EC,0x1ED,0x1EE,0x1EF,0x1F0,0x1F1,0x1F2,0x1F3,
                                  0x240 
                              };
            balls = new string[ballindex.Length];
            for (int i = 0; i < ballindex.Length; i++)
                balls[i] = itemlist[ballindex[i]];

            // vivillon pattern list
            vivlist = new string[20];
            vivlist[0] = formlist[666];
            for (int i = 1; i < 20; i++)
                vivlist[i] = formlist[835+i];
            // unown list
            unownlist = new string[29];
            unownlist[0] = formlist[201];
            for (int i = 1; i < 29; i++)
                unownlist[i] = formlist[201 + i];
        }
		
        // I have an idea of what I'm doing
        // But it's not pretty
		// by VioletSpatula

        private int getLevel(int species, int exp)
        {
            DataTable spectable = SpeciesTable();
            int growth = (int)spectable.Rows[species][1];
            int tl = 1; // Initial Level
            if (exp == 0) { return tl; }
            DataTable table = ExpTable();
            if ((int)table.Rows[tl][growth + 1] < exp)
            {
                while ((int)table.Rows[tl][growth + 1] < exp)
                {
                    // While EXP for guessed level is below our current exp
                    tl += 1;
                    if (tl == 100)
                    {
                        getEXP(100, species);
                        return tl;
                    }
                    // when calcexp exceeds our exp, we exit loop
                }
                if ((int)table.Rows[tl][growth + 1] == exp)
                {
                    // Matches level threshold
                    return tl;
                }
                else return (tl - 1);
            }
            else return tl;
        }

        private int getEXP(int level, int species)
        {
            // Fetch Growth
            DataTable spectable = SpeciesTable();
            int growth = (int)spectable.Rows[species][1];
            int exp;
            if ((level == 0) || (level == 1))
            {
                exp = 0;
                //TB_EXP.Text = exp.ToString();
                return exp;
            }
            switch (growth)
            {
                case 0: // Erratic
                    if (level <= 50)
                    {
                        exp = (level * level * level) * (100 - level) / 50;
                    }
                    else if (level < 69)
                    {
                        exp = (level * level * level) * (150 - level) / 100;
                    }
                    else if (level < 99)
                    {
                        exp = (level * level * level) * ((1911 - 10 * level) / 3) / 500;
                    }
                    else
                    {
                        exp = (level * level * level) * (160 - level) / 100;
                    }
                    //TB_EXP.Text = exp.ToString();
                    return exp;
                case 1: // Fast
                    exp = 4 * (level * level * level) / 5;
                    //TB_EXP.Text = exp.ToString();
                    return exp;
                case 2: // Medium Fast
                    exp = (level * level * level);
                    //TB_EXP.Text = exp.ToString();
                    return exp;
                case 3: // Medium Slow
                    exp = 6 * (level * level * level) / 5 - 15 * (level * level) + 100 * level - 140;
                    //TB_EXP.Text = exp.ToString();
                    return exp;
                case 4:
                    exp = 5 * (level * level * level) / 4;
                    //TB_EXP.Text = exp.ToString();
                    return exp;
                case 5:
                    if (level <= 15)
                    {
                        exp = (level * level * level) * ((((level + 1) / 3) + 24) / 50);
                    }
                    else if (level <= 36)
                    {
                        exp = (level * level * level) * ((level + 14) / 50);
                    }
                    else
                    {
                        exp = (level * level * level) * (((level / 2) + 32) / 50);
                    }
                    //TB_EXP.Text = exp.ToString();
                    return exp;
            }
            return 0;
        }

        public string getcountry(int country)
        {
            Dictionary<string, int> country_list = new Dictionary<string, int>();
			country_list.Add("---", 0);
			country_list.Add("Albania", 64);
			country_list.Add("Andorra", 122);
			country_list.Add("Anguilla", 8);
			country_list.Add("Antigua and Barbuda", 9);
			country_list.Add("Argentina",  10);
			country_list.Add("Aruba",  11);
			country_list.Add("Australia",  65);
			country_list.Add("Austria",  66);
			country_list.Add("Azerbaijan", 113);
			country_list.Add("Bahamas", 12);
			country_list.Add("Barbados", 13);
			country_list.Add("Belgium", 67);
			country_list.Add("Belize", 14);
			country_list.Add("Bermuda", 186);
			country_list.Add("Bolivia", 15);
			country_list.Add("Bosnia and Herzegovina", 68);
			country_list.Add("Botswana", 69);
			country_list.Add("Brazil", 16);
			country_list.Add("British Virgin Islands", 17);
			country_list.Add("Bulgaria", 70);
			country_list.Add("Canada", 18);
			country_list.Add("Cayman Islands", 19);
			country_list.Add("Chad", 117);
			country_list.Add("Chile", 20);
			country_list.Add("China", 160);
			country_list.Add("Colombia", 21);
			country_list.Add("Costa Rica", 22);
			country_list.Add("Croatia", 71);
			country_list.Add("Cyprus", 72);
			country_list.Add("Czech Republic", 73);
			country_list.Add("Denmark (Kingdom of)", 74);
			country_list.Add("Djibouti", 120);
			country_list.Add("Dominica", 23);
			country_list.Add("Dominican Republic", 24);
			country_list.Add("Ecuador", 25);
			country_list.Add("El Salvador", 26);
			country_list.Add("Eritrea", 119);
			country_list.Add("Estonia", 75);
			country_list.Add("Finland", 76);
			country_list.Add("France", 77);
			country_list.Add("French Guiana", 27);
			country_list.Add("Germany", 78);
			country_list.Add("Gibraltar", 123);
			country_list.Add("Greece", 79);
			country_list.Add("Grenada", 28);
			country_list.Add("Guadeloupe", 29);
			country_list.Add("Guatemala", 30);
			country_list.Add("Guernsey", 124);
			country_list.Add("Guyana", 31);
			country_list.Add("Haiti", 32);
			country_list.Add("Honduras", 33);
			country_list.Add("Hong Kong", 144);
			country_list.Add("Hungary", 80);
			country_list.Add("Iceland", 81);
			country_list.Add("India", 169);
			country_list.Add("Ireland", 82);
			country_list.Add("Isle of Man", 125);
			country_list.Add("Italy", 83);
			country_list.Add("Jamaica", 34);
			country_list.Add("Japan", 1);
			country_list.Add("Jersey", 126);
			country_list.Add("Latvia", 84);
			country_list.Add("Lesotho", 85);
			country_list.Add("Liechtenstein", 86);
			country_list.Add("Lithuania", 87);
			country_list.Add("Luxembourg", 88);
			country_list.Add("Macedonia (Republic of)", 89);
			country_list.Add("Malaysia", 156);
			country_list.Add("Mali", 115);
			country_list.Add("Malta", 90);
			country_list.Add("Martinique", 35);
			country_list.Add("Mauritania", 114);
			country_list.Add("Mexico", 36);
			country_list.Add("Monaco", 127);
			country_list.Add("Montenegro", 91);
			country_list.Add("Montserrat", 37);
			country_list.Add("Mozambique", 92);
			country_list.Add("Namibia", 93);
			country_list.Add("Netherlands", 94);
			country_list.Add("Netherlands Antilles", 38);
			country_list.Add("New Zealand", 95);
			country_list.Add("Nicaragua", 39);
			country_list.Add("Niger", 116);
			country_list.Add("Norway", 96);
			country_list.Add("Panama", 40);
			country_list.Add("Paraguay", 41);
			country_list.Add("Peru", 42);
			country_list.Add("Poland", 97);
			country_list.Add("Portugal", 98);
			country_list.Add("Romania", 99);
			country_list.Add("Russia", 100);
			country_list.Add("San Marino", 184);
			country_list.Add("Saudi Arabia", 174);
			country_list.Add("Serbia and Kosovo", 101);
			country_list.Add("Singapore", 153);
			country_list.Add("Slovakia", 102);
			country_list.Add("Slovenia", 103);
			country_list.Add("Somalia", 121);
			country_list.Add("South Africa", 104);
			country_list.Add("South Korea", 136);
			country_list.Add("Spain", 105);
			country_list.Add("St. Kitts and Nevis", 43);
			country_list.Add("St. Lucia", 44);
			country_list.Add("St. Vincent and the Grenadines", 45);
			country_list.Add("Sudan", 118);
			country_list.Add("Suriname", 46);
			country_list.Add("Swaziland", 106);
			country_list.Add("Sweden", 107);
			country_list.Add("Switzerland", 108);
			country_list.Add("Taiwan", 128);
			country_list.Add("Trinidad and Tobago", 47);
			country_list.Add("Turkey", 109);
			country_list.Add("Turks and Caicos Islands", 48);
			country_list.Add("U.A.E.", 168);
			country_list.Add("United Kingdom", 110);
			country_list.Add("United States", 49);
			country_list.Add("Uruguay", 50);
			country_list.Add("US Virgin Islands", 51);
			country_list.Add("Vatican City", 185);
			country_list.Add("Venezuela", 52);
			country_list.Add("Zambia", 111);
			country_list.Add("Zimbabwe", 112);
			var reversed = country_list.ToDictionary(kp => kp.Value, kp => kp.Key);
			string result = reversed[country];
			return result;
		}

		public string getregion(int region, int type)
		{
			Dictionary<int, string> region_list = new Dictionary<int, string>();
            region_list.Add(27, "Hoenn"); //Omega Ruby
            region_list.Add(26, "Hoenn"); //Alpha Sapphire
			region_list.Add(24, "Kalos"); //X
			region_list.Add(25, "Kalos"); //Y
			region_list.Add(20, "Unova"); //White
			region_list.Add(21, "Unova"); //Black
			region_list.Add(22, "Unova"); //White 2
			region_list.Add(23, "Unova"); //Black 2
			region_list.Add(10, "Sinnoh"); //Diamond
			region_list.Add(11, "Sinnoh"); //Pearl
			region_list.Add(12, "Sinnoh"); //Platinum
			region_list.Add(7, "Johto"); //Heart Gold
			region_list.Add(8, "Johto"); //Soul Silver
			region_list.Add(2, "Hoenn"); //Ruby
			region_list.Add(1, "Hoenn"); //Sapphire
			region_list.Add(3, "Hoenn"); //Emerald
			region_list.Add(4, "Kanto"); //Fire Red
			region_list.Add(5, "Kanto"); //Leaf Green
			region_list.Add(15, "Orre"); //Colosseum/XD

			Dictionary<int, string> game_list = new Dictionary<int, string>();
            game_list.Add(27, "Omega Ruby"); //Hoenn
            game_list.Add(26, "Alpha Sapphire"); //Hoenn
			game_list.Add(24, "X"); //Kalos
			game_list.Add(25, "Y"); //Kalos
			game_list.Add(20, "White"); //Unova
			game_list.Add(21, "Black"); //Unova
			game_list.Add(22, "White 2"); //Unova
			game_list.Add(23, "Black 2"); //Unova
			game_list.Add(10, "Diamond"); //Sinnoh
			game_list.Add(11, "Pearl"); //Sinnoh
			game_list.Add(12, "Platinum"); //Sinnoh
			game_list.Add(7, "Heart Gold"); //Johto
			game_list.Add(8, "Soul Silver"); //Johto
			game_list.Add(2, "Ruby"); //Hoenn
			game_list.Add(1, "Sapphire"); //Hoenn
			game_list.Add(3, "Emerald"); //Hoenn
			game_list.Add(4, "Fire Red"); //Kanto
			game_list.Add(5, "Leaf Green"); //Kanto
			game_list.Add(15, "Colosseum/XD"); //Orre

			string result;
			if (type == 0) result = region_list[region];
			else result = game_list[region];

			return result;
		}

		public string getlanguage(int language)
		{
			Dictionary<int, string> language_list = new Dictionary<int, string>();
			language_list.Add(0, "ERROR"); //Corrupt?
			language_list.Add(2, "ENG"); //English
			language_list.Add(1, "JPN"); //日本語
			language_list.Add(3, "FRE"); //Français
			language_list.Add(4, "ITA"); //Italiano
			language_list.Add(5, "GER"); //Deutsch
			language_list.Add(7, "ESP"); //Español
			language_list.Add(8, "KOR"); //한국어

			string result = language_list[language];
			return result;
		}

        // Structs
        public class Structures
        {
            public struct PKX
            {
                public uint EC, PID, IV32,

                    exp,
                    HP_EV, ATK_EV, DEF_EV, SPA_EV, SPD_EV, SPE_EV,
                    HP_IV, ATK_IV, DEF_IV, SPE_IV, SPA_IV, SPD_IV,
                    cnt_cool, cnt_beauty, cnt_cute, cnt_smart, cnt_tough, cnt_sheen,
                    markings, hptype;

                public string
                    nicknamestr, notOT, ot, genderstring;

                public int
                    ability, abilitynum, nature, feflag, genderflag, altforms, PKRS_Strain, PKRS_Duration,
                    metlevel, otgender;

                public bool
                    isegg, isnick, isshiny;

                public ushort
                    species, helditem, TID, SID, TSV, ESV,
                    move1, move2, move3, move4,
                    move1_pp, move2_pp, move3_pp, move4_pp,
                    move1_ppu, move2_ppu, move3_ppu, move4_ppu,
                    eggmove1, eggmove2, eggmove3, eggmove4,
                    chk,

                    OTfriendship, OTaffection,
                    egg_year, egg_month, egg_day,
                    met_year, met_month, met_day,
                    eggloc, metloc,
                    ball, encountertype,
                    gamevers, countryID, regionID, dsregID, otlang;

                public PKX(byte[] pkx)
                {
                    nicknamestr = "";
                    notOT = "";
                    ot = "";
                    EC = BitConverter.ToUInt32(pkx, 0);
                    chk = BitConverter.ToUInt16(pkx, 6);
                    species = BitConverter.ToUInt16(pkx, 0x08);
                    helditem = BitConverter.ToUInt16(pkx, 0x0A);
                    TID = BitConverter.ToUInt16(pkx, 0x0C);
                    SID = BitConverter.ToUInt16(pkx, 0x0E);
                    exp = BitConverter.ToUInt32(pkx, 0x10);
                    ability = pkx[0x14];
                    abilitynum = pkx[0x15];
                    // 0x16, 0x17 - unknown
                    PID = BitConverter.ToUInt32(pkx, 0x18);
                    nature = pkx[0x1C];
                    feflag = pkx[0x1D] % 2;
                    genderflag = (pkx[0x1D] >> 1) & 0x3;
                    altforms = (pkx[0x1D] >> 3);
                    HP_EV = pkx[0x1E];
                    ATK_EV = pkx[0x1F];
                    DEF_EV = pkx[0x20];
                    SPA_EV = pkx[0x22];
                    SPD_EV = pkx[0x23];
                    SPE_EV = pkx[0x21];
                    cnt_cool = pkx[0x24];
                    cnt_beauty = pkx[0x25];
                    cnt_cute = pkx[0x26];
                    cnt_smart = pkx[0x27];
                    cnt_tough = pkx[0x28];
                    cnt_sheen = pkx[0x29];
                    markings = pkx[0x2A];
                    PKRS_Strain = pkx[0x2B] >> 4;
                    PKRS_Duration = pkx[0x2B] % 0x10;

                    // Block B
                    nicknamestr = TrimFromZero(Encoding.Unicode.GetString(pkx, 0x40, 24));
                    // 0x58, 0x59 - unused
                    move1 = BitConverter.ToUInt16(pkx, 0x5A);
                    move2 = BitConverter.ToUInt16(pkx, 0x5C);
                    move3 = BitConverter.ToUInt16(pkx, 0x5E);
                    move4 = BitConverter.ToUInt16(pkx, 0x60);
                    move1_pp = pkx[0x62];
                    move2_pp = pkx[0x63];
                    move3_pp = pkx[0x64];
                    move4_pp = pkx[0x65];
                    move1_ppu = pkx[0x66];
                    move2_ppu = pkx[0x67];
                    move3_ppu = pkx[0x68];
                    move4_ppu = pkx[0x69];
                    eggmove1 = BitConverter.ToUInt16(pkx, 0x6A);
                    eggmove2 = BitConverter.ToUInt16(pkx, 0x6C);
                    eggmove3 = BitConverter.ToUInt16(pkx, 0x6E);
                    eggmove4 = BitConverter.ToUInt16(pkx, 0x70);

                    // 0x72 - Super Training Flag - Passed with pkx to new form

                    // 0x73 - unused/unknown
                    IV32 = BitConverter.ToUInt32(pkx, 0x74);
                    HP_IV = IV32 & 0x1F;
                    ATK_IV = (IV32 >> 5) & 0x1F;
                    DEF_IV = (IV32 >> 10) & 0x1F;
                    SPE_IV = (IV32 >> 15) & 0x1F;
                    SPA_IV = (IV32 >> 20) & 0x1F;
                    SPD_IV = (IV32 >> 25) & 0x1F;
                    isegg = Convert.ToBoolean((IV32 >> 30) & 1);
                    isnick = Convert.ToBoolean((IV32 >> 31));

                    // Block C
                    notOT = TrimFromZero(Encoding.Unicode.GetString(pkx, 0x78, 24));
                    bool notOTG = Convert.ToBoolean(pkx[0x92]);
                    // Memory Editor edits everything else with pkx in a new form

                    // Block D
                    ot = TrimFromZero(Encoding.Unicode.GetString(pkx, 0xB0, 24));
                    // 0xC8, 0xC9 - unused
                    OTfriendship = pkx[0xCA];
                    OTaffection = pkx[0xCB]; // Handled by Memory Editor
                    // 0xCC, 0xCD, 0xCE, 0xCF, 0xD0
                    egg_year = pkx[0xD1];
                    egg_month = pkx[0xD2];
                    egg_day = pkx[0xD3];
                    met_year = pkx[0xD4];
                    met_month = pkx[0xD5];
                    met_day = pkx[0xD6];
                    // 0xD7 - unused
                    eggloc = BitConverter.ToUInt16(pkx, 0xD8);
                    metloc = BitConverter.ToUInt16(pkx, 0xDA);
                    ball = pkx[0xDC];
                    metlevel = pkx[0xDD] & 0x7F;
                    otgender = (pkx[0xDD]) >> 7;
                    encountertype = pkx[0xDE];
                    gamevers = pkx[0xDF];
                    countryID = pkx[0xE0];
                    regionID = pkx[0xE1];
                    dsregID = pkx[0xE2];
                    otlang = pkx[0xE3];

                    if (genderflag == 0)
                        genderstring = "♂";
                    else if (genderflag == 1)
                        genderstring = "♀";
                    else genderstring = "-";

                    hptype = (15 * ((HP_IV & 1) + 2 * (ATK_IV & 1) + 4 * (DEF_IV & 1) + 8 * (SPE_IV & 1) + 16 * (SPA_IV & 1) + 32 * (SPD_IV & 1))) / 63 + 1;

                    TSV = (ushort)((TID ^ SID) >> 4);
                    ESV = (ushort)(((PID >> 16) ^ (PID & 0xFFFF)) >> 4);

                    isshiny = (TSV == ESV);
                }
            }
        }

        private void B_BKP_SAV_Click(object sender, EventArgs e)
        {
            TextBox tb = TB_SAV;

            FileInfo fi = new FileInfo(tb.Text);
            DateTime dt = fi.LastWriteTime;
            int year = dt.Year;
            int month = dt.Month;
            int day = dt.Day;
            int hour = dt.Hour;
            int minute = dt.Minute;
            int second = dt.Second;

            string bkpdate = year.ToString("0000") + month.ToString("00") + day.ToString("00") + hour.ToString("00") + minute.ToString("00") + second.ToString("00") + " ";
            string newpath = bakpath + "\\" + bkpdate + fi.Name;
            if (File.Exists(newpath))
            {
                DialogResult sdr = MessageBox.Show("File already exists!\r\n\r\nOverwrite?", "Prompt", MessageBoxButtons.YesNo);
                if (sdr == DialogResult.Yes)
                    File.Delete(newpath);
                else 
                    return;
            }

            File.Copy(tb.Text, newpath);
            MessageBox.Show("Copied to Backup Folder.\r\n\r\nFile named:\r\n" + newpath, "Alert");
        }
        private void B_BKP_BV_Click(object sender, EventArgs e)
        {
            TextBox tb = TB_BV;

            FileInfo fi = new FileInfo(tb.Text);
            DateTime dt = fi.LastWriteTime;
            int year = dt.Year;
            int month = dt.Month;
            int day = dt.Day;
            int hour = dt.Hour;
            int minute = dt.Minute;
            int second = dt.Second;

            string bkpdate = year.ToString("0000") + month.ToString("00") + day.ToString("00") + hour.ToString("00") + minute.ToString("00") + second.ToString("00") + " ";
            string newpath = bakpath + "\\" + bkpdate + fi.Name;
            if (File.Exists(newpath))
            {
                DialogResult sdr = MessageBox.Show("File already exists!\r\n\r\nOverwrite?", "Prompt", MessageBoxButtons.YesNo);
                if (sdr == DialogResult.Yes)
                    File.Delete(newpath);
                else 
                    return;
            }

            File.Copy(tb.Text, newpath);
            MessageBox.Show("Copied to Backup Folder.\r\n\r\nFile named:\r\n" + newpath, "Alert");
        }
        private void B_BreakFolder_Click(object sender, EventArgs e)
        {
            byte[] savefile = new byte[0x10009C];
            string savkeypath = "";
            foreach (string path in Directory.GetFiles(TB_Folder.Text))
            {
                openSAV_(path, ref savefile, ref savkeypath, false);
            }
            MessageBox.Show("Processed all files in folder...");
        }
        // Data Tables
        static DataTable SpeciesTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Species", typeof(int));
            table.Columns.Add("EXP Growth", typeof(int));
            table.Columns.Add("BST HP", typeof(int));
            table.Columns.Add("BST ATK", typeof(int));
            table.Columns.Add("BST DEF", typeof(int));
            table.Columns.Add("BST SpA", typeof(int));
            table.Columns.Add("BST SpD", typeof(int));
            table.Columns.Add("BST Spe", typeof(int));
            table.Columns.Add("GT ID", typeof(int));

            table.Rows.Add(0, 0, 0, 0, 0, 0, 0, 0, 256);
            table.Rows.Add(1, 3, 45, 49, 49, 65, 65, 45, 32);
            table.Rows.Add(2, 3, 60, 62, 63, 80, 80, 60, 32);
            table.Rows.Add(3, 3, 80, 82, 83, 100, 100, 80, 32);
            table.Rows.Add(4, 3, 39, 52, 43, 60, 50, 65, 32);
            table.Rows.Add(5, 3, 58, 64, 58, 80, 65, 80, 32);
            table.Rows.Add(6, 3, 78, 84, 78, 109, 85, 100, 32);
            table.Rows.Add(7, 3, 44, 48, 65, 50, 64, 43, 32);
            table.Rows.Add(8, 3, 59, 63, 80, 65, 80, 58, 32);
            table.Rows.Add(9, 3, 79, 83, 100, 85, 105, 78, 32);
            table.Rows.Add(10, 2, 45, 30, 35, 20, 20, 45, 128);
            table.Rows.Add(11, 2, 50, 20, 55, 25, 25, 30, 128);
            table.Rows.Add(12, 2, 60, 45, 50, 90, 80, 70, 128);
            table.Rows.Add(13, 2, 40, 35, 30, 20, 20, 50, 128);
            table.Rows.Add(14, 2, 45, 25, 50, 25, 25, 35, 128);
            table.Rows.Add(15, 2, 65, 90, 40, 45, 80, 75, 128);
            table.Rows.Add(16, 3, 40, 45, 40, 35, 35, 56, 128);
            table.Rows.Add(17, 3, 63, 60, 55, 50, 50, 71, 128);
            table.Rows.Add(18, 3, 83, 80, 75, 70, 70, 101, 128);
            table.Rows.Add(19, 2, 30, 56, 35, 25, 35, 72, 128);
            table.Rows.Add(20, 2, 55, 81, 60, 50, 70, 97, 128);
            table.Rows.Add(21, 2, 40, 60, 30, 31, 31, 70, 128);
            table.Rows.Add(22, 2, 65, 90, 65, 61, 61, 100, 128);
            table.Rows.Add(23, 2, 35, 60, 44, 40, 54, 55, 128);
            table.Rows.Add(24, 2, 60, 85, 69, 65, 79, 80, 128);
            table.Rows.Add(25, 2, 35, 55, 40, 50, 50, 90, 128);
            table.Rows.Add(26, 2, 60, 90, 55, 90, 80, 110, 128);
            table.Rows.Add(27, 2, 50, 75, 85, 20, 30, 40, 128);
            table.Rows.Add(28, 2, 75, 100, 110, 45, 55, 65, 128);
            table.Rows.Add(29, 3, 55, 47, 52, 40, 40, 41, 257);
            table.Rows.Add(30, 3, 70, 62, 67, 55, 55, 56, 257);
            table.Rows.Add(31, 3, 90, 92, 87, 75, 85, 76, 257);
            table.Rows.Add(32, 3, 46, 57, 40, 40, 40, 50, 256);
            table.Rows.Add(33, 3, 61, 72, 57, 55, 55, 65, 256);
            table.Rows.Add(34, 3, 81, 102, 77, 85, 75, 85, 256);
            table.Rows.Add(35, 1, 70, 45, 48, 60, 65, 35, 192);
            table.Rows.Add(36, 1, 95, 70, 73, 95, 90, 60, 192);
            table.Rows.Add(37, 2, 38, 41, 40, 50, 65, 65, 192);
            table.Rows.Add(38, 2, 73, 76, 75, 81, 100, 100, 192);
            table.Rows.Add(39, 1, 115, 45, 20, 45, 25, 20, 192);
            table.Rows.Add(40, 1, 140, 70, 45, 85, 50, 45, 192);
            table.Rows.Add(41, 2, 40, 45, 35, 30, 40, 55, 128);
            table.Rows.Add(42, 2, 75, 80, 70, 65, 75, 90, 128);
            table.Rows.Add(43, 3, 45, 50, 55, 75, 65, 30, 128);
            table.Rows.Add(44, 3, 60, 65, 70, 85, 75, 40, 128);
            table.Rows.Add(45, 3, 75, 80, 85, 110, 90, 50, 128);
            table.Rows.Add(46, 2, 35, 70, 55, 45, 55, 25, 128);
            table.Rows.Add(47, 2, 60, 95, 80, 60, 80, 30, 128);
            table.Rows.Add(48, 2, 60, 55, 50, 40, 55, 45, 128);
            table.Rows.Add(49, 2, 70, 65, 60, 90, 75, 90, 128);
            table.Rows.Add(50, 2, 10, 55, 25, 35, 45, 95, 128);
            table.Rows.Add(51, 2, 35, 80, 50, 50, 70, 120, 128);
            table.Rows.Add(52, 2, 40, 45, 35, 40, 40, 90, 128);
            table.Rows.Add(53, 2, 65, 70, 60, 65, 65, 115, 128);
            table.Rows.Add(54, 2, 50, 52, 48, 65, 50, 55, 128);
            table.Rows.Add(55, 2, 80, 82, 78, 95, 80, 85, 128);
            table.Rows.Add(56, 2, 40, 80, 35, 35, 45, 70, 128);
            table.Rows.Add(57, 2, 65, 105, 60, 60, 70, 95, 128);
            table.Rows.Add(58, 4, 55, 70, 45, 70, 50, 60, 64);
            table.Rows.Add(59, 4, 90, 110, 80, 100, 80, 95, 64);
            table.Rows.Add(60, 3, 40, 50, 40, 40, 40, 90, 128);
            table.Rows.Add(61, 3, 65, 65, 65, 50, 50, 90, 128);
            table.Rows.Add(62, 3, 90, 95, 95, 70, 90, 70, 128);
            table.Rows.Add(63, 3, 25, 20, 15, 105, 55, 90, 64);
            table.Rows.Add(64, 3, 40, 35, 30, 120, 70, 105, 64);
            table.Rows.Add(65, 3, 55, 50, 45, 135, 95, 120, 64);
            table.Rows.Add(66, 3, 70, 80, 50, 35, 35, 35, 64);
            table.Rows.Add(67, 3, 80, 100, 70, 50, 60, 45, 64);
            table.Rows.Add(68, 3, 90, 130, 80, 65, 85, 55, 64);
            table.Rows.Add(69, 3, 50, 75, 35, 70, 30, 40, 128);
            table.Rows.Add(70, 3, 65, 90, 50, 85, 45, 55, 128);
            table.Rows.Add(71, 3, 80, 105, 65, 100, 70, 70, 128);
            table.Rows.Add(72, 4, 40, 40, 35, 50, 100, 70, 128);
            table.Rows.Add(73, 4, 80, 70, 65, 80, 120, 100, 128);
            table.Rows.Add(74, 3, 40, 80, 100, 30, 30, 20, 128);
            table.Rows.Add(75, 3, 55, 95, 115, 45, 45, 35, 128);
            table.Rows.Add(76, 3, 80, 120, 130, 55, 65, 45, 128);
            table.Rows.Add(77, 2, 50, 85, 55, 65, 65, 90, 128);
            table.Rows.Add(78, 2, 65, 100, 70, 80, 80, 105, 128);
            table.Rows.Add(79, 2, 90, 65, 65, 40, 40, 15, 128);
            table.Rows.Add(80, 2, 95, 75, 110, 100, 80, 30, 128);
            table.Rows.Add(81, 2, 25, 35, 70, 95, 55, 45, 258);
            table.Rows.Add(82, 2, 50, 60, 95, 120, 70, 70, 258);
            table.Rows.Add(83, 2, 52, 65, 55, 58, 62, 60, 128);
            table.Rows.Add(84, 2, 35, 85, 45, 35, 35, 75, 128);
            table.Rows.Add(85, 2, 60, 110, 70, 60, 60, 100, 128);
            table.Rows.Add(86, 2, 65, 45, 55, 45, 70, 45, 128);
            table.Rows.Add(87, 2, 90, 70, 80, 70, 95, 70, 128);
            table.Rows.Add(88, 2, 80, 80, 50, 40, 50, 25, 128);
            table.Rows.Add(89, 2, 105, 105, 75, 65, 100, 50, 128);
            table.Rows.Add(90, 4, 30, 65, 100, 45, 25, 40, 128);
            table.Rows.Add(91, 4, 50, 95, 180, 85, 45, 70, 128);
            table.Rows.Add(92, 3, 30, 35, 30, 100, 35, 80, 128);
            table.Rows.Add(93, 3, 45, 50, 45, 115, 55, 95, 128);
            table.Rows.Add(94, 3, 60, 65, 60, 130, 75, 110, 128);
            table.Rows.Add(95, 2, 35, 45, 160, 30, 45, 70, 128);
            table.Rows.Add(96, 2, 60, 48, 45, 43, 90, 42, 128);
            table.Rows.Add(97, 2, 85, 73, 70, 73, 115, 67, 128);
            table.Rows.Add(98, 2, 30, 105, 90, 25, 25, 50, 128);
            table.Rows.Add(99, 2, 55, 130, 115, 50, 50, 75, 128);
            table.Rows.Add(100, 2, 40, 30, 50, 55, 55, 100, 258);
            table.Rows.Add(101, 2, 60, 50, 70, 80, 80, 140, 258);
            table.Rows.Add(102, 4, 60, 40, 80, 60, 45, 40, 128);
            table.Rows.Add(103, 4, 95, 95, 85, 125, 65, 55, 128);
            table.Rows.Add(104, 2, 50, 50, 95, 40, 50, 35, 128);
            table.Rows.Add(105, 2, 60, 80, 110, 50, 80, 45, 128);
            table.Rows.Add(106, 2, 50, 120, 53, 35, 110, 87, 256);
            table.Rows.Add(107, 2, 50, 105, 79, 35, 110, 76, 256);
            table.Rows.Add(108, 2, 90, 55, 75, 60, 75, 30, 128);
            table.Rows.Add(109, 2, 40, 65, 95, 60, 45, 35, 128);
            table.Rows.Add(110, 2, 65, 90, 120, 85, 70, 60, 128);
            table.Rows.Add(111, 4, 80, 85, 95, 30, 30, 25, 128);
            table.Rows.Add(112, 4, 105, 130, 120, 45, 45, 40, 128);
            table.Rows.Add(113, 1, 250, 5, 5, 35, 105, 50, 257);
            table.Rows.Add(114, 2, 65, 55, 115, 100, 40, 60, 128);
            table.Rows.Add(115, 2, 105, 95, 80, 40, 80, 90, 257);
            table.Rows.Add(116, 2, 30, 40, 70, 70, 25, 60, 128);
            table.Rows.Add(117, 2, 55, 65, 95, 95, 45, 85, 128);
            table.Rows.Add(118, 2, 45, 67, 60, 35, 50, 63, 128);
            table.Rows.Add(119, 2, 80, 92, 65, 65, 80, 68, 128);
            table.Rows.Add(120, 4, 30, 45, 55, 70, 55, 85, 258);
            table.Rows.Add(121, 4, 60, 75, 85, 100, 85, 115, 258);
            table.Rows.Add(122, 2, 40, 45, 65, 100, 120, 90, 128);
            table.Rows.Add(123, 2, 70, 110, 80, 55, 80, 105, 128);
            table.Rows.Add(124, 2, 65, 50, 35, 115, 95, 95, 257);
            table.Rows.Add(125, 2, 65, 83, 57, 95, 85, 105, 64);
            table.Rows.Add(126, 2, 65, 95, 57, 100, 85, 93, 64);
            table.Rows.Add(127, 4, 65, 125, 100, 55, 70, 85, 128);
            table.Rows.Add(128, 4, 75, 100, 95, 40, 70, 110, 256);
            table.Rows.Add(129, 4, 20, 10, 55, 15, 20, 80, 128);
            table.Rows.Add(130, 4, 95, 125, 79, 60, 100, 81, 128);
            table.Rows.Add(131, 4, 130, 85, 80, 85, 95, 60, 128);
            table.Rows.Add(132, 2, 48, 48, 48, 48, 48, 48, 258);
            table.Rows.Add(133, 2, 55, 55, 50, 45, 65, 55, 32);
            table.Rows.Add(134, 2, 130, 65, 60, 110, 95, 65, 32);
            table.Rows.Add(135, 2, 65, 65, 60, 110, 95, 130, 32);
            table.Rows.Add(136, 2, 65, 130, 60, 95, 110, 65, 32);
            table.Rows.Add(137, 2, 65, 60, 70, 85, 75, 40, 258);
            table.Rows.Add(138, 2, 35, 40, 100, 90, 55, 35, 32);
            table.Rows.Add(139, 2, 70, 60, 125, 115, 70, 55, 32);
            table.Rows.Add(140, 2, 30, 80, 90, 55, 45, 55, 32);
            table.Rows.Add(141, 2, 60, 115, 105, 65, 70, 80, 32);
            table.Rows.Add(142, 4, 80, 105, 65, 60, 75, 130, 32);
            table.Rows.Add(143, 4, 160, 110, 65, 65, 110, 30, 32);
            table.Rows.Add(144, 4, 90, 85, 100, 95, 125, 85, 258);
            table.Rows.Add(145, 4, 90, 90, 85, 125, 90, 100, 258);
            table.Rows.Add(146, 4, 90, 100, 90, 125, 85, 90, 258);
            table.Rows.Add(147, 4, 41, 64, 45, 50, 50, 50, 128);
            table.Rows.Add(148, 4, 61, 84, 65, 70, 70, 70, 128);
            table.Rows.Add(149, 4, 91, 134, 95, 100, 100, 80, 128);
            table.Rows.Add(150, 4, 106, 110, 90, 154, 90, 130, 258);
            table.Rows.Add(151, 3, 100, 100, 100, 100, 100, 100, 258);
            table.Rows.Add(152, 3, 45, 49, 65, 49, 65, 45, 32);
            table.Rows.Add(153, 3, 60, 62, 80, 63, 80, 60, 32);
            table.Rows.Add(154, 3, 80, 82, 100, 83, 100, 80, 32);
            table.Rows.Add(155, 3, 39, 52, 43, 60, 50, 65, 32);
            table.Rows.Add(156, 3, 58, 64, 58, 80, 65, 80, 32);
            table.Rows.Add(157, 3, 78, 84, 78, 109, 85, 100, 32);
            table.Rows.Add(158, 3, 50, 65, 64, 44, 48, 43, 32);
            table.Rows.Add(159, 3, 65, 80, 80, 59, 63, 58, 32);
            table.Rows.Add(160, 3, 85, 105, 100, 79, 83, 78, 32);
            table.Rows.Add(161, 2, 35, 46, 34, 35, 45, 20, 128);
            table.Rows.Add(162, 2, 85, 76, 64, 45, 55, 90, 128);
            table.Rows.Add(163, 2, 60, 30, 30, 36, 56, 50, 128);
            table.Rows.Add(164, 2, 100, 50, 50, 76, 96, 70, 128);
            table.Rows.Add(165, 1, 40, 20, 30, 40, 80, 55, 128);
            table.Rows.Add(166, 1, 55, 35, 50, 55, 110, 85, 128);
            table.Rows.Add(167, 1, 40, 60, 40, 40, 40, 30, 128);
            table.Rows.Add(168, 1, 70, 90, 70, 60, 60, 40, 128);
            table.Rows.Add(169, 2, 85, 90, 80, 70, 80, 130, 128);
            table.Rows.Add(170, 4, 75, 38, 38, 56, 56, 67, 128);
            table.Rows.Add(171, 4, 125, 58, 58, 76, 76, 67, 128);
            table.Rows.Add(172, 2, 20, 40, 15, 35, 35, 60, 128);
            table.Rows.Add(173, 1, 50, 25, 28, 45, 55, 15, 192);
            table.Rows.Add(174, 1, 90, 30, 15, 40, 20, 15, 192);
            table.Rows.Add(175, 1, 35, 20, 65, 40, 65, 20, 32);
            table.Rows.Add(176, 1, 55, 40, 85, 80, 105, 40, 32);
            table.Rows.Add(177, 2, 40, 50, 45, 70, 45, 70, 128);
            table.Rows.Add(178, 2, 65, 75, 70, 95, 70, 95, 128);
            table.Rows.Add(179, 3, 55, 40, 40, 65, 45, 35, 128);
            table.Rows.Add(180, 3, 70, 55, 55, 80, 60, 45, 128);
            table.Rows.Add(181, 3, 90, 75, 85, 115, 90, 55, 128);
            table.Rows.Add(182, 3, 75, 80, 95, 90, 100, 50, 128);
            table.Rows.Add(183, 1, 70, 20, 50, 20, 50, 40, 128);
            table.Rows.Add(184, 1, 100, 50, 80, 60, 80, 50, 128);
            table.Rows.Add(185, 2, 70, 100, 115, 30, 65, 30, 128);
            table.Rows.Add(186, 3, 90, 75, 75, 90, 100, 70, 128);
            table.Rows.Add(187, 3, 35, 35, 40, 35, 55, 50, 128);
            table.Rows.Add(188, 3, 55, 45, 50, 45, 65, 80, 128);
            table.Rows.Add(189, 3, 75, 55, 70, 55, 95, 110, 128);
            table.Rows.Add(190, 1, 55, 70, 55, 40, 55, 85, 128);
            table.Rows.Add(191, 3, 30, 30, 30, 30, 30, 30, 128);
            table.Rows.Add(192, 3, 75, 75, 55, 105, 85, 30, 128);
            table.Rows.Add(193, 2, 65, 65, 45, 75, 45, 95, 128);
            table.Rows.Add(194, 2, 55, 45, 45, 25, 25, 15, 128);
            table.Rows.Add(195, 2, 95, 85, 85, 65, 65, 35, 128);
            table.Rows.Add(196, 2, 65, 65, 60, 130, 95, 110, 32);
            table.Rows.Add(197, 2, 95, 65, 110, 60, 130, 65, 32);
            table.Rows.Add(198, 3, 60, 85, 42, 85, 42, 91, 128);
            table.Rows.Add(199, 2, 95, 75, 80, 100, 110, 30, 128);
            table.Rows.Add(200, 1, 60, 60, 60, 85, 85, 85, 128);
            table.Rows.Add(201, 2, 48, 72, 48, 72, 48, 48, 258);
            table.Rows.Add(202, 2, 190, 33, 58, 33, 58, 33, 128);
            table.Rows.Add(203, 2, 70, 80, 65, 90, 65, 85, 128);
            table.Rows.Add(204, 2, 50, 65, 90, 35, 35, 15, 128);
            table.Rows.Add(205, 2, 75, 90, 140, 60, 60, 40, 128);
            table.Rows.Add(206, 2, 100, 70, 70, 65, 65, 45, 128);
            table.Rows.Add(207, 3, 65, 75, 105, 35, 65, 85, 128);
            table.Rows.Add(208, 2, 75, 85, 200, 55, 65, 30, 128);
            table.Rows.Add(209, 1, 60, 80, 50, 40, 40, 30, 192);
            table.Rows.Add(210, 1, 90, 120, 75, 60, 60, 45, 192);
            table.Rows.Add(211, 2, 65, 95, 75, 55, 55, 85, 128);
            table.Rows.Add(212, 2, 70, 130, 100, 55, 80, 65, 128);
            table.Rows.Add(213, 3, 20, 10, 230, 10, 230, 5, 128);
            table.Rows.Add(214, 4, 80, 125, 75, 40, 95, 85, 128);
            table.Rows.Add(215, 3, 55, 95, 55, 35, 75, 115, 128);
            table.Rows.Add(216, 2, 60, 80, 50, 50, 50, 40, 128);
            table.Rows.Add(217, 2, 90, 130, 75, 75, 75, 55, 128);
            table.Rows.Add(218, 2, 40, 40, 40, 70, 40, 20, 128);
            table.Rows.Add(219, 2, 50, 50, 120, 80, 80, 30, 128);
            table.Rows.Add(220, 4, 50, 50, 40, 30, 30, 50, 128);
            table.Rows.Add(221, 4, 100, 100, 80, 60, 60, 50, 128);
            table.Rows.Add(222, 1, 55, 55, 85, 65, 85, 35, 192);
            table.Rows.Add(223, 2, 35, 65, 35, 65, 35, 65, 128);
            table.Rows.Add(224, 2, 75, 105, 75, 105, 75, 45, 128);
            table.Rows.Add(225, 1, 45, 55, 45, 65, 45, 75, 128);
            table.Rows.Add(226, 4, 65, 40, 70, 80, 140, 70, 128);
            table.Rows.Add(227, 4, 65, 80, 140, 40, 70, 70, 128);
            table.Rows.Add(228, 4, 45, 60, 30, 80, 50, 65, 128);
            table.Rows.Add(229, 4, 75, 90, 50, 110, 80, 95, 128);
            table.Rows.Add(230, 2, 75, 95, 95, 95, 95, 85, 128);
            table.Rows.Add(231, 2, 90, 60, 60, 40, 40, 40, 128);
            table.Rows.Add(232, 2, 90, 120, 120, 60, 60, 50, 128);
            table.Rows.Add(233, 2, 85, 80, 90, 105, 95, 60, 258);
            table.Rows.Add(234, 4, 73, 95, 62, 85, 65, 85, 128);
            table.Rows.Add(235, 1, 55, 20, 35, 20, 45, 75, 128);
            table.Rows.Add(236, 2, 35, 35, 35, 35, 35, 35, 256);
            table.Rows.Add(237, 2, 50, 95, 95, 35, 110, 70, 256);
            table.Rows.Add(238, 2, 45, 30, 15, 85, 65, 65, 257);
            table.Rows.Add(239, 2, 45, 63, 37, 65, 55, 95, 64);
            table.Rows.Add(240, 2, 45, 75, 37, 70, 55, 83, 64);
            table.Rows.Add(241, 4, 95, 80, 105, 40, 70, 100, 257);
            table.Rows.Add(242, 1, 255, 10, 10, 75, 135, 55, 257);
            table.Rows.Add(243, 4, 90, 85, 75, 115, 100, 115, 258);
            table.Rows.Add(244, 4, 115, 115, 85, 90, 75, 100, 258);
            table.Rows.Add(245, 4, 100, 75, 115, 90, 115, 85, 258);
            table.Rows.Add(246, 4, 50, 64, 50, 45, 50, 41, 128);
            table.Rows.Add(247, 4, 70, 84, 70, 65, 70, 51, 128);
            table.Rows.Add(248, 4, 100, 134, 110, 95, 100, 61, 128);
            table.Rows.Add(249, 4, 106, 90, 130, 90, 154, 110, 258);
            table.Rows.Add(250, 4, 106, 130, 90, 110, 154, 90, 258);
            table.Rows.Add(251, 3, 100, 100, 100, 100, 100, 100, 258);
            table.Rows.Add(252, 3, 40, 45, 35, 65, 55, 70, 32);
            table.Rows.Add(253, 3, 50, 65, 45, 85, 65, 95, 32);
            table.Rows.Add(254, 3, 70, 85, 65, 105, 85, 120, 32);
            table.Rows.Add(255, 3, 45, 60, 40, 70, 50, 45, 32);
            table.Rows.Add(256, 3, 60, 85, 60, 85, 60, 55, 32);
            table.Rows.Add(257, 3, 80, 120, 70, 110, 70, 80, 32);
            table.Rows.Add(258, 3, 50, 70, 50, 50, 50, 40, 32);
            table.Rows.Add(259, 3, 70, 85, 70, 60, 70, 50, 32);
            table.Rows.Add(260, 3, 100, 110, 90, 85, 90, 60, 32);
            table.Rows.Add(261, 2, 35, 55, 35, 30, 30, 35, 128);
            table.Rows.Add(262, 2, 70, 90, 70, 60, 60, 70, 128);
            table.Rows.Add(263, 2, 38, 30, 41, 30, 41, 60, 128);
            table.Rows.Add(264, 2, 78, 70, 61, 50, 61, 100, 128);
            table.Rows.Add(265, 2, 45, 45, 35, 20, 30, 20, 128);
            table.Rows.Add(266, 2, 50, 35, 55, 25, 25, 15, 128);
            table.Rows.Add(267, 2, 60, 70, 50, 100, 50, 65, 128);
            table.Rows.Add(268, 2, 50, 35, 55, 25, 25, 15, 128);
            table.Rows.Add(269, 2, 60, 50, 70, 50, 90, 65, 128);
            table.Rows.Add(270, 3, 40, 30, 30, 40, 50, 30, 128);
            table.Rows.Add(271, 3, 60, 50, 50, 60, 70, 50, 128);
            table.Rows.Add(272, 3, 80, 70, 70, 90, 100, 70, 128);
            table.Rows.Add(273, 3, 40, 40, 50, 30, 30, 30, 128);
            table.Rows.Add(274, 3, 70, 70, 40, 60, 40, 60, 128);
            table.Rows.Add(275, 3, 90, 100, 60, 90, 60, 80, 128);
            table.Rows.Add(276, 3, 40, 55, 30, 30, 30, 85, 128);
            table.Rows.Add(277, 3, 60, 85, 60, 50, 50, 125, 128);
            table.Rows.Add(278, 2, 40, 30, 30, 55, 30, 85, 128);
            table.Rows.Add(279, 2, 60, 50, 100, 85, 70, 65, 128);
            table.Rows.Add(280, 4, 28, 25, 25, 45, 35, 40, 128);
            table.Rows.Add(281, 4, 38, 35, 35, 65, 55, 50, 128);
            table.Rows.Add(282, 4, 68, 65, 65, 125, 115, 80, 128);
            table.Rows.Add(283, 2, 40, 30, 32, 50, 52, 65, 128);
            table.Rows.Add(284, 2, 70, 60, 62, 80, 82, 60, 128);
            table.Rows.Add(285, 5, 60, 40, 60, 40, 60, 35, 128);
            table.Rows.Add(286, 5, 60, 130, 80, 60, 60, 70, 128);
            table.Rows.Add(287, 4, 60, 60, 60, 35, 35, 30, 128);
            table.Rows.Add(288, 4, 80, 80, 80, 55, 55, 90, 128);
            table.Rows.Add(289, 4, 150, 160, 100, 95, 65, 100, 128);
            table.Rows.Add(290, 0, 31, 45, 90, 30, 30, 40, 128);
            table.Rows.Add(291, 0, 61, 90, 45, 50, 50, 160, 128);
            table.Rows.Add(292, 0, 1, 90, 45, 30, 30, 40, 258);
            table.Rows.Add(293, 3, 64, 51, 23, 51, 23, 28, 128);
            table.Rows.Add(294, 3, 84, 71, 43, 71, 43, 48, 128);
            table.Rows.Add(295, 3, 104, 91, 63, 91, 73, 68, 128);
            table.Rows.Add(296, 5, 72, 60, 30, 20, 30, 25, 64);
            table.Rows.Add(297, 5, 144, 120, 60, 40, 60, 50, 64);
            table.Rows.Add(298, 1, 50, 20, 40, 20, 40, 20, 192);
            table.Rows.Add(299, 2, 30, 45, 135, 45, 90, 30, 128);
            table.Rows.Add(300, 1, 50, 45, 45, 35, 35, 50, 192);
            table.Rows.Add(301, 1, 70, 65, 65, 55, 55, 70, 192);
            table.Rows.Add(302, 3, 50, 75, 75, 65, 65, 50, 128);
            table.Rows.Add(303, 1, 50, 85, 85, 55, 55, 50, 128);
            table.Rows.Add(304, 4, 50, 70, 100, 40, 40, 30, 128);
            table.Rows.Add(305, 4, 60, 90, 140, 50, 50, 40, 128);
            table.Rows.Add(306, 4, 70, 110, 180, 60, 60, 50, 128);
            table.Rows.Add(307, 2, 30, 40, 55, 40, 55, 60, 128);
            table.Rows.Add(308, 2, 60, 60, 75, 60, 75, 80, 128);
            table.Rows.Add(309, 4, 40, 45, 40, 65, 40, 65, 128);
            table.Rows.Add(310, 4, 70, 75, 60, 105, 60, 105, 128);
            table.Rows.Add(311, 2, 60, 50, 40, 85, 75, 95, 128);
            table.Rows.Add(312, 2, 60, 40, 50, 75, 85, 95, 128);
            table.Rows.Add(313, 0, 65, 73, 55, 47, 75, 85, 256);
            table.Rows.Add(314, 5, 65, 47, 55, 73, 75, 85, 257);
            table.Rows.Add(315, 3, 50, 60, 45, 100, 80, 65, 128);
            table.Rows.Add(316, 5, 70, 43, 53, 43, 53, 40, 128);
            table.Rows.Add(317, 5, 100, 73, 83, 73, 83, 55, 128);
            table.Rows.Add(318, 4, 45, 90, 20, 65, 20, 65, 128);
            table.Rows.Add(319, 4, 70, 120, 40, 95, 40, 95, 128);
            table.Rows.Add(320, 5, 130, 70, 35, 70, 35, 60, 128);
            table.Rows.Add(321, 5, 170, 90, 45, 90, 45, 60, 128);
            table.Rows.Add(322, 2, 60, 60, 40, 65, 45, 35, 128);
            table.Rows.Add(323, 2, 70, 100, 70, 105, 75, 40, 128);
            table.Rows.Add(324, 2, 70, 85, 140, 85, 70, 20, 128);
            table.Rows.Add(325, 1, 60, 25, 35, 70, 80, 60, 128);
            table.Rows.Add(326, 1, 80, 45, 65, 90, 110, 80, 128);
            table.Rows.Add(327, 1, 60, 60, 60, 60, 60, 60, 128);
            table.Rows.Add(328, 3, 45, 100, 45, 45, 45, 10, 128);
            table.Rows.Add(329, 3, 50, 70, 50, 50, 50, 70, 128);
            table.Rows.Add(330, 3, 80, 100, 80, 80, 80, 100, 128);
            table.Rows.Add(331, 3, 50, 85, 40, 85, 40, 35, 128);
            table.Rows.Add(332, 3, 70, 115, 60, 115, 60, 55, 128);
            table.Rows.Add(333, 0, 45, 40, 60, 40, 75, 50, 128);
            table.Rows.Add(334, 0, 75, 70, 90, 70, 105, 80, 128);
            table.Rows.Add(335, 0, 73, 115, 60, 60, 60, 90, 128);
            table.Rows.Add(336, 5, 73, 100, 60, 100, 60, 65, 128);
            table.Rows.Add(337, 1, 70, 55, 65, 95, 85, 70, 258);
            table.Rows.Add(338, 1, 70, 95, 85, 55, 65, 70, 258);
            table.Rows.Add(339, 2, 50, 48, 43, 46, 41, 60, 128);
            table.Rows.Add(340, 2, 110, 78, 73, 76, 71, 60, 128);
            table.Rows.Add(341, 5, 43, 80, 65, 50, 35, 35, 128);
            table.Rows.Add(342, 5, 63, 120, 85, 90, 55, 55, 128);
            table.Rows.Add(343, 2, 40, 40, 55, 40, 70, 55, 258);
            table.Rows.Add(344, 2, 60, 70, 105, 70, 120, 75, 258);
            table.Rows.Add(345, 0, 66, 41, 77, 61, 87, 23, 32);
            table.Rows.Add(346, 0, 86, 81, 97, 81, 107, 43, 32);
            table.Rows.Add(347, 0, 45, 95, 50, 40, 50, 75, 32);
            table.Rows.Add(348, 0, 75, 125, 100, 70, 80, 45, 32);
            table.Rows.Add(349, 0, 20, 15, 20, 10, 55, 80, 128);
            table.Rows.Add(350, 0, 95, 60, 79, 100, 125, 81, 128);
            table.Rows.Add(351, 2, 70, 70, 70, 70, 70, 70, 128);
            table.Rows.Add(352, 3, 60, 90, 70, 60, 120, 40, 128);
            table.Rows.Add(353, 1, 44, 75, 35, 63, 33, 45, 128);
            table.Rows.Add(354, 1, 64, 115, 65, 83, 63, 65, 128);
            table.Rows.Add(355, 1, 20, 40, 90, 30, 90, 25, 128);
            table.Rows.Add(356, 1, 40, 70, 130, 60, 130, 25, 128);
            table.Rows.Add(357, 4, 99, 68, 83, 72, 87, 51, 128);
            table.Rows.Add(358, 1, 65, 50, 70, 95, 80, 65, 128);
            table.Rows.Add(359, 3, 65, 130, 60, 75, 60, 75, 128);
            table.Rows.Add(360, 2, 95, 23, 48, 23, 48, 23, 128);
            table.Rows.Add(361, 2, 50, 50, 50, 50, 50, 50, 128);
            table.Rows.Add(362, 2, 80, 80, 80, 80, 80, 80, 128);
            table.Rows.Add(363, 3, 70, 40, 50, 55, 50, 25, 128);
            table.Rows.Add(364, 3, 90, 60, 70, 75, 70, 45, 128);
            table.Rows.Add(365, 3, 110, 80, 90, 95, 90, 65, 128);
            table.Rows.Add(366, 0, 35, 64, 85, 74, 55, 32, 128);
            table.Rows.Add(367, 0, 55, 104, 105, 94, 75, 52, 128);
            table.Rows.Add(368, 0, 55, 84, 105, 114, 75, 52, 128);
            table.Rows.Add(369, 4, 100, 90, 130, 45, 65, 55, 32);
            table.Rows.Add(370, 1, 43, 30, 55, 40, 65, 97, 192);
            table.Rows.Add(371, 4, 45, 75, 60, 40, 30, 50, 128);
            table.Rows.Add(372, 4, 65, 95, 100, 60, 50, 50, 128);
            table.Rows.Add(373, 4, 95, 135, 80, 110, 80, 100, 128);
            table.Rows.Add(374, 4, 40, 55, 80, 35, 60, 30, 258);
            table.Rows.Add(375, 4, 60, 75, 100, 55, 80, 50, 258);
            table.Rows.Add(376, 4, 80, 135, 130, 95, 90, 70, 258);
            table.Rows.Add(377, 4, 80, 100, 200, 50, 100, 50, 258);
            table.Rows.Add(378, 4, 80, 50, 100, 100, 200, 50, 258);
            table.Rows.Add(379, 4, 80, 75, 150, 75, 150, 50, 258);
            table.Rows.Add(380, 4, 80, 80, 90, 110, 130, 110, 257);
            table.Rows.Add(381, 4, 80, 90, 80, 130, 110, 110, 256);
            table.Rows.Add(382, 4, 100, 100, 90, 150, 140, 90, 258);
            table.Rows.Add(383, 4, 100, 150, 140, 100, 90, 90, 258);
            table.Rows.Add(384, 4, 105, 150, 90, 150, 90, 95, 258);
            table.Rows.Add(385, 4, 100, 100, 100, 100, 100, 100, 258);
            table.Rows.Add(386, 4, 50, 180, 20, 180, 20, 150, 258);
            table.Rows.Add(387, 3, 55, 68, 64, 45, 55, 31, 32);
            table.Rows.Add(388, 3, 75, 89, 85, 55, 65, 36, 32);
            table.Rows.Add(389, 3, 95, 109, 105, 75, 85, 56, 32);
            table.Rows.Add(390, 3, 44, 58, 44, 58, 44, 61, 32);
            table.Rows.Add(391, 3, 64, 78, 52, 78, 52, 81, 32);
            table.Rows.Add(392, 3, 76, 104, 71, 104, 71, 108, 32);
            table.Rows.Add(393, 3, 53, 51, 53, 61, 56, 40, 32);
            table.Rows.Add(394, 3, 64, 66, 68, 81, 76, 50, 32);
            table.Rows.Add(395, 3, 84, 86, 88, 111, 101, 60, 32);
            table.Rows.Add(396, 3, 40, 55, 30, 30, 30, 60, 128);
            table.Rows.Add(397, 3, 55, 75, 50, 40, 40, 80, 128);
            table.Rows.Add(398, 3, 85, 120, 70, 50, 60, 100, 128);
            table.Rows.Add(399, 2, 59, 45, 40, 35, 40, 31, 128);
            table.Rows.Add(400, 2, 79, 85, 60, 55, 60, 71, 128);
            table.Rows.Add(401, 3, 37, 25, 41, 25, 41, 25, 128);
            table.Rows.Add(402, 3, 77, 85, 51, 55, 51, 65, 128);
            table.Rows.Add(403, 3, 45, 65, 34, 40, 34, 45, 128);
            table.Rows.Add(404, 3, 60, 85, 49, 60, 49, 60, 128);
            table.Rows.Add(405, 3, 80, 120, 79, 95, 79, 70, 128);
            table.Rows.Add(406, 3, 40, 30, 35, 50, 70, 55, 128);
            table.Rows.Add(407, 3, 60, 70, 65, 125, 105, 90, 128);
            table.Rows.Add(408, 0, 67, 125, 40, 30, 30, 58, 32);
            table.Rows.Add(409, 0, 97, 165, 60, 65, 50, 58, 32);
            table.Rows.Add(410, 0, 30, 42, 118, 42, 88, 30, 32);
            table.Rows.Add(411, 0, 60, 52, 168, 47, 138, 30, 32);
            table.Rows.Add(412, 2, 40, 29, 45, 29, 45, 36, 128);
            table.Rows.Add(413, 2, 60, 59, 85, 79, 105, 36, 257);
            table.Rows.Add(414, 2, 70, 94, 50, 94, 50, 66, 256);
            table.Rows.Add(415, 3, 30, 30, 42, 30, 42, 70, 32);
            table.Rows.Add(416, 3, 70, 80, 102, 80, 102, 40, 257);
            table.Rows.Add(417, 2, 60, 45, 70, 45, 90, 95, 128);
            table.Rows.Add(418, 2, 55, 65, 35, 60, 30, 85, 128);
            table.Rows.Add(419, 2, 85, 105, 55, 85, 50, 115, 128);
            table.Rows.Add(420, 2, 45, 35, 45, 62, 53, 35, 128);
            table.Rows.Add(421, 2, 70, 60, 70, 87, 78, 85, 128);
            table.Rows.Add(422, 2, 76, 48, 48, 57, 62, 34, 128);
            table.Rows.Add(423, 2, 111, 83, 68, 92, 82, 39, 128);
            table.Rows.Add(424, 1, 75, 100, 66, 60, 66, 115, 128);
            table.Rows.Add(425, 5, 90, 50, 34, 60, 44, 70, 128);
            table.Rows.Add(426, 5, 150, 80, 44, 90, 54, 80, 128);
            table.Rows.Add(427, 2, 55, 66, 44, 44, 56, 85, 128);
            table.Rows.Add(428, 2, 65, 76, 84, 54, 96, 105, 128);
            table.Rows.Add(429, 1, 60, 60, 60, 105, 105, 105, 128);
            table.Rows.Add(430, 3, 100, 125, 52, 105, 52, 71, 128);
            table.Rows.Add(431, 1, 49, 55, 42, 42, 37, 85, 192);
            table.Rows.Add(432, 1, 71, 82, 64, 64, 59, 112, 192);
            table.Rows.Add(433, 1, 45, 30, 50, 65, 50, 45, 128);
            table.Rows.Add(434, 2, 63, 63, 47, 41, 41, 74, 128);
            table.Rows.Add(435, 2, 103, 93, 67, 71, 61, 84, 128);
            table.Rows.Add(436, 2, 57, 24, 86, 24, 86, 23, 258);
            table.Rows.Add(437, 2, 67, 89, 116, 79, 116, 33, 258);
            table.Rows.Add(438, 2, 50, 80, 95, 10, 45, 10, 128);
            table.Rows.Add(439, 2, 20, 25, 45, 70, 90, 60, 128);
            table.Rows.Add(440, 1, 100, 5, 5, 15, 65, 30, 257);
            table.Rows.Add(441, 3, 76, 65, 45, 92, 42, 91, 128);
            table.Rows.Add(442, 2, 50, 92, 108, 92, 108, 35, 128);
            table.Rows.Add(443, 4, 58, 70, 45, 40, 45, 42, 128);
            table.Rows.Add(444, 4, 68, 90, 65, 50, 55, 82, 128);
            table.Rows.Add(445, 4, 108, 130, 95, 80, 85, 102, 128);
            table.Rows.Add(446, 4, 135, 85, 40, 40, 85, 5, 32);
            table.Rows.Add(447, 3, 40, 70, 40, 35, 40, 60, 32);
            table.Rows.Add(448, 3, 70, 110, 70, 115, 70, 90, 32);
            table.Rows.Add(449, 4, 68, 72, 78, 38, 42, 32, 128);
            table.Rows.Add(450, 4, 108, 112, 118, 68, 72, 47, 128);
            table.Rows.Add(451, 4, 40, 50, 90, 30, 55, 65, 128);
            table.Rows.Add(452, 4, 70, 90, 110, 60, 75, 95, 128);
            table.Rows.Add(453, 2, 48, 61, 40, 61, 40, 50, 128);
            table.Rows.Add(454, 2, 83, 106, 65, 86, 65, 85, 128);
            table.Rows.Add(455, 4, 74, 100, 72, 90, 72, 46, 128);
            table.Rows.Add(456, 0, 49, 49, 56, 49, 61, 66, 128);
            table.Rows.Add(457, 0, 69, 69, 76, 69, 86, 91, 128);
            table.Rows.Add(458, 4, 45, 20, 50, 60, 120, 50, 128);
            table.Rows.Add(459, 4, 60, 62, 50, 62, 60, 40, 128);
            table.Rows.Add(460, 4, 90, 92, 75, 92, 85, 60, 128);
            table.Rows.Add(461, 3, 70, 120, 65, 45, 85, 125, 128);
            table.Rows.Add(462, 2, 70, 70, 115, 130, 90, 60, 258);
            table.Rows.Add(463, 2, 110, 85, 95, 80, 95, 50, 128);
            table.Rows.Add(464, 4, 115, 140, 130, 55, 55, 40, 128);
            table.Rows.Add(465, 2, 100, 100, 125, 110, 50, 50, 128);
            table.Rows.Add(466, 2, 75, 123, 67, 95, 85, 95, 64);
            table.Rows.Add(467, 2, 75, 95, 67, 125, 95, 83, 64);
            table.Rows.Add(468, 1, 85, 50, 95, 120, 115, 80, 32);
            table.Rows.Add(469, 2, 86, 76, 86, 116, 56, 95, 128);
            table.Rows.Add(470, 2, 65, 110, 130, 60, 65, 95, 32);
            table.Rows.Add(471, 2, 65, 60, 110, 130, 95, 65, 32);
            table.Rows.Add(472, 3, 75, 95, 125, 45, 75, 95, 128);
            table.Rows.Add(473, 4, 110, 130, 80, 70, 60, 80, 128);
            table.Rows.Add(474, 2, 85, 80, 70, 135, 75, 90, 258);
            table.Rows.Add(475, 4, 68, 125, 65, 65, 115, 80, 256);
            table.Rows.Add(476, 2, 60, 55, 145, 75, 150, 40, 128);
            table.Rows.Add(477, 1, 45, 100, 135, 65, 135, 45, 128);
            table.Rows.Add(478, 2, 70, 80, 70, 80, 70, 110, 257);
            table.Rows.Add(479, 2, 50, 65, 107, 105, 107, 86, 258);
            table.Rows.Add(480, 4, 75, 75, 130, 75, 130, 95, 258);
            table.Rows.Add(481, 4, 80, 105, 105, 105, 105, 80, 258);
            table.Rows.Add(482, 4, 75, 125, 70, 125, 70, 115, 258);
            table.Rows.Add(483, 4, 100, 120, 120, 150, 100, 90, 258);
            table.Rows.Add(484, 4, 90, 120, 100, 150, 120, 100, 258);
            table.Rows.Add(485, 4, 91, 90, 106, 130, 106, 77, 128);
            table.Rows.Add(486, 4, 110, 160, 110, 80, 110, 100, 258);
            table.Rows.Add(487, 4, 150, 100, 120, 100, 120, 90, 258);
            table.Rows.Add(488, 4, 120, 70, 120, 75, 130, 85, 257);
            table.Rows.Add(489, 4, 80, 80, 80, 80, 80, 80, 258);
            table.Rows.Add(490, 4, 100, 100, 100, 100, 100, 100, 258);
            table.Rows.Add(491, 4, 70, 90, 90, 135, 90, 125, 258);
            table.Rows.Add(492, 3, 100, 100, 100, 100, 100, 100, 258);
            table.Rows.Add(493, 4, 120, 120, 120, 120, 120, 120, 258);
            table.Rows.Add(494, 4, 100, 100, 100, 100, 100, 100, 258);
            table.Rows.Add(495, 3, 45, 45, 55, 45, 55, 63, 32);
            table.Rows.Add(496, 3, 60, 60, 75, 60, 75, 83, 32);
            table.Rows.Add(497, 3, 75, 75, 95, 75, 95, 113, 32);
            table.Rows.Add(498, 3, 65, 63, 45, 45, 45, 45, 32);
            table.Rows.Add(499, 3, 90, 93, 55, 70, 55, 55, 32);
            table.Rows.Add(500, 3, 110, 123, 65, 100, 65, 65, 32);
            table.Rows.Add(501, 3, 55, 55, 45, 63, 45, 45, 32);
            table.Rows.Add(502, 3, 75, 75, 60, 83, 60, 60, 32);
            table.Rows.Add(503, 3, 95, 100, 85, 108, 70, 70, 32);
            table.Rows.Add(504, 2, 45, 55, 39, 35, 39, 42, 128);
            table.Rows.Add(505, 2, 60, 85, 69, 60, 69, 77, 128);
            table.Rows.Add(506, 3, 45, 60, 45, 25, 45, 55, 128);
            table.Rows.Add(507, 3, 65, 80, 65, 35, 65, 60, 128);
            table.Rows.Add(508, 3, 85, 110, 90, 45, 90, 80, 128);
            table.Rows.Add(509, 2, 41, 50, 37, 50, 37, 66, 128);
            table.Rows.Add(510, 2, 64, 88, 50, 88, 50, 106, 128);
            table.Rows.Add(511, 2, 50, 53, 48, 53, 48, 64, 32);
            table.Rows.Add(512, 2, 75, 98, 63, 98, 63, 101, 32);
            table.Rows.Add(513, 2, 50, 53, 48, 53, 48, 64, 32);
            table.Rows.Add(514, 2, 75, 98, 63, 98, 63, 101, 32);
            table.Rows.Add(515, 2, 50, 53, 48, 53, 48, 64, 32);
            table.Rows.Add(516, 2, 75, 98, 63, 98, 63, 101, 32);
            table.Rows.Add(517, 1, 76, 25, 45, 67, 55, 24, 128);
            table.Rows.Add(518, 1, 116, 55, 85, 107, 95, 29, 128);
            table.Rows.Add(519, 3, 50, 55, 50, 36, 30, 43, 128);
            table.Rows.Add(520, 3, 62, 77, 62, 50, 42, 65, 128);
            table.Rows.Add(521, 3, 80, 115, 80, 65, 55, 93, 128);
            table.Rows.Add(522, 2, 45, 60, 32, 50, 32, 76, 128);
            table.Rows.Add(523, 2, 75, 100, 63, 80, 63, 116, 128);
            table.Rows.Add(524, 3, 55, 75, 85, 25, 25, 15, 128);
            table.Rows.Add(525, 3, 70, 105, 105, 50, 40, 20, 128);
            table.Rows.Add(526, 3, 85, 135, 130, 60, 80, 25, 128);
            table.Rows.Add(527, 2, 55, 45, 43, 55, 43, 72, 128);
            table.Rows.Add(528, 2, 67, 57, 55, 77, 55, 114, 128);
            table.Rows.Add(529, 2, 60, 85, 40, 30, 45, 68, 128);
            table.Rows.Add(530, 2, 110, 135, 60, 50, 65, 88, 128);
            table.Rows.Add(531, 1, 103, 60, 86, 60, 86, 50, 128);
            table.Rows.Add(532, 3, 75, 80, 55, 25, 35, 35, 64);
            table.Rows.Add(533, 3, 85, 105, 85, 40, 50, 40, 64);
            table.Rows.Add(534, 3, 105, 140, 95, 55, 65, 45, 64);
            table.Rows.Add(535, 3, 50, 50, 40, 50, 40, 64, 128);
            table.Rows.Add(536, 3, 75, 65, 55, 65, 55, 69, 128);
            table.Rows.Add(537, 3, 105, 95, 75, 85, 75, 74, 128);
            table.Rows.Add(538, 2, 120, 100, 85, 30, 85, 45, 256);
            table.Rows.Add(539, 2, 75, 125, 75, 30, 75, 85, 256);
            table.Rows.Add(540, 3, 45, 53, 70, 40, 60, 42, 128);
            table.Rows.Add(541, 3, 55, 63, 90, 50, 80, 42, 128);
            table.Rows.Add(542, 3, 75, 103, 80, 70, 80, 92, 128);
            table.Rows.Add(543, 3, 30, 45, 59, 30, 39, 57, 128);
            table.Rows.Add(544, 3, 40, 55, 99, 40, 79, 47, 128);
            table.Rows.Add(545, 3, 60, 100, 89, 55, 69, 112, 128);
            table.Rows.Add(546, 2, 40, 27, 60, 37, 50, 66, 128);
            table.Rows.Add(547, 2, 60, 67, 85, 77, 75, 116, 128);
            table.Rows.Add(548, 2, 45, 35, 50, 70, 50, 30, 257);
            table.Rows.Add(549, 2, 70, 60, 75, 110, 75, 90, 257);
            table.Rows.Add(550, 2, 70, 92, 65, 80, 55, 98, 128);
            table.Rows.Add(551, 3, 50, 72, 35, 35, 35, 65, 128);
            table.Rows.Add(552, 3, 60, 82, 45, 45, 45, 74, 128);
            table.Rows.Add(553, 3, 95, 117, 80, 65, 70, 92, 128);
            table.Rows.Add(554, 3, 70, 90, 45, 15, 45, 50, 128);
            table.Rows.Add(555, 3, 105, 140, 55, 30, 55, 95, 128);
            table.Rows.Add(556, 2, 75, 86, 67, 106, 67, 60, 128);
            table.Rows.Add(557, 2, 50, 65, 85, 35, 35, 55, 128);
            table.Rows.Add(558, 2, 70, 95, 125, 65, 75, 45, 128);
            table.Rows.Add(559, 2, 50, 75, 70, 35, 70, 48, 128);
            table.Rows.Add(560, 2, 65, 90, 115, 45, 115, 58, 128);
            table.Rows.Add(561, 2, 72, 58, 80, 103, 80, 97, 128);
            table.Rows.Add(562, 2, 38, 30, 85, 55, 65, 30, 128);
            table.Rows.Add(563, 2, 58, 50, 145, 95, 105, 30, 128);
            table.Rows.Add(564, 2, 54, 78, 103, 53, 45, 22, 32);
            table.Rows.Add(565, 2, 74, 108, 133, 83, 65, 32, 32);
            table.Rows.Add(566, 2, 55, 112, 45, 74, 45, 70, 32);
            table.Rows.Add(567, 2, 75, 140, 65, 112, 65, 110, 32);
            table.Rows.Add(568, 2, 50, 50, 62, 40, 62, 65, 128);
            table.Rows.Add(569, 2, 80, 95, 82, 60, 82, 75, 128);
            table.Rows.Add(570, 3, 40, 65, 40, 80, 40, 65, 32);
            table.Rows.Add(571, 3, 60, 105, 60, 120, 60, 105, 32);
            table.Rows.Add(572, 1, 55, 50, 40, 40, 40, 75, 192);
            table.Rows.Add(573, 1, 75, 95, 60, 65, 60, 115, 192);
            table.Rows.Add(574, 3, 45, 30, 50, 55, 65, 45, 192);
            table.Rows.Add(575, 3, 60, 45, 70, 75, 85, 55, 192);
            table.Rows.Add(576, 3, 70, 55, 95, 95, 110, 65, 192);
            table.Rows.Add(577, 3, 45, 30, 40, 105, 50, 20, 128);
            table.Rows.Add(578, 3, 65, 40, 50, 125, 60, 30, 128);
            table.Rows.Add(579, 3, 110, 65, 75, 125, 85, 30, 128);
            table.Rows.Add(580, 2, 62, 44, 50, 44, 50, 55, 128);
            table.Rows.Add(581, 2, 75, 87, 63, 87, 63, 98, 128);
            table.Rows.Add(582, 4, 36, 50, 50, 65, 60, 44, 128);
            table.Rows.Add(583, 4, 51, 65, 65, 80, 75, 59, 128);
            table.Rows.Add(584, 4, 71, 95, 85, 110, 95, 79, 128);
            table.Rows.Add(585, 2, 60, 60, 50, 40, 50, 75, 128);
            table.Rows.Add(586, 2, 80, 100, 70, 60, 70, 95, 128);
            table.Rows.Add(587, 2, 55, 75, 60, 75, 60, 103, 128);
            table.Rows.Add(588, 2, 50, 75, 45, 40, 45, 60, 128);
            table.Rows.Add(589, 2, 70, 135, 105, 60, 105, 20, 128);
            table.Rows.Add(590, 2, 69, 55, 45, 55, 55, 15, 128);
            table.Rows.Add(591, 2, 114, 85, 70, 85, 80, 30, 128);
            table.Rows.Add(592, 2, 55, 40, 50, 65, 85, 40, 128);
            table.Rows.Add(593, 2, 100, 60, 70, 85, 105, 60, 128);
            table.Rows.Add(594, 1, 165, 75, 80, 40, 45, 65, 128);
            table.Rows.Add(595, 2, 50, 47, 50, 57, 50, 65, 128);
            table.Rows.Add(596, 2, 70, 77, 60, 97, 60, 108, 128);
            table.Rows.Add(597, 2, 44, 50, 91, 24, 86, 10, 128);
            table.Rows.Add(598, 2, 74, 94, 131, 54, 116, 20, 128);
            table.Rows.Add(599, 3, 40, 55, 70, 45, 60, 30, 258);
            table.Rows.Add(600, 3, 60, 80, 95, 70, 85, 50, 258);
            table.Rows.Add(601, 3, 60, 100, 115, 70, 85, 90, 258);
            table.Rows.Add(602, 4, 35, 55, 40, 45, 40, 60, 128);
            table.Rows.Add(603, 4, 65, 85, 70, 75, 70, 40, 128);
            table.Rows.Add(604, 4, 85, 115, 80, 105, 80, 50, 128);
            table.Rows.Add(605, 2, 55, 55, 55, 85, 55, 30, 128);
            table.Rows.Add(606, 2, 75, 75, 75, 125, 95, 40, 128);
            table.Rows.Add(607, 3, 50, 30, 55, 65, 55, 20, 128);
            table.Rows.Add(608, 3, 60, 40, 60, 95, 60, 55, 128);
            table.Rows.Add(609, 3, 60, 55, 90, 145, 90, 80, 128);
            table.Rows.Add(610, 4, 46, 87, 60, 30, 40, 57, 128);
            table.Rows.Add(611, 4, 66, 117, 70, 40, 50, 67, 128);
            table.Rows.Add(612, 4, 76, 147, 90, 60, 70, 97, 128);
            table.Rows.Add(613, 2, 55, 70, 40, 60, 40, 40, 128);
            table.Rows.Add(614, 2, 95, 110, 80, 70, 80, 50, 128);
            table.Rows.Add(615, 2, 70, 50, 30, 95, 135, 105, 258);
            table.Rows.Add(616, 2, 50, 40, 85, 40, 65, 25, 128);
            table.Rows.Add(617, 2, 80, 70, 40, 100, 60, 145, 128);
            table.Rows.Add(618, 2, 109, 66, 84, 81, 99, 32, 128);
            table.Rows.Add(619, 3, 45, 85, 50, 55, 50, 65, 128);
            table.Rows.Add(620, 3, 65, 125, 60, 95, 60, 105, 128);
            table.Rows.Add(621, 2, 77, 120, 90, 60, 90, 48, 128);
            table.Rows.Add(622, 2, 59, 74, 50, 35, 50, 35, 258);
            table.Rows.Add(623, 2, 89, 124, 80, 55, 80, 55, 258);
            table.Rows.Add(624, 2, 45, 85, 70, 40, 40, 60, 128);
            table.Rows.Add(625, 2, 65, 125, 100, 60, 70, 70, 128);
            table.Rows.Add(626, 2, 95, 110, 95, 40, 95, 55, 128);
            table.Rows.Add(627, 4, 70, 83, 50, 37, 50, 60, 256);
            table.Rows.Add(628, 4, 100, 123, 75, 57, 75, 80, 256);
            table.Rows.Add(629, 4, 70, 55, 75, 45, 65, 60, 257);
            table.Rows.Add(630, 4, 110, 65, 105, 55, 95, 80, 257);
            table.Rows.Add(631, 2, 85, 97, 66, 105, 66, 65, 128);
            table.Rows.Add(632, 2, 58, 109, 112, 48, 48, 109, 128);
            table.Rows.Add(633, 4, 52, 65, 50, 45, 50, 38, 128);
            table.Rows.Add(634, 4, 72, 85, 70, 65, 70, 58, 128);
            table.Rows.Add(635, 4, 92, 105, 90, 125, 90, 98, 128);
            table.Rows.Add(636, 4, 55, 85, 55, 50, 55, 60, 128);
            table.Rows.Add(637, 4, 85, 60, 65, 135, 105, 100, 128);
            table.Rows.Add(638, 4, 91, 90, 129, 90, 72, 108, 258);
            table.Rows.Add(639, 4, 91, 129, 90, 72, 90, 108, 258);
            table.Rows.Add(640, 4, 91, 90, 72, 90, 129, 108, 258);
            table.Rows.Add(641, 4, 79, 115, 70, 125, 80, 111, 256);
            table.Rows.Add(642, 4, 79, 115, 70, 125, 80, 111, 256);
            table.Rows.Add(643, 4, 100, 120, 100, 150, 120, 90, 258);
            table.Rows.Add(644, 4, 100, 150, 120, 120, 100, 90, 258);
            table.Rows.Add(645, 4, 89, 125, 90, 115, 80, 101, 256);
            table.Rows.Add(646, 4, 125, 170, 100, 120, 90, 95, 258);
            table.Rows.Add(647, 4, 91, 72, 90, 129, 90, 108, 258);
            table.Rows.Add(648, 4, 100, 77, 77, 128, 128, 90, 258);
            table.Rows.Add(649, 4, 71, 120, 95, 120, 95, 99, 258);
            table.Rows.Add(650, 3, 56, 61, 65, 48, 45, 33, 32);
            table.Rows.Add(651, 3, 61, 78, 95, 56, 58, 57, 32);
            table.Rows.Add(652, 3, 88, 107, 122, 74, 75, 64, 32);
            table.Rows.Add(653, 3, 40, 45, 40, 62, 60, 60, 32);
            table.Rows.Add(654, 3, 59, 59, 58, 90, 70, 73, 32);
            table.Rows.Add(655, 3, 75, 69, 72, 114, 100, 104, 32);
            table.Rows.Add(656, 3, 41, 56, 40, 62, 44, 71, 32);
            table.Rows.Add(657, 3, 54, 63, 52, 83, 56, 97, 32);
            table.Rows.Add(658, 3, 72, 95, 67, 103, 71, 122, 32);
            table.Rows.Add(659, 2, 38, 36, 38, 32, 36, 57, 128);
            table.Rows.Add(660, 2, 85, 56, 77, 50, 77, 78, 128);
            table.Rows.Add(661, 3, 45, 50, 43, 40, 38, 62, 128);
            table.Rows.Add(662, 3, 62, 73, 55, 56, 52, 84, 128);
            table.Rows.Add(663, 3, 78, 81, 71, 74, 69, 126, 128);
            table.Rows.Add(664, 2, 38, 35, 40, 27, 25, 35, 128);
            table.Rows.Add(665, 2, 45, 22, 60, 27, 30, 29, 128);
            table.Rows.Add(666, 2, 80, 52, 50, 90, 50, 89, 128);
            table.Rows.Add(667, 3, 62, 50, 58, 73, 54, 72, 192);
            table.Rows.Add(668, 3, 86, 68, 72, 109, 66, 106, 192);
            table.Rows.Add(669, 2, 44, 38, 39, 61, 79, 42, 257);
            table.Rows.Add(670, 2, 54, 45, 47, 75, 98, 52, 257);
            table.Rows.Add(671, 2, 78, 65, 68, 112, 154, 75, 257);
            table.Rows.Add(672, 2, 66, 65, 48, 62, 57, 52, 128);
            table.Rows.Add(673, 2, 123, 100, 62, 97, 81, 68, 128);
            table.Rows.Add(674, 2, 67, 82, 62, 46, 48, 43, 128);
            table.Rows.Add(675, 2, 95, 124, 78, 69, 71, 58, 128);
            table.Rows.Add(676, 2, 75, 80, 60, 65, 90, 102, 128);
            table.Rows.Add(677, 2, 62, 48, 54, 63, 60, 68, 128);
            table.Rows.Add(678, 2, 74, 48, 76, 83, 81, 104, 128);
            table.Rows.Add(679, 2, 45, 80, 100, 35, 37, 28, 128);
            table.Rows.Add(680, 2, 59, 110, 150, 45, 49, 35, 128);
            table.Rows.Add(681, 2, 60, 150, 50, 150, 50, 60, 128);
            table.Rows.Add(682, 2, 78, 52, 60, 63, 65, 23, 128);
            table.Rows.Add(683, 2, 101, 72, 72, 99, 89, 29, 128);
            table.Rows.Add(684, 2, 62, 48, 66, 59, 57, 49, 128);
            table.Rows.Add(685, 2, 82, 80, 86, 85, 75, 72, 128);
            table.Rows.Add(686, 2, 53, 54, 37, 46, 45, 45, 128);
            table.Rows.Add(687, 2, 86, 92, 88, 68, 75, 73, 128);
            table.Rows.Add(688, 2, 42, 52, 67, 39, 56, 50, 128);
            table.Rows.Add(689, 2, 72, 105, 115, 54, 86, 68, 128);
            table.Rows.Add(690, 2, 50, 60, 60, 60, 60, 30, 128);
            table.Rows.Add(691, 2, 65, 75, 90, 97, 123, 44, 128);
            table.Rows.Add(692, 4, 50, 53, 62, 58, 63, 44, 128);
            table.Rows.Add(693, 4, 71, 73, 88, 120, 89, 59, 128);
            table.Rows.Add(694, 2, 44, 38, 33, 61, 43, 70, 128);
            table.Rows.Add(695, 2, 62, 55, 52, 109, 94, 109, 128);
            table.Rows.Add(696, 2, 58, 89, 77, 45, 45, 48, 32);
            table.Rows.Add(697, 2, 82, 121, 119, 69, 59, 71, 32);
            table.Rows.Add(698, 2, 77, 59, 50, 67, 63, 46, 32);
            table.Rows.Add(699, 2, 123, 77, 72, 99, 92, 58, 32);
            table.Rows.Add(700, 2, 95, 65, 65, 110, 130, 60, 32);
            table.Rows.Add(701, 2, 78, 92, 75, 74, 63, 118, 128);
            table.Rows.Add(702, 2, 67, 58, 57, 81, 67, 101, 128);
            table.Rows.Add(703, 4, 50, 50, 150, 50, 150, 50, 258);
            table.Rows.Add(704, 4, 45, 50, 35, 55, 75, 40, 128);
            table.Rows.Add(705, 4, 68, 75, 53, 83, 113, 60, 128);
            table.Rows.Add(706, 4, 90, 100, 70, 110, 150, 80, 128);
            table.Rows.Add(707, 1, 57, 80, 91, 80, 87, 75, 128);
            table.Rows.Add(708, 2, 43, 70, 48, 50, 60, 38, 128);
            table.Rows.Add(709, 2, 85, 110, 76, 65, 82, 56, 128);
            table.Rows.Add(710, 2, 49, 66, 70, 44, 55, 51, 128);
            table.Rows.Add(711, 2, 65, 90, 122, 58, 75, 84, 128);
            table.Rows.Add(712, 2, 55, 69, 85, 32, 35, 28, 128);
            table.Rows.Add(713, 2, 95, 117, 184, 44, 46, 28, 128);
            table.Rows.Add(714, 2, 40, 30, 35, 45, 40, 55, 128);
            table.Rows.Add(715, 2, 85, 70, 80, 97, 80, 123, 128);
            table.Rows.Add(716, 4, 126, 131, 95, 131, 98, 99, 258);
            table.Rows.Add(717, 4, 126, 131, 95, 131, 98, 99, 258);
            table.Rows.Add(718, 4, 108, 100, 121, 81, 95, 95, 258);
            table.Rows.Add(719, 4, 50, 100, 150, 100, 150, 50, 258);    // Diancie
            table.Rows.Add(720, 4, 80, 110, 60, 150, 130, 70, 258);  // Hoopa
            table.Rows.Add(721, 4, 80, 110, 120, 130, 90, 70, 258);  // Volcanion
            table.Rows.Add(722, 4, 100, 100, 100, 100, 100, 100, 258);

            return table;
        }
        static DataTable ExpTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Level", typeof(int));

            table.Columns.Add("0 - Erratic", typeof(int));
            table.Columns.Add("1 - Fast", typeof(int));
            table.Columns.Add("2 - MF", typeof(int));
            table.Columns.Add("3 - MS", typeof(int));
            table.Columns.Add("4 - Slow", typeof(int));
            table.Columns.Add("5 - Fluctuating", typeof(int));
            table.Rows.Add(0, 0, 0, 0, 0, 0, 0);
            table.Rows.Add(1, 0, 0, 0, 0, 0, 0);
            table.Rows.Add(2, 15, 6, 8, 9, 10, 4);
            table.Rows.Add(3, 52, 21, 27, 57, 33, 13);
            table.Rows.Add(4, 122, 51, 64, 96, 80, 32);
            table.Rows.Add(5, 237, 100, 125, 135, 156, 65);
            table.Rows.Add(6, 406, 172, 216, 179, 270, 112);
            table.Rows.Add(7, 637, 274, 343, 236, 428, 178);
            table.Rows.Add(8, 942, 409, 512, 314, 640, 276);
            table.Rows.Add(9, 1326, 583, 729, 419, 911, 393);
            table.Rows.Add(10, 1800, 800, 1000, 560, 1250, 540);
            table.Rows.Add(11, 2369, 1064, 1331, 742, 1663, 745);
            table.Rows.Add(12, 3041, 1382, 1728, 973, 2160, 967);
            table.Rows.Add(13, 3822, 1757, 2197, 1261, 2746, 1230);
            table.Rows.Add(14, 4719, 2195, 2744, 1612, 3430, 1591);
            table.Rows.Add(15, 5737, 2700, 3375, 2035, 4218, 1957);
            table.Rows.Add(16, 6881, 3276, 4096, 2535, 5120, 2457);
            table.Rows.Add(17, 8155, 3930, 4913, 3120, 6141, 3046);
            table.Rows.Add(18, 9564, 4665, 5832, 3798, 7290, 3732);
            table.Rows.Add(19, 11111, 5487, 6859, 4575, 8573, 4526);
            table.Rows.Add(20, 12800, 6400, 8000, 5460, 10000, 5440);
            table.Rows.Add(21, 14632, 7408, 9261, 6458, 11576, 6482);
            table.Rows.Add(22, 16610, 8518, 10648, 7577, 13310, 7666);
            table.Rows.Add(23, 18737, 9733, 12167, 8825, 15208, 9003);
            table.Rows.Add(24, 21012, 11059, 13824, 10208, 17280, 10506);
            table.Rows.Add(25, 23437, 12500, 15625, 11735, 19531, 12187);
            table.Rows.Add(26, 26012, 14060, 17576, 13411, 21970, 14060);
            table.Rows.Add(27, 28737, 15746, 19683, 15244, 24603, 16140);
            table.Rows.Add(28, 31610, 17561, 21952, 17242, 27440, 18439);
            table.Rows.Add(29, 34632, 19511, 24389, 19411, 30486, 20974);
            table.Rows.Add(30, 37800, 21600, 27000, 21760, 33750, 23760);
            table.Rows.Add(31, 41111, 23832, 29791, 24294, 37238, 26811);
            table.Rows.Add(32, 44564, 26214, 32768, 27021, 40960, 30146);
            table.Rows.Add(33, 48155, 28749, 35937, 29949, 44921, 33780);
            table.Rows.Add(34, 51881, 31443, 39304, 33084, 49130, 37731);
            table.Rows.Add(35, 55737, 34300, 42875, 36435, 53593, 42017);
            table.Rows.Add(36, 59719, 37324, 46656, 40007, 58320, 46656);
            table.Rows.Add(37, 63822, 40522, 50653, 43808, 63316, 50653);
            table.Rows.Add(38, 68041, 43897, 54872, 47846, 68590, 55969);
            table.Rows.Add(39, 72369, 47455, 59319, 52127, 74148, 60505);
            table.Rows.Add(40, 76800, 51200, 64000, 56660, 80000, 66560);
            table.Rows.Add(41, 81326, 55136, 68921, 61450, 86151, 71677);
            table.Rows.Add(42, 85942, 59270, 74088, 66505, 92610, 78533);
            table.Rows.Add(43, 90637, 63605, 79507, 71833, 99383, 84277);
            table.Rows.Add(44, 95406, 68147, 85184, 77440, 106480, 91998);
            table.Rows.Add(45, 100237, 72900, 91125, 83335, 113906, 98415);
            table.Rows.Add(46, 105122, 77868, 97336, 89523, 121670, 107069);
            table.Rows.Add(47, 110052, 83058, 103823, 96012, 129778, 114205);
            table.Rows.Add(48, 115015, 88473, 110592, 102810, 138240, 123863);
            table.Rows.Add(49, 120001, 94119, 117649, 109923, 147061, 131766);
            table.Rows.Add(50, 125000, 100000, 125000, 117360, 156250, 142500);
            table.Rows.Add(51, 131324, 106120, 132651, 125126, 165813, 151222);
            table.Rows.Add(52, 137795, 112486, 140608, 133229, 175760, 163105);
            table.Rows.Add(53, 144410, 119101, 148877, 141677, 186096, 172697);
            table.Rows.Add(54, 151165, 125971, 157464, 150476, 196830, 185807);
            table.Rows.Add(55, 158056, 133100, 166375, 159635, 207968, 196322);
            table.Rows.Add(56, 165079, 140492, 175616, 169159, 219520, 210739);
            table.Rows.Add(57, 172229, 148154, 185193, 179056, 231491, 222231);
            table.Rows.Add(58, 179503, 156089, 195112, 189334, 243890, 238036);
            table.Rows.Add(59, 186894, 164303, 205379, 199999, 256723, 250562);
            table.Rows.Add(60, 194400, 172800, 216000, 211060, 270000, 267840);
            table.Rows.Add(61, 202013, 181584, 226981, 222522, 283726, 281456);
            table.Rows.Add(62, 209728, 190662, 238328, 234393, 297910, 300293);
            table.Rows.Add(63, 217540, 200037, 250047, 246681, 312558, 315059);
            table.Rows.Add(64, 225443, 209715, 262144, 259392, 327680, 335544);
            table.Rows.Add(65, 233431, 219700, 274625, 272535, 343281, 351520);
            table.Rows.Add(66, 241496, 229996, 287496, 286115, 359370, 373744);
            table.Rows.Add(67, 249633, 240610, 300763, 300140, 375953, 390991);
            table.Rows.Add(68, 257834, 251545, 314432, 314618, 393040, 415050);
            table.Rows.Add(69, 267406, 262807, 328509, 329555, 410636, 433631);
            table.Rows.Add(70, 276458, 274400, 343000, 344960, 428750, 459620);
            table.Rows.Add(71, 286328, 286328, 357911, 360838, 447388, 479600);
            table.Rows.Add(72, 296358, 298598, 373248, 377197, 466560, 507617);
            table.Rows.Add(73, 305767, 311213, 389017, 394045, 486271, 529063);
            table.Rows.Add(74, 316074, 324179, 405224, 411388, 506530, 559209);
            table.Rows.Add(75, 326531, 337500, 421875, 429235, 527343, 582187);
            table.Rows.Add(76, 336255, 351180, 438976, 447591, 548720, 614566);
            table.Rows.Add(77, 346965, 365226, 456533, 466464, 570666, 639146);
            table.Rows.Add(78, 357812, 379641, 474552, 485862, 593190, 673863);
            table.Rows.Add(79, 367807, 394431, 493039, 505791, 616298, 700115);
            table.Rows.Add(80, 378880, 409600, 512000, 526260, 640000, 737280);
            table.Rows.Add(81, 390077, 425152, 531441, 547274, 664301, 765275);
            table.Rows.Add(82, 400293, 441094, 551368, 568841, 689210, 804997);
            table.Rows.Add(83, 411686, 457429, 571787, 590969, 714733, 834809);
            table.Rows.Add(84, 423190, 474163, 592704, 613664, 740880, 877201);
            table.Rows.Add(85, 433572, 491300, 614125, 636935, 767656, 908905);
            table.Rows.Add(86, 445239, 508844, 636056, 660787, 795070, 954084);
            table.Rows.Add(87, 457001, 526802, 658503, 685228, 823128, 987754);
            table.Rows.Add(88, 467489, 545177, 681472, 710266, 851840, 1035837);
            table.Rows.Add(89, 479378, 563975, 704969, 735907, 881211, 1071552);
            table.Rows.Add(90, 491346, 583200, 729000, 762160, 911250, 1122660);
            table.Rows.Add(91, 501878, 602856, 753571, 789030, 941963, 1160499);
            table.Rows.Add(92, 513934, 622950, 778688, 816525, 973360, 1214753);
            table.Rows.Add(93, 526049, 643485, 804357, 844653, 1005446, 1254796);
            table.Rows.Add(94, 536557, 664467, 830584, 873420, 1038230, 1312322);
            table.Rows.Add(95, 548720, 685900, 857375, 902835, 1071718, 1354652);
            table.Rows.Add(96, 560922, 707788, 884736, 932903, 1105920, 1415577);
            table.Rows.Add(97, 571333, 730138, 912673, 963632, 1140841, 1460276);
            table.Rows.Add(98, 583539, 752953, 941192, 995030, 1176490, 1524731);
            table.Rows.Add(99, 591882, 776239, 970299, 1027103, 1212873, 1571884);
            table.Rows.Add(100, 600000, 800000, 1000000, 1059860, 1250000, 1640000);
            return table;
        }

        private void label4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("\"Use legacy breaking behavior\" will use the old save-twice method for breaking save files. With this checked, you need only 26.sav and 16.sav for File 1 and 2, respectively, in order to break your save. Once you've done that, you must leave this option checked and you can open any save file as long as you saved twice (save, reset, save).\r\n\r\nIf you decide to uncheck this option, you MUST break your save again with the new 26.sav, 16.sav, 165.sav method. Then you must leave this option UNchecked and you can open any save file as long as you only saved once.\r\n\r\nOnce you've broken your save with both this option checked and unchecked, you do not need to break your save again - you just need to make sure this is checked if you open a save that was saved twice (or unchecked if you open a save that was saved once).\r\n\r\nThis is useful if not all of your slots are being properly decrypted using the new save-once method.", "Information");
        }
    }
}
