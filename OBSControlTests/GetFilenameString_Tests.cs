using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OBSControlTests.DataTypes;
using System.Diagnostics;
using static OBSControl.Utilities.FileRenaming;
using OBSControl.Wrappers;
using OBSControl.Utilities;
using System.Text;
using System.IO;

namespace OBSControlTests
{
    [TestClass]
    public class GetFilenameString_Tests
    {
        [TestMethod]
        public void NullArguments()
        {
            TestLevelCompletionResults? results = null;
            TestDifficultyBeatmap? difficultyBeatmap = TestDifficultyBeatmap.Default;
            string baseString = "?N-?A_?%<_[?M]><-?F><-?e>";
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.ThrowsException<ArgumentNullException>(() => GetFilenameString(baseString, difficultyBeatmap, results));
#pragma warning restore CS8604 // Possible null reference argument.
            results = TestLevelCompletionResults.DefaultCompletionResults;
            difficultyBeatmap = null;
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.ThrowsException<ArgumentNullException>(() => GetFilenameString(baseString, difficultyBeatmap, results));
#pragma warning restore CS8604 // Possible null reference argument.
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
        public void NullFormat()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string? baseString = null;
            Console.WriteLine($"Format: '{baseString}'");
            string expectedResult = string.Empty;
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void EmptyFormat()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string? baseString = string.Empty;
            Console.WriteLine($"Format: '{baseString}'");
            string expectedResult = string.Empty;
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
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
            int expectedLength = LevelDataSubstitutions.Keys.Count * 7 * 3;
            StringBuilder stringBuilder = new StringBuilder(expectedLength);
            foreach (var ch in LevelDataSubstitutions.Keys)
            {
                stringBuilder.Append("|" + LevelDataSubstitutions[ch] + "-?" + ch + "__");
            }
            string baseString = stringBuilder.ToString();
            Console.WriteLine("Format: " + baseString.Replace("__", "\n        "));
            string result = GetFilenameString(baseString, difficultyBeatmap, results);
            Console.WriteLine(result.Replace("__", "\n"));
        }

        [TestMethod]
        public void CustomSpaceSubstitute()
        {
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            difficultyBeatmap.SongName = "Test Song";
            string baseString = $"?N-?A";
            string result = GetFilenameString(baseString, difficultyBeatmap, results, string.Empty, ".");
            string expectedResult = $"{difficultyBeatmap.SongName.Replace(' ', '.')}-{difficultyBeatmap.LevelAuthorName}";
            Assert.AreEqual(expectedResult, result);
            Console.WriteLine(result.Replace("__", "\n"));
        }

        [TestMethod]
        public void CustomInvalidCharacterSubstitute()
        {
            TestDifficultyBeatmap difficultyBeatmap = TestDifficultyBeatmap.Default;
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            Console.WriteLine($"InvalidFileNameChars: {string.Join(' ', Path.GetInvalidFileNameChars())}");
            for(int i = 0; i < invalidChars.Length; i++)
            {
                difficultyBeatmap.SongName = $"Test{invalidChars[i]}Son g";
                string baseString = $"?N-?A";
                string result = GetFilenameString(baseString, difficultyBeatmap, results, ".", ".");
                string expectedResult = $"Test.Son.g-{difficultyBeatmap.LevelAuthorName}";
                Assert.AreEqual(expectedResult, result);
                Console.WriteLine(result.Replace("__", "\n"));
            }
        }

        [TestMethod]
        public void DateAtStart_Test()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            results.GameplayModifiers = modifiers;
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            string dateFormat = "yyyyMMdd";
            string baseString = $"?@{{{dateFormat}}}-?N-?A-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{DateTime.Now.ToString(dateFormat)}-{b.SongName}-{b.LevelAuthorName}-{b.Difficulty}");
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void DateInProcessingGroup_Test()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            string baseString = $"?N-?A<_?@{{{dateFormat}}}>-?D<_?F>";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}");
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }


        [TestMethod]
        public void UnclosedGroup()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            string baseString = $"?N-?A_?@{{{dateFormat}}}<-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void UnclosedData()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMddHHmm";
            string baseString = $"?N-?A_?@{{{dateFormat}<-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}{{{dateFormat}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void DataWithNoDataSubstitute()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            int limit = 40;
            string baseString = $"?N-?A_?@{{{dateFormat}}}-?D{{{limit}}}";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void LimitedLevelAuthor()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            int limit = 5;
            string baseString = $"?N-?A{{{limit}}}_?@{{{dateFormat}}}-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName.Substring(0, limit)}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void SongAndAuthorLimits()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            int authorLimit = 5;
            int songLimit = 3;
            string baseString = $"?N{{{songLimit}}}-?A{{{authorLimit}}}_?@{{{dateFormat}}}-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName.Substring(0, songLimit)}-{b.LevelAuthorName.Substring(0, authorLimit)}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AuthorLimitHigherThanString()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            int limit = 40;
            string baseString = $"?N-?A{{{limit}}}_?@{{{dateFormat}}}-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AuthorLimit_InvalidInt()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            string baseString = $"?N-?A{{{"A5"}}}_?@{{{dateFormat}}}-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}";
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename(expectedResult);
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }


        [TestMethod]
        public void DateInMiddle_Test()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            string baseString = $"?N-?A_?@{{{dateFormat}}}-?D";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}-{b.Difficulty}");
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void DateAtEnd_Test()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            string dateFormat = "yyyyMMdd";
            string baseString = $"?N-?A_?@{{{dateFormat}}}";
            Console.WriteLine("Format: " + baseString);
            string expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{b.SongName}-{b.LevelAuthorName}_{DateTime.Now.ToString(dateFormat)}");
            string result = GetFilenameString(baseString, b, results);
            Console.WriteLine($"Result: '{result}'");
            Assert.AreEqual(expectedResult, result);
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
            string expectedModifierString = modifiers.ToString();
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
        public void Group_OneSubstituteEmpty()
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
            string baseString = "?N<_?F>";
            string expectedModifierString = modifiers.ToString();
            string expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{b.SongName}");
            Console.WriteLine("Format: " + baseString);
            string result = GetFilenameString(baseString, b, results);
            Assert.AreEqual(expectedResult, result);
            Console.WriteLine("  " + result + ".mkv");
            baseString = "?N-?A_[?M]<-?F>-?e";
            Console.WriteLine("Format: " + baseString);
            result = GetFilenameString(baseString, b, results);
            expectedResult = OBSControl.Utilities.Utilities.GetSafeFilename($"{b.SongName}-{b.LevelAuthorName}_[{expectedModifierString}]-Failed");
            Assert.AreEqual(expectedResult, result);
            Console.WriteLine("  " + result + ".mkv");
        }

        [TestMethod]
        public void InvalidFilenameCharacters()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
            results.GameplayModifiers = modifiers;
            b.SongName = "<?Test?S%ongName>";
            modifiers.DisappearingArrows = true;
            modifiers.FastNotes = true;
            modifiers.SongSpeed = SongSpeed.Slower;
            results.LevelEndStateType = LevelEndState.Failed;
            results.MaxCombo = results.TotalNotes - 1;
            string baseString = "?N-?A_?%<_[?M]><-?F><-?e>";
            Console.WriteLine("Format: " + baseString);
            string result = GetFilenameString(baseString, b, results);
            string scoreStr = Math.Round(results.ScorePercent, 2, MidpointRounding.ToZero).ToString("F2");
            string expectedResult = $"{b.SongName}-{b.LevelAuthorName}_{scoreStr}_[{modifiers}]-Failed";
            expectedResult = Utilities.GetSafeFilename(expectedResult);
            Console.WriteLine($"  '{expectedResult}'");
            Console.WriteLine($"  '{result}'");
            Assert.AreEqual(expectedResult, result);
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
            int expectedLength = LevelDataSubstitutions.Keys.Count * 7 * 3;
            StringBuilder stringBuilder = new StringBuilder(expectedLength);
            foreach (var ch in LevelDataSubstitutions.Keys)
            {
                stringBuilder.Append("|" + LevelDataSubstitutions[ch] + "-?" + ch + "__");
            }
            string baseString = stringBuilder.ToString();
            int iterations = 100;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                _ = GetFilenameString(baseString, difficultyBeatmap, results);
            }
            sw.Stop();
            Console.WriteLine($"Average: {(float)sw.ElapsedMilliseconds / iterations}ms |  {sw.ElapsedMilliseconds}ms for {iterations} iterations.");
        }
    }
}
