using System;
using System.IO;
using System.Linq;
using System.Text;
#nullable enable
namespace OBSControl.Utilities
{
    public static class Utilities
    {
        public static string GetSafeFilename(string fileName, string? substitute = null)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName), "fileName cannot be null for GetSafeFilename");
            StringBuilder retStr = new StringBuilder(fileName);
            GetSafeFilename(ref retStr, substitute);
            return retStr.ToString();
        }

        public static void GetSafeFilename(ref StringBuilder filenameBuilder, string? substitute = null)
        {
            _ = filenameBuilder ?? throw new ArgumentNullException(nameof(filenameBuilder), "filenameBuilder cannot be null for GetSafeFilename");
            char[] invalidChars = Path.GetInvalidFileNameChars();

            char[] invalidSubstitutes = invalidChars.Where(c => substitute.Contains(c)).ToArray();
            if (substitute == null || invalidSubstitutes.Length > 0)
            {
                if (invalidSubstitutes.Length > 0)
                {
                    Logger.log?.Warn($"{nameof(Plugin.config.InvalidCharacterSubstitute)} has invalid character(s): {string.Join(", ", invalidSubstitutes)}");
                }
                substitute = string.Empty;
            }
            string spaceReplacement = Plugin.config.ReplaceSpacesWith ?? " ";
            if (Plugin.config.ReplaceSpacesWith != " ")
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
