using System.IO;
using System.Text;

namespace OBSControl.Utilities
{
    public static class Utilities
    {
        public static string GetSafeFileName(string fileName)
        {
            StringBuilder retStr = new StringBuilder(fileName);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            retStr.Replace(" ", "_");
            return retStr.ToString();
        }

        public static void MinutesAndSeconds(this float totalSeconds, out int minutes, out int seconds)
        {
            minutes = (int)totalSeconds / 60;
            seconds = (int)(totalSeconds % 60f);
        }
    }
}
