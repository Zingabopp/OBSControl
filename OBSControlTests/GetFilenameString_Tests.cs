using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OBSControlTests.DataTypes;
using System.Diagnostics;
using static OBSControl.Utilities.FileRenaming;
using OBSControl.Wrappers;

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
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            results.GameplayModifiers = modifiers;
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            modifiers.DisappearingArrows = true;
            modifiers.FastNotes = true;
            modifiers.SongSpeed = SongSpeed.Slower;
            results.LevelEndStateType = LevelEndState.Failed;
            results.MaxCombo = results.TotalNotes;
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
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            b.SongName = "<TestSongName>";
            modifiers.DisappearingArrows = true;
            modifiers.FastNotes = true;
            modifiers.SongSpeed = SongSpeed.Slower;
            results.LevelEndStateType = LevelEndState.Failed;
            results.MaxCombo = results.TotalNotes - 1;
            string baseString = "?N-?A<_[?M]><-?F><-?e>";
            string expectedModifierString = GetModifierString(modifiers);
            string expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{b.SongName}-{b.LevelAuthorName}_[{expectedModifierString}]-Failed");
            Console.WriteLine("Format: " + baseString);
            string result = GetFilenameString(baseString, b, results);
            Assert.AreEqual(expectedResult, result);
            Console.WriteLine("  " + result + ".mkv");
            baseString = "?N-?A_?%_[?M]-?F-?e";
            Console.WriteLine("Format: " + baseString);
            result = GetFilenameString(baseString, b, results);
            Console.WriteLine("  " + result + ".mkv");
        }

        [TestMethod]
        public void InvalidFilenameCharacters()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            difficultyBeatmap.SongName = "<?Test?S%ongName>";
            modifiers.DisappearingArrows = true;
            modifiers.FastNotes = true;
            modifiers.SongSpeed = SongSpeed.Slower;
            results.LevelEndStateType = LevelEndState.Failed;
            results.MaxCombo = results.TotalNotes - 1;
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
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            difficultyBeatmap.SongName = "<TestSongName>";
            modifiers.DisappearingArrows = true;
            modifiers.FastNotes = true;
            modifiers.SongSpeed = SongSpeed.Slower;
            results.LevelEndStateType = LevelEndState.Failed;
            results.MaxCombo = results.TotalNotes - 1;
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
