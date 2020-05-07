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
        private readonly IDifficultyBeatmap _difficultyBeatmap;
        public BeatmapLevelWrapper(IDifficultyBeatmap difficultyBeatmap)
        {
            _difficultyBeatmap = difficultyBeatmap ?? throw new ArgumentNullException(nameof(difficultyBeatmap), "difficultyBeatmap cannot be null.");
        }
        public string LevelID => _difficultyBeatmap.level.levelID;

        public string SongName => _difficultyBeatmap.level.songName;

        public string SongSubName => _difficultyBeatmap.level.songSubName;

        public string SongAuthorName => _difficultyBeatmap.level.songAuthorName;

        public string LevelAuthorName => _difficultyBeatmap.level.levelAuthorName;

        public float BeatsPerMinute => _difficultyBeatmap.level.beatsPerMinute;

        public float SongTimeOffset => _difficultyBeatmap.level.songTimeOffset;

        public float Shuffle => _difficultyBeatmap.level.shuffle;

        public float ShufflePeriod => _difficultyBeatmap.level.shufflePeriod;

        public float PreviewStartTime => _difficultyBeatmap.level.previewStartTime;

        public float PreviewDuration => _difficultyBeatmap.level.previewDuration;

        public float SongDuration => _difficultyBeatmap.level.songDuration;

        public Difficulty Difficulty => _difficultyBeatmap.difficulty.ToBeatmapDifficulty();

        public int DifficultyRank => _difficultyBeatmap.difficultyRank;

        public float NoteJumpMovementSpeed => _difficultyBeatmap.noteJumpMovementSpeed;

        public float NoteJumpStartBeatOffset => _difficultyBeatmap.noteJumpStartBeatOffset;
    }

    public enum Difficulty
    {
        Easy, Normal, Hard, Expert, ExpertPlus
    }

}

