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
        public void DateAtStart_Test()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
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
        public void TestThing()
        {
            string intString = "5";
            if(TryParseInteger(intString, out int firstValue) == true)
            {
                Console.WriteLine($"intString is an integer with a value of {firstValue}");
            }
            else
            {
                Console.WriteLine($"intString, '{intString}', is not an integer");
            }
            intString = "a5";
            if (TryParseInteger(intString, out int secondValue) == true)
            {
                Console.WriteLine($"intString is an integer with a value of {secondValue}");
            }
            else
            {
                Console.WriteLine($"intString '{intString}' is not an integer");
            }
        }

        public static bool TryParseInteger(string intString, out int value)
        {
            bool returnValue = int.TryParse(intString, out int result);
            value = result;
            return returnValue;
        }

        [TestMethod]
        public void DateInMiddle_Test()
        {
            TestLevelCompletionResults results = TestLevelCompletionResults.DefaultCompletionResults;
            TestGameplayModifiers modifiers = new TestGameplayModifiers();
            TestDifficultyBeatmap b = TestDifficultyBeatmap.Default;
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
