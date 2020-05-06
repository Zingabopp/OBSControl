using OBSControl.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Utilities
{
    public interface ILevelData
    {
        string LevelID { get; }
        string SongName { get; }
        string SongSubName { get; }
        string SongAuthorName { get; }
        string LevelAuthorName { get; }
        float BeatsPerMinute { get; }
        float SongTimeOffset { get; }
        float Shuffle { get; }
        float ShufflePeriod { get; }
        float PreviewStartTime { get; }
        float PreviewDuration { get; }
        float SongDuration { get; }
        Difficulty Difficulty { get; }
        int DifficultyRank { get; }
        float NoteJumpMovementSpeed { get; }
        float NoteJumpStartBeatOffset { get; }
    }

}
