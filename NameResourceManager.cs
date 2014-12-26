using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace KeySAV2
{
    class NameResourceManager
    {
        private static Dictionary<string, Lazy<string[]>> natures;
        private static Dictionary<string, Lazy<string[]>> types;
        private static Dictionary<string, Lazy<string[]>> abilitylist;
        private static Dictionary<string, Lazy<string[]>> movelist;
        private static Dictionary<string, Lazy<string[]>> itemlist;
        private static Dictionary<string, Lazy<string[]>> specieslist;
        private static Dictionary<string, Lazy<string[]>> balls;
        private static Dictionary<string, Lazy<string[]>> formlist;
        private static Dictionary<string, Lazy<string[]>> vivlist;

        static NameResourceManager()
        {
            string[] languages = new string[] { "en", "ja", "fr", "it", "de", "es", "ko" };
            foreach (string lang in languages)
            {
                var lang_ = lang;
                natures[lang] = new Lazy<string[]>(() => getStringList("Natures", lang_));
                types[lang] = new Lazy<string[]>(() => getStringList("Types", lang_));
                abilitylist[lang] = new Lazy<string[]>(() => getStringList("Abilities", lang_));
                movelist[lang] = new Lazy<string[]>(() => getStringList("Moves", lang_));
                itemlist[lang] = new Lazy<string[]>(() => getStringList("Items", lang_));
                specieslist[lang] = new Lazy<string[]>(() => getStringList("Species", lang_));
                formlist[lang] = new Lazy<string[]>(() => getStringList("Forms", lang_));
                balls[lang] = new Lazy<string[]>(() =>
                {
                    ushort[] ballindices = {
                          0,1,2,3,4,5,6,7,8,9,0xA,0xB,0xC,0xD,0xE,0xF,0x10,
                          0x1EC,0x1ED,0x1EE,0x1EF,0x1F0,0x1F1,0x1F2,0x1F3,
                          0x240 
                    };
                    return (from id in ballindices select GetItem(lang_, id)).ToArray();
                });
                vivlist[lang] = new Lazy<string[]>(() =>
                {
                    string[] viv = new string[20];
                    viv[0] = GetForm(lang_, 666);
                    for (byte i = 1; i < 20; i++)
                        viv[i] = GetForm(lang_, (byte)(i+835));
                    return viv;
                });
            }
        }

        private static string[] getStringList(string f, string l) {
            object txt = Properties.Resources.ResourceManager.GetObject("text_" + f + "_" + l); // Fetch File, \n to list.
            List<string> rawlist = ((string)txt).Split(new char[] { '\n' }).ToList();

            string[] stringdata = new string[rawlist.Count];
            for (int i = 0; i < rawlist.Count; i++)
                stringdata[i] = rawlist[i].Trim();
            return stringdata;
        }

        public static string GetNature(string lang, uint id)
        {
            return natures[lang].Value[id];
        }

        public static string GetType(string lang, uint id)
        {
            return types[lang].Value[id];
        }

        public static string GetAbility(string lang, uint id)
        {
            return abilitylist[lang].Value[id];
        }

        public static string GetMove(string lang, uint id)
        {
            return movelist[lang].Value[id];
        }

        public static string GetItem(string lang, uint id)
        {
            return itemlist[lang].Value[id];
        }

        public static string GetSpecies(string lang, uint id)
        {
            return specieslist[lang].Value[id];
        }

        public static string GetBall(string lang, uint id)
        {
            return balls[lang].Value[id];
        }

        public static string GetForm(string lang, uint id)
        {
            return formlist[lang].Value[id];
        }

        public static string GetVivillon(string lang, uint id)
        {
            return vivlist[lang].Value[id];
        }
   }
}
