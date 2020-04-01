extern alias BeatSaber;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OBSControlTests.DataTypes;
using System.Diagnostics;
using OBSControl.Utilities;

namespace OBSControlTests
{
    [TestClass]
    public class MinutesAndSeconds_Tests
    {
        [TestMethod]
        public void ValueTests()
        {
            for(int expectedMinutes = 0; expectedMinutes < 120; expectedMinutes++)
            {
                for(int expectedSeconds = 0; expectedSeconds < 60; expectedSeconds++)
                {
                    float totalSeconds = expectedMinutes * 60 + expectedSeconds;
                    totalSeconds.MinutesAndSeconds(out int actualMinutes, out int actualSeconds);
                    Assert.AreEqual(expectedMinutes, actualMinutes);
                    Assert.AreEqual(expectedSeconds, actualSeconds);
                }
            }
        }

      
    }
}
