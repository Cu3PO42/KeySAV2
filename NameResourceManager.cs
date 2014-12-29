using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KeySAV2
{
    public static class NameResourceManager
    {
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> natures;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> types;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> abilitylist;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> movelist;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> itemlist;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> specieslist;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> balls;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> formlist;
        private static Dictionary<string, Lazy<ReadOnlyCollection<string>>> vivlist;

        public static readonly ReadOnlyCollection<string> languages;

        static NameResourceManager()
        {
            natures = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            types = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            abilitylist = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            movelist = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            itemlist = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            specieslist = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            balls = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            formlist = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();
            vivlist = new Dictionary<string, Lazy<ReadOnlyCollection<string>>>();

            languages = new ReadOnlyCollection<string>(new string[]{ "en", "ja", "fr", "it", "de", "es", "ko" });
            foreach (string lang in languages)
            {
                var lang_ = lang;
                natures[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Natures", lang_));
                types[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Types", lang_));
                abilitylist[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Abilities", lang_));
                movelist[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Moves", lang_));
                itemlist[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Items", lang_));
                specieslist[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Species", lang_));
                formlist[lang] = new Lazy<ReadOnlyCollection<string>>(() => getStringList("Forms", lang_));
                balls[lang] = new Lazy<ReadOnlyCollection<string>>(() =>
                {
                    ushort[] ballindices = {
                          0,1,2,3,4,5,6,7,8,9,0xA,0xB,0xC,0xD,0xE,0xF,0x10,
                          0x1EC,0x1ED,0x1EE,0x1EF,0x1F0,0x1F1,0x1F2,0x1F3,
                          0x240 
                    };
                    return new ReadOnlyCollection<string>((from id in ballindices select GetItems(lang_)[id]).ToList());
                });
                vivlist[lang] = new Lazy<ReadOnlyCollection<string>>(() =>
                {
                    string[] viv = new string[20];
                    viv[0] = GetForms(lang_)[666];
                    for (byte i = 1; i < 20; i++)
                        viv[i] = GetForms(lang_)[i+835];
                    return new ReadOnlyCollection<string>(viv);
                });
            }
        }

        private static ReadOnlyCollection<string> getStringList(string f, string l) {
            object txt = Properties.Resources.ResourceManager.GetObject("text_" + f + "_" + l); // Fetch File, \n to list.
            return new ReadOnlyCollection<string>((from str in ((string)txt).Split(new char[] { '\n' }) select str.Trim()).ToList());
        }

        public static ReadOnlyCollection<string> GetNatures(string lang)
        {
            return natures[lang].Value;
        } 

        public static ReadOnlyCollection<string> GetTypes(string lang)
        {
            return types[lang].Value;
        }

        public static ReadOnlyCollection<string> GetAbilities(string lang)
        {
            return abilitylist[lang].Value;
        }

        public static ReadOnlyCollection<string> GetMoves(string lang)
        {
            return movelist[lang].Value;
        }

        public static ReadOnlyCollection<string> GetItems(string lang)
        {
            return itemlist[lang].Value;
        }

        public static ReadOnlyCollection<string> GetSpecies(string lang)
        {
            return specieslist[lang].Value;
        }

        public static ReadOnlyCollection<string> GetBalls(string lang)
        {
            return balls[lang].Value;
        }

        public static ReadOnlyCollection<string> GetForms(string lang)
        {
            return formlist[lang].Value;
        }

        public static ReadOnlyCollection<string> GetVivillons(string lang)
        {
            return vivlist[lang].Value;
        }
   }
}
