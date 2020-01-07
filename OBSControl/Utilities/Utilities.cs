using System.IO;
using System.Text;

namespace OBSControl.Utilities
{
    public class Utilities
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
    }
}
