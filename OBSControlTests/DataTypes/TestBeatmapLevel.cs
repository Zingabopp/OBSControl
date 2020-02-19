extern alias BeatSaber;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BeatSaber;

namespace OBSControlTests.DataTypes
{
    public class TestBeatmapLevel : IBeatmapLevel
    {
        public static TestBeatmapLevel Default
        {
            get
            {
                return new TestBeatmapLevel()
                {
                    beatsPerMinute = 100f,
                    levelAuthorName = "TestMapper",
                    levelID = "custom_level_ABC123",
                    songAuthorName = "TestAuthor",
                    songDuration = 63f,
                    songName = "TestSong",
                    songSubName = "TestSubname"
                };
            }
        }
        public IBeatmapLevelData beatmapLevelData => throw new NotImplementedException();

        public string levelID { get; set; }

        public string songName { get; set; }

        public string songSubName { get; set; }

        public string songAuthorName { get; set; }

        public string levelAuthorName { get; set; }

        public float beatsPerMinute { get; set; }

        public float songTimeOffset { get; set; }

        public float shuffle { get; set; }

        public float shufflePeriod { get; set; }

        public float previewStartTime { get; set; }

        public float previewDuration { get; set; }

        public float songDuration { get; set; }

        public EnvironmentInfoSO environmentInfo => throw new NotImplementedException();

        public EnvironmentInfoSO allDirectionsEnvironmentInfo => throw new NotImplementedException();

        public PreviewDifficultyBeatmapSet[] previewDifficultyBeatmapSets => throw new NotImplementedException();

        public System.Threading.Tasks.Task<UnityEngine.Texture2D> GetCoverImageTexture2DAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<UnityEngine.AudioClip> GetPreviewAudioClipAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
