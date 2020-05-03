using OBSControl.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public class BeatmapLevelWrapper : ILevelData
    {
        private IDifficultyBeatmap _difficultyBeatmap;
        public BeatmapLevelWrapper(IDifficultyBeatmap difficultyBeatmap)
        {
            _difficultyBeatmap = difficultyBeatmap;
        }
        public string levelID => _difficultyBeatmap.level.levelID;

        public string songName => _difficultyBeatmap.level.songName;

        public string songSubName => _difficultyBeatmap.level.songSubName;

        public string songAuthorName => _difficultyBeatmap.level.songAuthorName;

        public string levelAuthorName => _difficultyBeatmap.level.levelAuthorName;

        public float beatsPerMinute => _difficultyBeatmap.level.beatsPerMinute;

        public float songTimeOffset => _difficultyBeatmap.level.songTimeOffset;

        public float shuffle => _difficultyBeatmap.level.shuffle;

        public float shufflePeriod => _difficultyBeatmap.level.shufflePeriod;

        public float previewStartTime => _difficultyBeatmap.level.previewStartTime;

        public float previewDuration => _difficultyBeatmap.level.previewDuration;

        public float songDuration => _difficultyBeatmap.level.songDuration;

        public Difficulty difficulty => _difficultyBeatmap.difficulty.ToBeatmapDifficulty();

        public int difficultyRank => _difficultyBeatmap.difficultyRank;

        public float noteJumpMovementSpeed => _difficultyBeatmap.noteJumpMovementSpeed;

        public float noteJumpStartBeatOffset => _difficultyBeatmap.noteJumpStartBeatOffset;
    }

    public enum Difficulty
    {
        Easy, Normal, Hard, Expert, ExpertPlus
    }

    public static class ConversionExtensions
    {
        public static Difficulty ToBeatmapDifficulty(this BeatmapDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BeatmapDifficulty.Easy:
                    return Difficulty.Easy;
                case BeatmapDifficulty.Normal:
                    return Difficulty.Normal;
                case BeatmapDifficulty.Hard:
                    return Difficulty.Hard;
                case BeatmapDifficulty.Expert:
                    return Difficulty.Expert;
                case BeatmapDifficulty.ExpertPlus:
                    return Difficulty.ExpertPlus;
                default:
                    return Difficulty.Easy;
            }
        }
    }
}

