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
    public class MinutesAndSeconds_Tests
    {
        [TestMethod]
        public void ValueTests()
        {
            for (int expectedMinutes = 0; expectedMinutes < 120; expectedMinutes++)
            {
                for (int expectedSeconds = 0; expectedSeconds < 60; expectedSeconds++)
                {
                    float totalSeconds = expectedMinutes * 60 + expectedSeconds;
                    totalSeconds.MinutesAndSeconds(out int actualMinutes, out int actualSeconds);
                    Assert.AreEqual(expectedMinutes, actualMinutes);
                    Assert.AreEqual(expectedSeconds, actualSeconds);
                }
            }
        }

        [TestMethod]
        public async Task VersionCheckTest()
        {
            Uri releaseUri = new Uri("https://api.github.com/repos/andruzzzhka/BeatSaberMultiplayer/releases");
            var version = await OBSControl.Utilities.VersionCheck.GetLatestVersionAsync(releaseUri);
            Console.WriteLine($"Current Version: {string.Join(".", version.GetVersionArray())} released on {version.ReleaseDate}");
        }


    }
}
