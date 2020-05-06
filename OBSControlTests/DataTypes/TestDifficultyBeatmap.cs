using System;
using System.Collections.Generic;
using System.Text;
using OBSControl.Utilities;
using OBSControl.Wrappers;

namespace OBSControlTests.DataTypes
{
    public class TestDifficultyBeatmap : ILevelData
    {
        public static TestDifficultyBeatmap Default
        {
            get
            {
                return new TestDifficultyBeatmap();
            }
        }

        public string LevelID { get; set; } = "custom_level_ABC123";

        public string SongName { get; set; } = "TestSong";

        public string SongSubName { get; set; } = "TestSubname";

        public string SongAuthorName { get; set; } = "TestAuthor";

        public string LevelAuthorName { get; set; } = "TestMapper";

        public float BeatsPerMinute { get; set; } = 123.50f;

        public float SongTimeOffset { get; set; } = 5.33f;

        public float Shuffle { get; set; } = 1.5f;

        public float ShufflePeriod { get; set; } = 0.33f;

        public float PreviewStartTime { get; set; } = 30.34f;

        public float PreviewDuration { get; set; } = 30f;

        public float SongDuration { get; set; } = 180.32f;

        public Difficulty Difficulty { get; set; } = Difficulty.Expert;

        public int DifficultyRank => (int)Difficulty;

        public float NoteJumpMovementSpeed { get; set; } = 15.32f;

        public float NoteJumpStartBeatOffset { get; set; } = 1.99f;
    }
}
