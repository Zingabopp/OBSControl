extern alias BeatSaber;
using BeatSaber;
using System;
using System.Collections.Generic;
using System.Text;

namespace OBSControlTests.DataTypes
{
    public class TestLevelCompletionResults : OBSControl.Wrappers.ILevelCompletionResults
    {
        public static BeatSaber.GameplayModifiers DefaultModifiers
        {
            get
            {
                return new BeatSaber.GameplayModifiers();
            }
        }

        public static TestLevelCompletionResults DefaultCompletionResults
        {
            get
            {
                return new TestLevelCompletionResults(500)
                {
                    MaxModifiedScore = 110000,
                    PlayCount = 0,
                    gameplayModifiers = DefaultModifiers,
                    averageCutScore = 95,
                    maxCombo = 400,
                    badCutsCount = 100,
                    endSongTime = 65f,
                    goodCutsCount = 400,
                    levelEndAction = LevelCompletionResults.LevelEndAction.None,
                    levelEndStateType = LevelCompletionResults.LevelEndStateType.Cleared,
                    missedCount = 100,
                    modifiedScore = 90000,
                    rank = RankModel.Rank.S,
                    rawScore = 100000
                };
            }
        }

        public TestLevelCompletionResults(int totalNotes)
        {
            TotalNotes = totalNotes;
        }

        public int TotalNotes { get; set; }

        public int MaxModifiedScore { get; set; }

        public int PlayCount { get; set; }

        public float ScorePercent => ((float)modifiedScore / MaxModifiedScore) * 100;

        public BeatSaber.GameplayModifiers gameplayModifiers { get; set; }

        public int modifiedScore { get; set; }

        public int rawScore { get; set; }

        public BeatSaber.RankModel.Rank rank { get; set; }

        public bool fullCombo => maxCombo == TotalNotes;

        public BeatSaber.LevelCompletionResults.LevelEndStateType levelEndStateType { get; set; }

        public BeatSaber.LevelCompletionResults.LevelEndAction levelEndAction { get; set; }

        public int averageCutScore { get; set; }

        public int goodCutsCount { get; set; }

        public int badCutsCount { get; set; }

        public int missedCount { get; set; }

        public int maxCombo { get; set; }

        public float endSongTime { get; set; }
    }
}
