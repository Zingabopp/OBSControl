using OBSControl.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace OBSControl.OBSComponents
{
    public enum RecordActionSourceType
    {
        /// <summary>
        /// No information on recording action.
        /// </summary>
        None = 0,
        /// <summary>
        /// Recording started by OBS.
        /// </summary>
        ManualOBS = 1,
        /// <summary>
        /// Recording started manually from OBSControl.
        /// </summary>
        Manual = 2,
        /// <summary>
        /// Recording start/stop automatically.
        /// </summary>
        Auto = 3
    }

    public enum RecordActionType
    {
        /// <summary>
        /// No information on recording action.
        /// </summary>
        None = 0,
        /// <summary>
        /// Recording should be stopped only manually.
        /// </summary>
        NoAction = 1,
        /// <summary>
        /// Recording should be start/stopped immediately.
        /// </summary>
        Immediate = 2,
        /// <summary>
        /// Recording should be start/stopped after a delay.
        /// </summary>
        Delayed = 3,
        /// <summary>
        /// Recording should be stopped automatically (by SceneSequence callback).
        /// </summary>
        Auto = 4
    }

    public enum RecordStartOption
    {
        /// <summary>
        /// Recording will not be auto started
        /// </summary>
        None = 0,
        /// <summary>
        /// Recording starts when triggered by SceneSequence.
        /// </summary>
        SceneSequence = 2,
        /// <summary>
        /// Recording will be started in GameCore at the start of the song.
        /// </summary>
        SongStart = 3,
        /// <summary>
        /// Level start will begin after recording starts and a delay.
        /// </summary>
        LevelStartDelay = 4,
        /// <summary>
        /// Recording will be started immediately when LevelStarting is triggered.
        /// </summary>
        Immediate = 5
    }

    public enum RecordStopOption
    {
        /// <summary>
        /// Recording will not be auto stopped
        /// </summary>
        None = 0,
        /// <summary>
        /// Recording stopped when triggered by SceneSequence.
        /// </summary>
        SceneSequence = 2,
        /// <summary>
        /// Recording will be stopped based on when the song ends (paired with stop delay).
        /// </summary>
        SongEnd = 3,
        /// <summary>
        /// Recording will be stopped based on when the results view is presented (paired with stop delay).
        /// </summary>
        ResultsView = 4
    }




    public partial class RecordingController
    {
        protected class RecordingData
        {
            public bool MultipleLastLevels;
            public BeatmapLevelWrapper LevelData;
            public PlayerLevelStatsData? PlayerLevelStats;
            public LevelCompletionResultsWrapper? LevelResults;
            public RecordingData(BeatmapLevelWrapper levelData, PlayerLevelStatsData? playerLevelStats = null)
            {
                LevelData = levelData;
                PlayerLevelStats = playerLevelStats;
            }
            public RecordingData(BeatmapLevelWrapper levelData, LevelCompletionResultsWrapper? levelResults, PlayerLevelStatsData? playerLevelStats)
            {
                LevelResults = levelResults;
                LevelData = levelData;
                PlayerLevelStats = playerLevelStats;
            }
            public string? GetFilenameString(string? fileFormat, string? invalidSubstitute, string? spaceReplacement)
            {
                // TODO: Handle MultipleLastLevels?
                if (LevelData == null)
                    return null;
                return Utilities.FileRenaming.GetFilenameString(fileFormat,
                        LevelData,
                        LevelResults,
                        invalidSubstitute,
                        spaceReplacement);
            }
        }

        public struct RecordingSettings
        {
            public static RecordingSettings None => new RecordingSettings();

            public string? PreviousOutputDirectory;
            public bool OutputDirectorySet;

            public string? PreviousFileFormat;
            public bool FileFormatSet;
        }
    }
}
