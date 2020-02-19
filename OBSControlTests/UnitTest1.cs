extern alias BeatSaber;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OBSControlTests.DataTypes;
using System.Diagnostics;
using static OBSControl.Utilities.FileRenaming;

namespace OBSControlTests
{
    [TestClass]
    public class UnitTest1
    {


        [TestMethod]
        public void TestMethod1()
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
            string result = GetFileNameString(baseString, difficultyBeatmap, results, results.MaxModifiedScore);
            Console.WriteLine("  " + result + ".mkv");
            baseString = "?N-?A_?%_[?M]-?F-?e";
            Console.WriteLine("Format: " + baseString);
            result = GetFileNameString(baseString, difficultyBeatmap, results, results.MaxModifiedScore);
            Console.WriteLine("  " + result + ".mkv");
        }


        [TestMethod]
        public void TestMethod2()
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
            int iterations = 500000;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                result = GetFileNameString(baseString, difficultyBeatmap, results, results.MaxModifiedScore);
            }
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds}ms for {iterations} iterations.");
        }
    }
}
