using OBSControl.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Utilities
{

    public class thing
    {
        public void thingy(IBeatmapLevel level, ILevelCompletionResults results)
        {
            
        }
    }
    public interface ILevelData
    {
        string levelID { get; }
        string songName { get; }
        string songSubName { get; }
        string songAuthorName { get; }
        string levelAuthorName { get; }
        float beatsPerMinute { get; }
        float songTimeOffset { get; }
        float shuffle { get; }
        float shufflePeriod { get; }
        float previewStartTime { get; }
        float previewDuration { get; }
        float songDuration { get; }
        Difficulty difficulty { get; }
        int difficultyRank { get; }
        float noteJumpMovementSpeed { get; }
        float noteJumpStartBeatOffset { get; }
    }

}
