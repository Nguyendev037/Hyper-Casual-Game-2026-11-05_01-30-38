using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    public enum Language
    {
        En = 0,
        Vi = 1,
        Ko = 2
    }

    /// <summary>
    /// Tiny string table. Keys are the original Vietnamese sentences (catalog + UI), values are
    /// {English, Korean}. Missing entries fall back to the Vietnamese text. Contract: SPEC-P04 §1.
    /// </summary>
    public static partial class Loc
    {
        public static Language Current { get; set; } = Language.En;

        public static IEnumerable<string> Keys => Table.Keys;

        public static bool Has(string vi)
        {
            return !string.IsNullOrEmpty(vi) && Table.ContainsKey(vi);
        }

        public static string T(string vi)
        {
            if (string.IsNullOrEmpty(vi) || Current == Language.Vi) return vi;
            return Table.TryGetValue(vi, out string[] entry) ? entry[Current == Language.En ? 0 : 1] : vi;
        }

        public static string F(string viFormat, params object[] args)
        {
            string format = T(viFormat);
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        public static string Get(string vi, Language language)
        {
            if (string.IsNullOrEmpty(vi) || language == Language.Vi) return vi;
            return Table.TryGetValue(vi, out string[] entry) ? entry[language == Language.En ? 0 : 1] : vi;
        }

        public static string Code(Language language)
        {
            switch (language)
            {
                case Language.Vi: return "vi";
                case Language.Ko: return "ko";
                default: return "en";
            }
        }

        public static Language Parse(string code)
        {
            switch (code)
            {
                case "vi": return Language.Vi;
                case "ko": return Language.Ko;
                default: return Language.En;
            }
        }

        public static bool IsKnownCode(string code)
        {
            return code == "en" || code == "vi" || code == "ko";
        }

        public static Language Next(Language language)
        {
            switch (language)
            {
                case Language.En: return Language.Vi;
                case Language.Vi: return Language.Ko;
                default: return Language.En;
            }
        }

        /// <summary>Name of the language written in that language (never translated).</summary>
        public static string NativeName(Language language)
        {
            switch (language)
            {
                case Language.Vi: return "Tiếng Việt";
                case Language.Ko: return "한국어";
                default: return "English";
            }
        }
    }
}
