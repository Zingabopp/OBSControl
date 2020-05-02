extern alias BeatSaber;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OBSControlTests.DataTypes;
using System.Diagnostics;
using OBSControl.Utilities;
using System.Threading.Tasks;

namespace OBSControlTests
{
    [TestClass]
    public class VersionCheck_Tests
    {


        [TestMethod]
        public async Task VersionCheckHasRevision()
        {
            Uri releaseUri = new Uri("https://api.github.com/repos/andruzzzhka/BeatSaberMultiplayer/releases");
            var version = await OBSControl.Utilities.VersionCheck.GetLatestVersionAsync(releaseUri);
            Console.WriteLine($"Current Version: {string.Join(".", version.GetVersionArray())} released on {version.ReleaseDate}");
        }

        [TestMethod]
        public void VersionCheckNoRevision()
        {
            string versionString = "3.8.2";
            var version = VersionCheck.ParseVersion(versionString);
            Console.WriteLine($"Current Version: {string.Join(".", version.GetVersionArray())} released on {version.ReleaseDate}");
            Assert.AreEqual(3, version.Major);
            Assert.AreEqual(8, version.Minor);
            Assert.AreEqual(2, version.Build);
            Assert.IsNull(version.Revision);
        }

        [TestMethod]
        public void VersionCheckNoRevision_WithMeta()
        {
            string versionString = "3.8.2-beta";
            var version = VersionCheck.ParseVersion(versionString);
            Console.WriteLine($"Current Version: {string.Join(".", version.GetVersionArray())}-{version.Meta} released on {version.ReleaseDate}");
            Assert.AreEqual(3, version.Major);
            Assert.AreEqual(8, version.Minor);
            Assert.AreEqual(2, version.Build);
            Assert.IsNull(version.Revision);
            Assert.AreEqual("beta", version.Meta);
        }
        [TestMethod]
        public void VersionCheckNoRevision_WithMeta_NoDash()
        {
            string versionString = "3.8.2beta";
            var version = VersionCheck.ParseVersion(versionString);
            Console.WriteLine($"Current Version: {string.Join(".", version.GetVersionArray())}-{version.Meta} released on {version.ReleaseDate}");
            Assert.AreEqual(3, version.Major);
            Assert.AreEqual(8, version.Minor);
            Assert.AreEqual(2, version.Build);
            Assert.IsNull(version.Revision);
            Assert.AreEqual("beta", version.Meta);
        }

        [TestMethod]
        public void VersionCheckNoRevision_WithMetaLetter()
        {
            string versionString = "3.8.2g";
            var version = VersionCheck.ParseVersion(versionString);
            Console.WriteLine($"Current Version: {string.Join(".", version.GetVersionArray())}-{version.Meta} released on {version.ReleaseDate}");
            Assert.AreEqual(3, version.Major);
            Assert.AreEqual(8, version.Minor);
            Assert.AreEqual(2, version.Build);
            Assert.IsNull(version.Revision);
            Assert.AreEqual("g", version.Meta);
        }

    }
}
