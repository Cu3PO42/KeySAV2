using System;
using System.Text;

namespace KeySAV2.Structures
{
    public struct PKX
    {
        public uint 
            EC, PID, IV32, exp, 
            HP_EV, ATK_EV, DEF_EV, SPA_EV, SPD_EV, SPE_EV,
            HP_IV, ATK_IV, DEF_IV, SPE_IV, SPA_IV, SPD_IV,
            cnt_cool, cnt_beauty, cnt_cute, cnt_smart, cnt_tough, cnt_sheen,
            markings, hptype;

        public string
            nicknamestr, notOT, ot, genderstring;

        public int
            PKRS_Strain, PKRS_Duration,
            metlevel, otgender;

        public bool
            isegg, isnick, isshiny, isghost;

        public ushort
            feflag, genderflag, altforms, 
            ability, abilitynum, nature,
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

        public short 
            box, slot;

        public byte[]
            data;

        public PKX(byte[] pkx, short box, short slot, bool isghost)
        {
            data = pkx;
            this.box = box;
            this.slot = slot;

            this.isghost = isghost;

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
            feflag = (ushort)(pkx[0x1D] % 2);
            genderflag = (ushort)((pkx[0x1D] >> 1) & 0x3);
            altforms = (ushort)(pkx[0x1D] >> 3);
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
            nicknamestr = Encoding.Unicode.GetString(pkx, 0x40, 24).TrimCString();
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
            notOT = Encoding.Unicode.GetString(pkx, 0x78, 24).TrimCString();
            bool notOTG = Convert.ToBoolean(pkx[0x92]);
            // Memory Editor edits everything else with pkx in a new form

            // Block D
            ot = Encoding.Unicode.GetString(pkx, 0xB0, 24).TrimCString();
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

        private static byte[] shuffleArray(byte[] pkx, uint sv)
        {
            byte[] ekx = new Byte[pkx.Length]; Array.Copy(pkx, ekx, 8);

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

        public static byte[] decrypt(byte[] ekx)
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

        public static byte[] encrypt(byte[] pkx)
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

        private static uint LCRNG(uint seed)
        {
            return (seed * 0x41C64E6D + 0x00006073) & 0xFFFFFFFF;
        }

        public static bool verifyCHK(byte[] pkx)
        {
            ushort chk = 0;
            for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                chk += BitConverter.ToUInt16(pkx, i);

            ushort actualsum = BitConverter.ToUInt16(pkx, 0x6);
            if ((BitConverter.ToUInt16(pkx, 0x8) > 750) || (BitConverter.ToUInt16(pkx, 0x90) != 0))
                return false;
            return (chk == actualsum);
        }

        public static byte getDloc(uint ec)
        {
            // Define Shuffle Order Structure
            var dloc = new byte[] { 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 3, 2, 3, 2, 1, 1, 0, 0, 0, 0, 0, 0 };
            uint sv = (((ec & 0x3E000) >> 0xD) % 24);

            return dloc[sv];
        }
        public string Dump(FormattingParameters parameters, string lang)
        {
            if (isghost && parameters.ghost == FormattingParameters.GhostMode.Hide) return null; 

            // Printout Parsing
            if (this.species == 0) 
                return null;

            string box = this.box == -1 ? "~" :  "B"+(this.box+1).ToString("00");
            string slot = ((this.slot / 6 + 1).ToString() + "," + (this.slot % 6 + 1).ToString());
            string species = NameResourceManager.GetSpecies(lang)[this.species];
            string gender = this.genderstring;
            string nature = NameResourceManager.GetNatures(lang)[this.nature];
            string ability = NameResourceManager.GetAbilities(lang)[this.ability];
            string hp = this.HP_IV.ToString();
            string atk = this.ATK_IV.ToString();
            string def = this.DEF_IV.ToString();
            string spa = this.SPA_IV.ToString();
            string spd = this.SPD_IV.ToString();
            string spe = this.SPE_IV.ToString();
            string hptype = NameResourceManager.GetTypes(lang)[(int)this.hptype];
            string ESV = this.ESV.ToString("0000");
            string TSV = this.TSV.ToString("0000");
            string ball = NameResourceManager.GetBalls(lang)[this.ball];
            string nickname = this.nicknamestr;
            string otname = this.ot;
            string TID = this.TID.ToString("00000");
            string SID = this.SID.ToString("00000");
            string move1 = NameResourceManager.GetMoves(lang)[this.move1];
            string move2 = NameResourceManager.GetMoves(lang)[this.move2];
            string move3 = NameResourceManager.GetMoves(lang)[this.move3];
            string move4 = NameResourceManager.GetMoves(lang)[this.move4];
            string ev_hp = this.HP_EV.ToString();
            string ev_at = this.ATK_EV.ToString();
            string ev_de = this.DEF_EV.ToString();
            string ev_sa = this.SPA_EV.ToString();
            string ev_sd = this.SPD_EV.ToString();
            string ev_se = this.SPE_EV.ToString();

            // Bonus
            string relearn1 = NameResourceManager.GetMoves(lang)[this.eggmove1];
            string relearn2 = NameResourceManager.GetMoves(lang)[this.eggmove2];
            string relearn3 = NameResourceManager.GetMoves(lang)[this.eggmove3];
            string relearn4 = NameResourceManager.GetMoves(lang)[this.eggmove4];
            string isshiny = ""; if (this.isshiny) isshiny = "★";
            string isegg = ""; if (this.isegg) isegg = "✓";

            ESV = !this.isegg ? "" : parameters.encloseESV ? "[" + ESV + "]" : ESV;

            // Vivillon Forms...
            if (this.species >= 664 && this.species <= 666)
                species += "-" + NameResourceManager.GetVivillons(lang)[this.altforms];

            if (parameters.boldIVs)
            {
                if (hp == "31") hp = "**31**";
                if (atk == "31") atk = "**31**";
                if (def == "31") def = "**31**";
                if (spa == "31") spa = "**31**";
                if (spd == "31") spd = "**31**";
                if (spe == "31") spe = "**31**";
            }

            string result = String.Format(parameters.formatString, box, slot, species, gender, nature, ability, hp, atk, def, spa, spd, spe, hptype, ESV, TSV, nickname, otname, ball, TID, SID, ev_hp, ev_at, ev_de, ev_sa, ev_sd, ev_se, move1, move2, move3, move4, relearn1, relearn2, relearn3, relearn4, isshiny, isegg);

            if (isghost && parameters.ghost == FormattingParameters.GhostMode.Mark) result = "~" + result;
            return result;
        }
    }
}
