using HearthDb.Enums;
using Hearthstone_Deck_Tracker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Data;

namespace PackTracker.View
{
    internal class PackNameConverter : IValueConverter
    {
        private static Config _config = Config.Instance;
        internal static Dictionary<int, Dictionary<string, string>> PackNames;
        static PackNameConverter()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("PackTracker.Resources.packs.json"))
            {
                using (var sr = new StreamReader(s))
                {
                    PackNames = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<string, string>>>(sr.ReadToEnd());
                }
            }
        }

        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "";
            }

            if (int.TryParse(value.ToString(), out var id))
            {
                if (PackNames.ContainsKey(id))
                {
                    if (PackNames[id].ContainsKey("enUS"))
                    {
                        return PackNames[id]["enUS"];
                    }
                }
            }

            return value;
        }

        public static string Convert(int packId, string lang="enUS")
        {
            if (PackNames.ContainsKey(packId))
            {
                return PackNames[packId].ContainsKey(lang) ? PackNames[packId][lang] : PackNames[packId]["enUS"];
            }

            return null;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
