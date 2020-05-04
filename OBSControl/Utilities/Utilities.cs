using Microsoft.SqlServer.Server;
using System;
using System.IO;
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
            if (substitute == null)
                substitute = string.Empty;
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                filenameBuilder.Replace(character.ToString(), substitute);
            }
            filenameBuilder.Replace(" ", "_");
        }

        public static void MinutesAndSeconds(this float totalSeconds, out int minutes, out int seconds)
        {
            minutes = (int)totalSeconds / 60;
            seconds = (int)(totalSeconds % 60f);
        }
    }
}
