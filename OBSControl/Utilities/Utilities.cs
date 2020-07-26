using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
#nullable enable
namespace OBSControl.Utilities
{
    public static class Utilities
    {
        public static bool TryParseColorString(string colorStr, out Color color)
        {
            return ColorUtility.TryParseHtmlString(colorStr, out color);
        }

        public static string GetSafeFilename(string fileName, string? substitute = null, string? spaceReplacement = null)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName), "fileName cannot be null for GetSafeFilename");
            StringBuilder retStr = new StringBuilder(fileName);
            GetSafeFilename(ref retStr, substitute, spaceReplacement);
            return retStr.ToString();
        }

        public static void GetSafeFilename(ref StringBuilder filenameBuilder, string? substitute = null, string? spaceReplacement = null)
        {
            _ = filenameBuilder ?? throw new ArgumentNullException(nameof(filenameBuilder), "filenameBuilder cannot be null for GetSafeFilename");
            char[] invalidChars = Path.GetInvalidFileNameChars();

            char[] invalidSubstitutes = substitute == null ? Array.Empty<char>() : invalidChars.Where(c => substitute.Contains(c)).ToArray();
            if (substitute == null || invalidSubstitutes.Length > 0)
            {
                if (invalidSubstitutes.Length > 0)
                {
                    //Logger.log?.Warn($"{nameof(Plugin.config.InvalidCharacterSubstitute)} has invalid character(s): {string.Join(", ", invalidSubstitutes)}");
                }
                substitute = string.Empty;
            }
            if (spaceReplacement != null && spaceReplacement != " ")
                filenameBuilder.Replace(" ", spaceReplacement);
            foreach (char character in invalidChars)
            {
                filenameBuilder.Replace(character.ToString(), substitute);
            }
        }

        public static void MinutesAndSeconds(this float totalSeconds, out int minutes, out int seconds)
        {
            minutes = (int)totalSeconds / 60;
            seconds = (int)(totalSeconds % 60f);
        }
    }
}
