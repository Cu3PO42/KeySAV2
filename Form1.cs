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
using CheckComboBox;
using KeySAV2.Structures;

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
        public static string datapath = path_exe + Path.DirectorySeparatorChar + "data";
        public static string dbpath = path_exe + Path.DirectorySeparatorChar + "db";
        public static string bakpath = path_exe + Path.DirectorySeparatorChar + "backup";
        public string path_3DS = "";
        public string path_POW = "";

        // Dumping Usage
        private string vidpath;
        private string savpath;
        public string custom1 = ""; public string custom2 = ""; public string custom3 = "";
        public bool custom1b = false; public bool custom2b = false; public bool custom3b = false;
        public string[] boxcolors = new string[] { "", "###", "####", "#####", "######" };
        private ushort[] selectedTSVs = new ushort[0];

        private BattleVideoReader bvReader;
        private ISaveReader saveReader;

        // Breaking Usage
        public string file1 = "";
        public string file2 = "";
        public string file3 = "";

        // UI Usage
        private bool updateIVCheckboxes = true;
        private string oldLanguage = "";

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
                if (!File.Exists(Path.Combine(datapath, "config.ini")))
                    File.Create(Path.Combine(datapath, "config.ini"));
                else
                {
                    TextReader tr = new StreamReader(Path.Combine(datapath, "config.ini"));
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
                if (!File.Exists(Path.Combine(datapath, "config.ini")))
                    File.Create(Path.Combine(datapath, "config.ini"));
                else
                {
                    TextWriter tr = new StreamWriter(Path.Combine(datapath, "config.ini"));
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
        private static Random rand = new Random();
        private static uint rnd32()
        {
            return (uint)(rand.Next(1 << 30)) << 2 | (uint)(rand.Next(1 << 2));
        }

        // PKX Struct Manipulation

        // File Type Loading
        private void B_OpenSAV_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.InitialDirectory = savpath;
            ofd.RestoreDirectory = true;
            ofd.Filter = "SAV 1MB|*.sav;*.bin|Main file|*";
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
            try
            {
                saveReader = SaveBreaker.Load(path);
                TB_SAV.Text = path;
                B_GoSAV.Enabled = CB_BoxEnd.Enabled = CB_BoxStart.Enabled = B_BKP_SAV.Visible = true;
                L_KeySAV.Text = "Key: " + saveReader.KeyName;
                saveReader.scanSlots();
            }
            catch (Exceptions.NoSaveException)
            {
                MessageBox.Show("Incorrect File Size");
                B_GoSAV.Enabled = CB_BoxEnd.Enabled = CB_BoxStart.Enabled = B_BKP_SAV.Visible = false;
            }
            catch (Exceptions.NoKeyException)
            {
                L_KeySAV.Text = "Key not found. Please break for this SAV first.";
                B_GoSAV.Enabled = false;
                B_GoSAV.Enabled = CB_BoxEnd.Enabled = CB_BoxStart.Enabled = B_BKP_SAV.Visible = false;
            }

        }
        
        private void openVID(string path)
        {
            B_GoBV.Enabled = CB_Team.Enabled = false;
            try
            {
                bvReader = BattleVideoBreaker.Load(path);
                B_GoBV.Enabled = CB_Team.Enabled = B_BKP_BV.Visible = true;
                L_KeyBV.Text = "Key: " + bvReader.KeyName;
            }
            catch (Exceptions.NoBattleVideoException)
            {
                MessageBox.Show("Incorrect File Size");
                B_GoBV.Enabled = CB_Team.Enabled = B_BKP_BV.Visible = false;
            }
            catch (Exceptions.NoKeyException)
            {
                L_KeyBV.Text = "Key not found. Please break for this BV first.";
                B_GoBV.Enabled = CB_Team.Enabled = B_BKP_BV.Visible = false;
            }

            // Check up on the key file...
            CB_Team.Items.Clear();
            CB_Team.Items.Add("My Team");
            if (bvReader.DumpsEnemy)
                CB_Team.Items.Add("Enemy Team");

            CB_Team.SelectedIndex = 0;
        }

        // File Dumping
        // SAV

        private FormattingParameters GetFormattingParameters()
        {
            string format = CB_ExportStyle.SelectedIndex == 7 || CB_ExportStyle.SelectedIndex == 6
                ? "{0} - {1} - {2} ({3}) - {4} - {5} - {6}.{7}.{8}.{9}.{10}.{11} - {12} - {13}"
                : RTB_OPTIONS.Text;
            FormattingParameters res = new FormattingParameters
            {
                formatString = format,
                header =
                    String.Format(format,
                        "Box", "Slot", "Species", "Gender", "Nature", "Ability", "HP", "ATK",
                        "DEF", "SPA", "SPD", "SPE", "HiddenPower", "ESV", "TSV", "Nick", "OT", "Ball", "TID", "SID",
                        "HP EV", "ATK EV", "DEF EV", "SPA EV", "SPD EV", "SPE EV", "Move 1", "Move 2", "Move 3",
                        "Move 4", "Relearn 1", "Relearn 2", "Relearn 3", "Relearn 4", "Shiny", "Egg"),
                ghost = CHK_MarkFirst.Checked ? FormattingParameters.GhostMode.Mark : CHK_HideFirst.Checked ? FormattingParameters.GhostMode.Hide : FormattingParameters.GhostMode.None,
                encloseESV = false,
                boldIVs = ((CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || (CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6)) && CHK_BoldIVs.Checked)
            };

            if (CB_ExportStyle.SelectedIndex == 1 || CB_ExportStyle.SelectedIndex == 2 || (CB_ExportStyle.SelectedIndex != 0 && CB_ExportStyle.SelectedIndex < 6 && CHK_R_Table.Checked))
            {
                int args = Regex.Split(RTB_OPTIONS.Text, "{").Length;
                res.header += "\n|";
                for (int i = 0; i < args; i++)
                    res.header += ":---:|";
            }
            else
            {
                res.encloseESV = true;
            }
            return res;
        }

        private void DumpSAV(object sender, EventArgs e)
        {
            RTB_SAV.Clear();
            string csvdata = "Box,Row,Column,Species,Gender,Nature,Ability,HP IV,ATK IV,DEF IV,SPA IV,SPD IV,SPE IV,HP Type,ESV,TSV,Nickname,OT,Ball,TID,SID,HP EV,ATK EV,DEF EV,SPA EV,SPD EV,SPE EV,Move 1,Move 2,Move 3,Move 4,Relearn 1, Relearn 2, Relearn 3, Relearn 4, Shiny, Egg\n";
            FormattingParameters parameters = GetFormattingParameters();
            FormattingParameters csvParameters = parameters;
            csvParameters.formatString = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}\n";
            csvParameters.encloseESV = false;
            csvParameters.boldIVs = false;
            byte boxstart, boxend;
            ushort dumpedcounter = 0;
            ushort tmp = 0;
            ushort[] selectedTSVs = (from val in Regex.Split(TB_SVs.Text, @"\s*[\s,;.]\s*") where UInt16.TryParse(val, out tmp) select tmp).ToArray();
            if (CB_BoxStart.Text == "All")
            {
                boxstart = 0;
                boxend = 30;
            }
            else
            {
                boxstart = (byte)(CB_BoxStart.SelectedIndex - 1);
                boxend = (byte)(boxstart + CB_BoxEnd.SelectedIndex + 1);
            }

            for (byte i = boxstart; i < boxend; i++)
            {
                if (CHK_Split.Checked)
                {
                    RTB_SAV.AppendText("\n");
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
                    RTB_SAV.AppendText("B" + i.ToString("00") + "\n\n");
                    RTB_SAV.AppendText(parameters.header + "\n");
                }
                else if (i == boxstart)
                    RTB_SAV.AppendText(parameters.header + "\n");
                for (ushort j = (ushort)(i*30); j < i*30 + 30; ++j)
                {
                    PKX? slot = saveReader.getPkx(j);
                    if (!slot.HasValue) continue;
                    string res = slot.Value.Dump(parameters, oldLanguage);
                    if (res == null) continue;
                    if (CHK_Enable_Filtering.Checked && !((Func<bool>) (() =>
                    {
                        PKX pkx = slot.Value;
                        if (CHK_Egg.Checked && !pkx.isegg) return false;

                        if (CB_Abilities.Text != "" && CB_Abilities.SelectedIndex != 0 && CB_Abilities.Text != NameResourceManager.GetAbilities(oldLanguage)[pkx.ability])
                            return false;

                        bool checkHP = CCB_HPType.GetItemCheckState(0) != CheckState.Checked;
                        byte checkHPDiff = (byte)Convert.ToInt16(checkHP);
                        int perfects = CB_No_IVs.SelectedIndex;
                        foreach(var iv in new [] {
                            new Tuple<uint, bool>(pkx.HP_IV, CHK_IV_HP.Checked), 
                            new Tuple<uint, bool>(pkx.DEF_IV, CHK_IV_Def.Checked), 
                            new Tuple<uint, bool>(pkx.SPA_IV, CHK_IV_SpAtk.Checked), 
                            new Tuple<uint, bool>(pkx.SPD_IV, CHK_IV_SpDef.Checked) })
                        {
                            if (31 - iv.Item1 <= checkHPDiff) --perfects;
                            else if (iv.Item2) return false;
                        }
                        foreach(var iv in new [] {
                            new Tuple<uint, bool, bool>(pkx.ATK_IV, CHK_IV_Atk.Checked, CHK_Special_Attacker.Checked), 
                            new Tuple<uint, bool, bool>(pkx.SPE_IV, CHK_IV_Spe.Checked, CHK_Trickroom.Checked) })
                        {
                            if (Math.Abs((iv.Item3 ? 0: 31) - iv.Item1) <= checkHPDiff) --perfects;
                            else if (iv.Item2) return false;
                        }

                        if(perfects > 0) return false;

                        if(checkHP && !CCB_HPType.GetItemChecked((int)pkx.hptype)) return false;

                        if(!CCB_Natures.GetItemChecked((int)pkx.nature+1)) return false;

                        if (CHK_Is_Shiny.Checked || CHK_Hatches_Shiny_For_Me.Checked || CHK_Hatches_Shiny_For.Checked)
                        {
                            if (!(CHK_Is_Shiny.Checked && pkx.isshiny ||
                                pkx.isegg && CHK_Hatches_Shiny_For_Me.Checked && pkx.ESV == pkx.TSV ||
                                pkx.isegg && CHK_Hatches_Shiny_For.Checked && Array.IndexOf(selectedTSVs, pkx.ESV) > -1))
                                return false;
                        }

                        if(RAD_Male.Checked && pkx.genderflag != 0 || RAD_Female.Checked && pkx.genderflag != 1)
                            return false;

                        return true;
                    }))())
                        continue;
                    if (CB_ExportStyle.SelectedIndex == 7)
                        SavePKX(slot.Value);
                    else if (CB_ExportStyle.SelectedIndex == 6)
                        csvdata += slot.Value.Dump(csvParameters, oldLanguage);
                    dumpedcounter++;
                    RTB_SAV.AppendText(res);
                    RTB_SAV.AppendText("\n");
                }
            }

            // Copy Results to Clipboard
            try
            {
                Clipboard.SetText(RTB_SAV.Text);
                RTB_SAV.AppendText("\nData copied to clipboard!");
            }
            catch { };
            RTB_SAV.AppendText("\nDumped: " + dumpedcounter);
            RTB_SAV.Select(RTB_SAV.Text.Length - 1, 0);
            RTB_SAV.ScrollToCaret();

            if (CB_ExportStyle.SelectedIndex == 6)
            {
                SaveFileDialog savecsv = new SaveFileDialog();
                savecsv.Filter = "Spreadsheet|*.csv";
                savecsv.FileName = "KeySAV Data Dump.csv";
                if (savecsv.ShowDialog() == DialogResult.OK)
                    System.IO.File.WriteAllText(savecsv.FileName, csvdata, Encoding.UTF8);
            }
        }

        private void SavePKX(PKX pkx)
        {
            string isshiny = "";
            string nicknamestr = pkx.nicknamestr;
            if (pkx.isshiny)
                isshiny = " ★";
            if (pkx.isnick)
                nicknamestr += String.Format(" ({0})", NameResourceManager.GetSpecies(oldLanguage)[pkx.species]);

            string savedname =
                pkx.species.ToString("000") + isshiny + " - "
                + pkx.nicknamestr + " - "
                + pkx.chk.ToString("X4") + pkx.EC.ToString("X8");
            File.WriteAllBytes(Path.Combine(dbpath, Utility.CleanFileName(savedname) + ".pk6"), pkx.data);
        }
        // BV
        private void dumpBV(object sender, EventArgs e)
        {
            RTB_VID.Clear();
            string csvdata = "Position,Species,Gender,Nature,Ability,HP IV,ATK IV,DEF IV,SPA IV,SPD IV,SPE IV,HP Type,ESV,TSV,Nickname,OT,Ball,TID,SID,HP EV,ATK EV,DEF EV,SPA EV,SPD EV,SPE EV,Move 1,Move 2,Move 3,Move 4,Relearn 1, Relearn 2, Relearn 3, Relearn 4, Shiny, Egg\n";

            // player @ 0xX100, opponent @ 0x1800;
            FormattingParameters parameters = GetFormattingParameters();
            FormattingParameters csvParameters = parameters;
            csvParameters.formatString = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}\n";
            csvParameters.encloseESV = false;
            csvParameters.boldIVs = false;

            for (byte i = 0; i < 6; i++)
            {
                PKX pkx = bvReader.getPkx(i, (byte)CB_Team.SelectedIndex);
                string res = pkx.Dump(parameters, oldLanguage);
                if (res == null) continue;
                RTB_VID.AppendText(res);
                RTB_VID.AppendText("\n");
                if (CB_ExportStyle.SelectedIndex == 7)
                    SavePKX(pkx);
                else if (CB_ExportStyle.SelectedIndex == 6)
                    csvdata += pkx.Dump(csvParameters, oldLanguage);

            }

            // Copy Results to Clipboard
            try
            {
                Clipboard.SetText(RTB_VID.Text);
                RTB_VID.AppendText("\nData copied to clipboard!"); 
            }
            catch { };
            
            RTB_VID.Select(RTB_VID.Text.Length - 1, 0);
            RTB_VID.ScrollToCaret(); 
            if (CB_ExportStyle.SelectedIndex == 6)
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
            CCB_HPType.Enabled = CB_No_IVs.Enabled = CHK_Trickroom.Enabled =
            CHK_Special_Attacker.Enabled = CHK_IVsAny.Enabled =
            CHK_IV_HP.Enabled = CHK_IV_Atk.Enabled = CHK_IV_Def.Enabled =
            CHK_IV_SpAtk.Enabled = CHK_IV_SpDef.Enabled = CHK_IV_Spe.Enabled =
            CHK_Is_Shiny.Enabled = CHK_Hatches_Shiny_For_Me.Enabled =
            CHK_Hatches_Shiny_For.Enabled = TB_SVs.Enabled =
            CHK_Egg.Enabled = RAD_Male.Enabled = RAD_Female.Enabled =
            RAD_GenderAny.Enabled  = CCB_Natures.Enabled =
            CB_Abilities.Enabled = CHK_Enable_Filtering.Checked;
        }

        // File Keystream Breaking
        private void LoadBreakBase(ref string file, TextBox textbox)
        {
            // Open Save File
            OpenFileDialog boxsave = new OpenFileDialog();
            boxsave.Filter = "Save/BV File|*.*";

            if (boxsave.ShowDialog() == DialogResult.OK)
            {
                string path = boxsave.FileName;
                FileInfo info = new FileInfo(path);
                if ((info.Length == 0x10009C) || info.Length == 0x100000)
                {
                    textbox.Text = path;
                    file = "SAV";
                }
                else if (info.Length == 28256)
                {
                    textbox.Text = path;
                    file = "BV";
                }
                else
                {
                    file = "";
                    MessageBox.Show("Incorrect File Loaded: Neither a SAV (1MB) or Battle Video (~27.5KB).", "Error");
                }
            } 
            togglebreak();
        }

        private void loadBreak1(object sender, EventArgs e)
        {
            LoadBreakBase(ref file1, TB_File1);
        }

        private void loadBreak2(object sender, EventArgs e)
        {
            LoadBreakBase(ref file2, TB_File2);
        }

        private void loadBreak3(object sender, EventArgs e)
        {
            LoadBreakBase(ref file3, TB_File3);
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
                if ((file1 == "SAV" && file2 == "SAV" && file3 == "SAV" && TB_File3.Text != "") || (file1 == "BV" && file2 == "BV"))
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
            string result;
            byte[] bvkey = BattleVideoBreaker.Break(file1, file2, out result);
            if (bvkey != null)
            {
                MessageBox.Show(result);

                FileInfo fi = new FileInfo(TB_File1.Text);
                string bvnumber = Regex.Split(fi.Name, "(-)")[0];
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = Utility.CleanFileName(String.Format("BV Key - {0}.bin", bvnumber));
                string ID = sfd.InitialDirectory;
                sfd.InitialDirectory = Path.Combine(path_exe, "data");
                sfd.RestoreDirectory = true;
                sfd.Filter = "Video Key|*.bin";
                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, bvkey);
                else
                    MessageBox.Show("Chose not to save keystream.", "Alert");
                sfd.InitialDirectory = ID; sfd.RestoreDirectory = true;
            }
            else
                MessageBox.Show(result, "Error");
        }
        private void breakSAV()
        {
            string result;
            byte[] pkx;
            SaveKey? key = SaveBreaker.Break(TB_File1.Text, TB_File2.Text, TB_File3.Text, out result, out pkx);

            MessageBox.Show(result);

            if (key.HasValue)
            {
                string ot = Encoding.Unicode.GetString(pkx, 0xB0, 24).TrimCString();
                ushort tid = BitConverter.ToUInt16(pkx, 0xC);
                ushort sid = BitConverter.ToUInt16(pkx, 0xE);
                ushort tsv = (ushort)((tid ^ sid) >> 4);
                SaveFileDialog sfd = new SaveFileDialog();
                string ID = sfd.InitialDirectory;
                sfd.InitialDirectory = Path.Combine(path_exe, "data");
                sfd.RestoreDirectory = true;
                sfd.FileName = Utility.CleanFileName(String.Format("SAV Key - {0} - ({1}.{2}) - TSV {3}.bin", ot, tid.ToString("00000"), sid.ToString("00000"), tsv.ToString("0000")));
                sfd.Filter = "Save Key|*.bin";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    key.Value.Save(sfd.FileName);
                    SaveKeyStore.UpdateFile(sfd.FileName, key.Value);
                }
                else
                    MessageBox.Show("Chose not to save keystream.", "Alert");

                sfd.InitialDirectory = ID; sfd.RestoreDirectory = true;
            }
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
                    savpath = Path.Combine(path_3DS, "title", "00040000", "00055d00"); 
                    vidpath = Path.Combine(path_3DS, "extdata", "00000000", "0000055d", "00000000"); 
                }
                else if (game == 1)
                {
                    // Y
                    savpath = Path.Combine(path_3DS, "title", "00040000", "00055e00"); 
                    vidpath = Path.Combine(path_3DS, "extdata", "00000000", "0000055e", "00000000"); 
                }
                else if (game == 2) 
                {
                    // OR
                    savpath = Path.Combine(path_3DS, "title", "00040000", "0011c400");
                    vidpath = Path.Combine(path_3DS, "extdata", "00000000", "000011c4", "00000000");
                }
                else if (game == 3)
                {
                    // AS
                    savpath = Path.Combine(path_3DS, "title", "00040000", "0011c500");
                    vidpath = Path.Combine(path_3DS, "extdata", "00000000", "000011c5", "00000000");
                }

                if (Directory.Exists(savpath))
                {
                    if (File.Exists(Path.Combine(savpath,"00000001.sav")))
                        this.Invoke(new MethodInvoker(delegate { openSAV(Path.Combine(savpath, "00000001.sav")); }));
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
            MessageBox.Show(
                 "{0} - Box\n"
                +"{1} - Slot\n"
                +"{2} - Species\n"
                +"{3} - Gender\n"
                +"{4} - Nature\n"
                +"{5} - Ability\n"
                +"{6} - HP IV\n"
                +"{7} - ATK IV\n"
                +"{8} - DEF IV\n"
                +"{9} - SPA IV\n"
                +"{10} - SPE IV\n"
                +"{11} - SPD IV\n"
                +"{12} - Hidden Power Type\n"
                +"{13} - ESV\n"
                +"{14} - TSV\n"
                +"{15} - Nickname\n"
                +"{16} - OT Name\n"
                +"{17} - Ball\n"
                +"{18} - TID\n"
                +"{19} - SID\n"
                +"{20} - HP EV\n"
                +"{21} - ATK EV\n"
                +"{22} - DEF EV\n"
                +"{23} - SPA EV\n"
                +"{24} - SPD EV\n"
                +"{25} - SPE EV\n"
                +"{26} - Move 1\n"
                +"{27} - Move 2\n"
                +"{28} - Move 3\n"
                +"{29} - Move 4\n"
                +"{30} - Relearn 1\n"
                +"{31} - Relearn 2\n"
                +"{32} - Relearn 3\n"
                +"{33} - Relearn 4\n"
                +"{34} - Is Shiny\n"
                +"{35} - Is Egg\n"
                ,"Help"
                );
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
                CSV
                To .PK6 File 
             */
            CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = false;
            if (CB_ExportStyle.SelectedIndex == 0) // Default
            {
                CHK_R_Table.Visible = false;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                    "{0} - {1} - {2} ({3}) - {4} - {5} - {6}.{7}.{8}.{9}.{10}.{11} - {12} - {13}";
            }
            else if (CB_ExportStyle.SelectedIndex == 1) // Reddit
            {
                CHK_R_Table.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "{0} | {1} | {2} ({3}) | {4} | {5} | {6}.{7}.{8}.{9}.{10}.{11} | {12} | {13} |";
            }
            else if (CB_ExportStyle.SelectedIndex == 2) // TSV
            {
                CHK_R_Table.Visible = false;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "{0} | {1} | {16} | {18} | {14} |";
            }
            else if (CB_ExportStyle.SelectedIndex == 3) // Custom 1
            {
                CHK_R_Table.Visible = true; CHK_R_Table.Checked = custom1b;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = false;
                RTB_OPTIONS.Text = custom1;
            }
            else if (CB_ExportStyle.SelectedIndex == 4) // Custom 2
            {
                CHK_R_Table.Visible = true; CHK_R_Table.Checked = custom2b;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = false;
                RTB_OPTIONS.Text = custom2;
            }
            else if (CB_ExportStyle.SelectedIndex == 5) // Custom 3
            {
                CHK_R_Table.Visible = true; CHK_R_Table.Checked = custom3b;
                CHK_BoldIVs.Visible = CHK_ColorBox.Visible = CB_BoxColor.Visible = true;
                RTB_OPTIONS.ReadOnly = false;
                RTB_OPTIONS.Text = custom3;
            }
            else if (CB_ExportStyle.SelectedIndex == 6) // CSV
            {
                CHK_R_Table.Visible = false;
                RTB_OPTIONS.ReadOnly = true; RTB_OPTIONS.Text =
                "CSV will output everything imagineable to the specified location.";
            }
            else if (CB_ExportStyle.SelectedIndex == 7) // PK6
            {
                CHK_R_Table.Visible = false;
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
        private void InitializeStrings()
        {
            string curlanguage = NameResourceManager.languages[CB_MainLanguage.SelectedIndex];

            var natures = NameResourceManager.GetNatures(curlanguage);
            var types = NameResourceManager.GetTypes(curlanguage);
            var abilitylist = NameResourceManager.GetAbilities(curlanguage);

            int curAbility;
            if (CB_Abilities.Text != "")
                curAbility = NameResourceManager.GetAbilities(oldLanguage).IndexOf(CB_Abilities.Text);
            else
                curAbility = -1;

            // Populate natures in filters
            if (CCB_Natures.Items.Count == 0)
            {
                CCB_Natures.Items.Add(new CCBoxItem("All", 0));
                for (byte i = 0; i < natures.Count;)
                    CCB_Natures.Items.Add(new CCBoxItem(natures[i], ++i));
                CCB_Natures.DisplayMember = "Name";
                CCB_Natures.SetItemChecked(0, true);
            }
            else
            {
                for (byte i = 0; i < natures.Count; ++i)
                    (CCB_Natures.Items[i+1] as CCBoxItem).Name = natures[i];
            }

            // Populate HP types in filters
            if (CCB_HPType.Items.Count == 0)
            {
                CCB_HPType.Items.Add(new CCBoxItem("Any", 0));
                for (byte i = 1; i < types.Count-1;)
                    CCB_HPType.Items.Add(new CCBoxItem(types[i], ++i));
                CCB_HPType.DisplayMember = "Name";
                CCB_HPType.SetItemChecked(0, true);
            }
            else
            {
                for (byte i = 1; i < types.Count-1; ++i)
                    (CCB_HPType.Items[i] as CCBoxItem).Name = types[i];
            }

            // Populate ability list
            string[] sortedAbilities = abilitylist.ToArray();
            Array.Sort(sortedAbilities);
            CB_Abilities.Items.Clear();
            CB_Abilities.Items.AddRange(sortedAbilities);
            if (curAbility != -1) CB_Abilities.Text = abilitylist[curAbility];

            oldLanguage = curlanguage;

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
            string newpath = bakpath + Path.DirectorySeparatorChar + bkpdate + fi.Name;
            if (File.Exists(newpath))
            {
                DialogResult sdr = MessageBox.Show("File already exists!\n\nOverwrite?", "Prompt", MessageBoxButtons.YesNo);
                if (sdr == DialogResult.Yes)
                    File.Delete(newpath);
                else 
                    return;
            }

            File.Copy(tb.Text, newpath);
            MessageBox.Show("Copied to Backup Folder.\n\nFile named:\n" + newpath, "Alert");
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
            string newpath = bakpath + Path.DirectorySeparatorChar + bkpdate + fi.Name;
            if (File.Exists(newpath))
            {
                DialogResult sdr = MessageBox.Show("File already exists!\n\nOverwrite?", "Prompt", MessageBoxButtons.YesNo);
                if (sdr == DialogResult.Yes)
                    File.Delete(newpath);
                else 
                    return;
            }

            File.Copy(tb.Text, newpath);
            MessageBox.Show("Copied to Backup Folder.\n\nFile named:\n" + newpath, "Alert");
        }

        private void B_BreakFolder_Click(object sender, EventArgs e)
        {
            foreach (string path in Directory.GetFiles(TB_Folder.Text))
            {
                try
                {
                    SaveBreaker.Load(path).scanSlots();
                }
                catch { }
            }
            MessageBox.Show("Processed all files in folder...");
        }

        private void toggleIVAll(object sender, EventArgs e)
        {
            if(updateIVCheckboxes)
                switch ((new [] {CHK_IV_HP, CHK_IV_Atk, CHK_IV_Def, CHK_IV_SpAtk, CHK_IV_SpDef, CHK_IV_Spe}).Count(c => c.Checked))
                {
                    case 0:
                        CHK_IVsAny.CheckState = CheckState.Unchecked;
                        break;
                    case 6:
                        CHK_IVsAny.CheckState = CheckState.Checked;
                        break;
                    default:
                        CHK_IVsAny.CheckState = CheckState.Indeterminate;
                        break;
                }
        }

        private void toggleIVsAny(object sender, EventArgs e)
        {
            updateIVCheckboxes = false;
            if (CHK_IVsAny.CheckState != CheckState.Indeterminate)
                foreach (var box in new [] {CHK_IV_HP, CHK_IV_Atk, CHK_IV_Def, CHK_IV_SpAtk, CHK_IV_SpDef, CHK_IV_Spe})
                    box.Checked = CHK_IVsAny.Checked;
            updateIVCheckboxes = true;
        }

        private void toggleTrickroom(object sender, EventArgs e)
        {
            CHK_IV_Spe.Text = (CHK_Trickroom.Checked ? "Spe (= 0)" : "Spe");
        }

        private void toggleSpecialAttacker(object sender, EventArgs e)
        {
            CHK_IV_Atk.Text = (CHK_Special_Attacker.Checked ? "Atk (= 0)" : "Atk");
        }
    }
}
