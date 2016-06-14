using System.Collections.Generic;
using System.Text;

namespace LightLDAP.Support
{
    internal class EscapeHelper
    {
        public static string EscapeCharacters(string source)
        {
            return EscapeHelper.EscapeCharacters(source, true);
        }

        public static string EscapeCharacters(string source, bool escapeAsterisk)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>
            {
                {
                    "\\\\",
                    "\\\\5c"
                },
                {
                    "\\/",
                    "\\2f"
                },
                {
                    "(",
                    "\\28"
                },
                {
                    ")",
                    "\\29"
                }
            };
            if (escapeAsterisk)
            {
                dictionary.Add("*", "\\2a");
            }
            StringBuilder stringBuilder = new StringBuilder(source);
            foreach (string current in dictionary.Keys)
            {
                stringBuilder.Replace(current, dictionary[current]);
            }
            return stringBuilder.ToString();
        }
    }
}
