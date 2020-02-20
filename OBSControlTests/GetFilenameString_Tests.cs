extern alias BeatSaber;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OBSControlTests.DataTypes;
using System.Diagnostics;
using static OBSControl.Utilities.FileRenaming;

namespace OBSControlTests
{
    [TestClass]
    public class GetFilenameString_Tests
    {
        [TestMethod]
        public void NullArguments()
        {
            TestLevelCompletionResults results = null;
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            string baseString = "?N-?A_?%<_[?M]><-?F><-?e>";
            Assert.ThrowsException<ArgumentNullException>(() => GetFilenameString(baseString, difficultyBeatmap, results));
            results = TestLevelCompletionResults.DefaultCompletionResults;
            difficultyBeatmap = null;
            Assert.ThrowsException<ArgumentNullException>(() => GetFilenameString(baseString, difficultyBeatmap, results));
        }

        [TestMethod]
        public void NoArguments()
        {

            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            string baseString = "NoArguments";
            Console.WriteLine("Format: " + baseString);
            string result = GetFilenameString(baseString, difficultyBeatmap, results);
            Assert.AreEqual(baseString, result);
        }

        [TestMethod]
        public void AllArguments()
        {

            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            results.gameplayModifiers.disappearingArrows = true;
            results.gameplayModifiers.fastNotes = true;
            results.gameplayModifiers.songSpeed = BeatSaber.GameplayModifiers.SongSpeed.Slower;
            results.levelEndStateType = BeatSaber.LevelCompletionResults.LevelEndStateType.Failed;
            results.maxCombo = results.TotalNotes;
            string baseString = string.Empty;
            foreach (var ch in LevelDataSubstitutions.Keys)
            {
                baseString += "|" + LevelDataSubstitutions[ch] + ":?" + ch + "|\n";
            }
            Console.WriteLine("Format: " + baseString.Replace("\n", "") + "\n");
            string result = GetFilenameString(baseString, difficultyBeatmap, results);
            Console.WriteLine(result);
        }

        [TestMethod]
        public void GroupDemo()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            difficultyBeatmap.TestBeatmapLevel.songName = "<TestSongName>";
            results.gameplayModifiers.disappearingArrows = true;
            results.gameplayModifiers.fastNotes = true;
            results.gameplayModifiers.songSpeed = BeatSaber.GameplayModifiers.SongSpeed.Slower;
            results.levelEndStateType = BeatSaber.LevelCompletionResults.LevelEndStateType.Failed;
            results.maxCombo = results.TotalNotes - 1;
            string baseString = "?N-?A_?%<_[?M]><-?F><-?e>";
            Console.WriteLine("Format: " + baseString);
            string result = GetFilenameString(baseString, difficultyBeatmap, results);
            Console.WriteLine("  " + result + ".mkv");
            baseString = "?N-?A_?%_[?M]-?F-?e";
            Console.WriteLine("Format: " + baseString);
            result = GetFilenameString(baseString, difficultyBeatmap, results);
            Console.WriteLine("  " + result + ".mkv");
        }


        [TestMethod]
        public void PerformanceTest()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            difficultyBeatmap.TestBeatmapLevel.songName = "<TestSongName>";
            results.gameplayModifiers.disappearingArrows = true;
            results.gameplayModifiers.fastNotes = true;
            results.gameplayModifiers.songSpeed = BeatSaber.GameplayModifiers.SongSpeed.Slower;
            results.levelEndStateType = BeatSaber.LevelCompletionResults.LevelEndStateType.Failed;
            results.maxCombo = results.TotalNotes - 1;
            string result = null;
            string baseString = "?N-?A_?%<_[?M]><-?F><-?e>";
            int iterations = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                result = GetFilenameString(baseString, difficultyBeatmap, results);
            }
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds}ms for {iterations} iterations.");
        }
    }
}
