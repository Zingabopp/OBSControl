using OBSControl.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OBSControlTests.DataTypes
{
    public class TestLevelCompletionResults : OBSControl.Wrappers.ILevelCompletionResults
    {
        public static IGameplayModifiers DefaultModifiers
        {
            get
            {
                return new TestGameplayModifiers();
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
                    GameplayModifiers = DefaultModifiers,
                    AverageCutScore = 95,
                    MaxCombo = 400,
                    BadCutsCount = 100,
                    EndSongTime = 65f,
                    GoodCutsCount = 400,
                    LevelEndAction = SongEndAction.None,
                    LevelEndStateType = LevelEndState.Cleared,
                    MissedCount = 100,
                    ModifiedScore = 90000,
                    Rank = ScoreRank.S,
                    RawScore = 100000
                };
            }
        }

        public TestLevelCompletionResults(int totalNotes)
        {
            TotalNotes = totalNotes;
            GameplayModifiers = DefaultModifiers;
        }

        public int TotalNotes { get; set; }

        public int MaxModifiedScore { get; set; }

        public int PlayCount { get; set; }

        public float ScorePercent => ((float)ModifiedScore / MaxModifiedScore) * 100;

        public IGameplayModifiers GameplayModifiers { get; set; }

        public int ModifiedScore { get; set; }

        public int RawScore { get; set; }

        public ScoreRank Rank { get; set; }

        public bool FullCombo => MaxCombo == TotalNotes;

        public LevelEndState LevelEndStateType { get; set; }

        public SongEndAction LevelEndAction { get; set; }

        public int AverageCutScore { get; set; }

        public int GoodCutsCount { get; set; }

        public int BadCutsCount { get; set; }

        public int MissedCount { get; set; }

        public int MaxCombo { get; set; }

        public float EndSongTime { get; set; }
    }
}
