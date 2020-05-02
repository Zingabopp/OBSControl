using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OBSControl.Utilities
{
    public struct GithubVersion
    {
        public int Major;
        public int Minor;
        public int Build;
        public int? Revision;
        public DateTime ReleaseDate;
        public void SetVersions(int[] versionAry)
        {
            if(versionAry.Length >= 3)
            {
                Major = versionAry[0];
                Minor = versionAry[1];
                Build = versionAry[2];
            }
            if (versionAry.Length >= 4)
                Revision = versionAry[3];
        }

        public int[] GetVersionArray()
        {
            int[] ary;
            if (Revision != null)
            {
                ary = new int[4];
                ary[3] = Revision.Value;
            }
            else
                ary = new int[3];
            ary[0] = Major;
            ary[1] = Minor;
            ary[2] = Build;
            return ary;
        }
    }
    public static class VersionCheck
    {
        public static readonly string Tag_Key = "tag_name";
        public static readonly string Created_Key = "created_at";
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="releasePageUri"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        public static async Task<GithubVersion> GetLatestVersionAsync(Uri releasePageUri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(releasePageUri);
            request.UserAgent = "VersionChecker/1.0.0";
            WebResponse response = await request.GetResponseAsync().ConfigureAwait(false);
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader sr = new StreamReader(responseStream))
            {
                var thing = JArray.Parse(await sr.ReadToEndAsync().ConfigureAwait(false));
                var first = thing.First;
                string tagLine = thing.First[Tag_Key].Value<string>();
                int[] versions = tagLine.Split('.').Select(s => int.Parse(s)).ToArray();
                DateTime releaseDate = thing.First[Created_Key].Value<DateTime>();
                GithubVersion version = new GithubVersion() { ReleaseDate = releaseDate };
                version.SetVersions(versions);
                return version;
            }


        }
    }
}
