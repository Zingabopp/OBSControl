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
        public readonly IDifficultyBeatmap DifficultyBeatmap;
        public BeatmapLevelWrapper(IDifficultyBeatmap difficultyBeatmap)
        {
            DifficultyBeatmap = difficultyBeatmap ?? throw new ArgumentNullException(nameof(difficultyBeatmap), "difficultyBeatmap cannot be null.");
        }
        public string LevelID => DifficultyBeatmap.level.levelID;

        public string SongName => DifficultyBeatmap.level.songName;

        public string SongSubName => DifficultyBeatmap.level.songSubName;

        public string SongAuthorName => DifficultyBeatmap.level.songAuthorName;

        public string LevelAuthorName => DifficultyBeatmap.level.levelAuthorName;

        public float BeatsPerMinute => DifficultyBeatmap.level.beatsPerMinute;

        public float SongTimeOffset => DifficultyBeatmap.level.songTimeOffset;

        public float Shuffle => DifficultyBeatmap.level.shuffle;

        public float ShufflePeriod => DifficultyBeatmap.level.shufflePeriod;

        public float PreviewStartTime => DifficultyBeatmap.level.previewStartTime;

        public float PreviewDuration => DifficultyBeatmap.level.previewDuration;

        public float SongDuration => DifficultyBeatmap.level.songDuration;

        public Difficulty Difficulty => DifficultyBeatmap.difficulty.ToBeatmapDifficulty();

        public int DifficultyRank => DifficultyBeatmap.difficultyRank;

        public float NoteJumpMovementSpeed => DifficultyBeatmap.noteJumpMovementSpeed;

        public float NoteJumpStartBeatOffset => DifficultyBeatmap.noteJumpStartBeatOffset;
    }

    public enum Difficulty
    {
        Easy, Normal, Hard, Expert, ExpertPlus
    }

}

