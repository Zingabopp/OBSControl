extern alias BeatSaber;
using System;
using System.Collections.Generic;
using System.Text;
using BeatSaber;

namespace OBSControlTests.DataTypes
{
    public class TestDifficultyBeatmap : IDifficultyBeatmap
    {
        public static TestDifficultyBeatmap Default
        {
            get
            {
                return new TestDifficultyBeatmap()
                {
                    TestBeatmapLevel = TestBeatmapLevel.Default,
                    difficulty = BeatmapDifficulty.ExpertPlus,
                    noteJumpMovementSpeed = 15.232f
                };
            }
        }

        public IBeatmapLevel level => TestBeatmapLevel;

        public TestBeatmapLevel TestBeatmapLevel { get; set; }

        public IDifficultyBeatmapSet parentDifficultyBeatmapSet => throw new NotImplementedException();

        public BeatmapDifficulty difficulty
        {
            get
            {
                if (_difficulty != null)
                    return _difficulty ?? BeatmapDifficulty.Easy;
                if (_difficultyRank == null)
                    return BeatmapDifficulty.Easy;
                if (Enum.IsDefined(typeof(BeatmapDifficulty), _difficultyRank))
                    return (BeatmapDifficulty)_difficultyRank;
                return BeatmapDifficulty.Easy;
            }
            set
            {
                _difficulty = value;
            }
        }

        public int difficultyRank
        {
            get
            {
                return _difficultyRank ?? (int)(_difficulty ?? BeatmapDifficulty.Easy);
            }
            set
            {
                _difficultyRank = value;
            }
        }

        public float noteJumpMovementSpeed { get; set; }

        public float noteJumpStartBeatOffset { get; set; }

        private BeatmapDifficulty? _difficulty;
        private int? _difficultyRank;

        public BeatmapData beatmapData => throw new NotImplementedException();
    }
}
