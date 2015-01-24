using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KeySAV2
{
    public static class NameResourceManager
    {
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> natures;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> types;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> abilitylist;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> movelist;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> itemlist;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> specieslist;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> balls;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> formlist;
        private static Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>> vivlist;

        public static readonly ReadOnlyDefaultableCollection<string> languages;

        static NameResourceManager()
        {
            natures = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            types = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            abilitylist = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            movelist = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            itemlist = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            specieslist = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            balls = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            formlist = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();
            vivlist = new Dictionary<string, Lazy<ReadOnlyDefaultableCollection<string>>>();

            languages = new ReadOnlyDefaultableCollection<string>(new string[]{ "en", "ja", "fr", "it", "de", "es", "ko" });
            foreach (string lang in languages)
            {
                var lang_ = lang;
                natures[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Natures", lang_));
                types[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Types", lang_));
                abilitylist[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Abilities", lang_));
                movelist[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Moves", lang_));
                itemlist[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Items", lang_));
                specieslist[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Species", lang_));
                formlist[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() => getStringList("Forms", lang_));
                balls[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() =>
                {
                    ushort[] ballindices = {
                          0,1,2,3,4,5,6,7,8,9,0xA,0xB,0xC,0xD,0xE,0xF,0x10,
                          0x1EC,0x1ED,0x1EE,0x1EF,0x1F0,0x1F1,0x1F2,0x1F3,
                          0x240 
                    };
                    return new ReadOnlyDefaultableCollection<string>((from id in ballindices select GetItems(lang_)[id]).ToList());
                });
                vivlist[lang] = new Lazy<ReadOnlyDefaultableCollection<string>>(() =>
                {
                    string[] viv = new string[20];
                    viv[0] = GetForms(lang_)[666];
                    for (byte i = 1; i < 20; i++)
                        viv[i] = GetForms(lang_)[i+835];
                    return new ReadOnlyDefaultableCollection<string>(viv);
                });
            }
        }

        private static ReadOnlyDefaultableCollection<string> getStringList(string f, string l) {
            object txt = Properties.Resources.ResourceManager.GetObject("text_" + f + "_" + l); // Fetch File, \n to list.
            return new ReadOnlyDefaultableCollection<string>((from str in ((string)txt).Split(new char[] { '\n' }) select str.Trim()).ToList());
        }

        public static ReadOnlyDefaultableCollection<string> GetNatures(string lang)
        {
            return natures[lang].Value;
        } 

        public static ReadOnlyDefaultableCollection<string> GetTypes(string lang)
        {
            return types[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetAbilities(string lang)
        {
            return abilitylist[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetMoves(string lang)
        {
            return movelist[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetItems(string lang)
        {
            return itemlist[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetSpecies(string lang)
        {
            return specieslist[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetBalls(string lang)
        {
            return balls[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetForms(string lang)
        {
            return formlist[lang].Value;
        }

        public static ReadOnlyDefaultableCollection<string> GetVivillons(string lang)
        {
            return vivlist[lang].Value;
        }
   }
}
