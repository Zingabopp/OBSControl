using OBS.WebSocket.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OBSControl
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class RecordingController
    {

        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
        public void TryStartRecording(string fileFormat = DefaultFileFormat)
        {
            Task.Run(async () =>
            {
                Logger.log.Debug($"TryStartRecording");
                RecordingFolder = obs.Api.GetRecordingFolder();
                obs.Api.SetFilenameFormatting(fileFormat);
                int tries = 1;
                string currentFormat = obs.Api.GetFilenameFormatting();
                while (currentFormat != fileFormat && tries < 10)
                {
                    Logger.log.Debug($"({tries})Failed to set OBS's FilenameFormatting to {fileFormat} retrying in 50ms");
                    tries++;
                    await Task.Delay(50);
                    obs.Api.SetFilenameFormatting(fileFormat);
                    currentFormat = obs.Api.GetFilenameFormatting();
                }
                CurrentFileFormat = fileFormat;
                obs.Api.StartRecording();
                obs.Api.SetFilenameFormatting(DefaultFileFormat);
            });
        }

        private string CurrentFileFormat;

        public void TryStopRecording(string renameTo = "")
        {
            Task.Run(() =>
            {
                obs.Api.StopRecording();
            });
        }

        public void AppendLastRecordingName(string suffix)
        {
            string recordingFolder = RecordingFolder;
            string fileFormat = CurrentFileFormat;
            if (string.IsNullOrEmpty(recordingFolder))
            {
                Logger.log.Warn($"Unable to determine current recording folder, unable to rename.");
                return;
            }
            if (string.IsNullOrEmpty(fileFormat))
            {
                Logger.log.Warn($"Recorded filename not stored, unable to rename.");
                return;
            }

            DirectoryInfo directory = new DirectoryInfo(recordingFolder);
            if (!directory.Exists)
            {
                Logger.log.Warn($"Recording directory doesn't exist, unable to rename.");
                return;
            }
            var targetFile = directory.GetFiles(fileFormat + "*").OrderByDescending(f => f.CreationTimeUtc).FirstOrDefault();
            if (targetFile == null)
            {
                Logger.log.Warn($"Couldn't find recorded file, unable to rename.");
                return;
            }
            string fileName = targetFile.Name.Substring(0, targetFile.Name.LastIndexOf('.'));
            var fileExtension = targetFile.Extension;
            Logger.log.Info($"Attempting to append {suffix} to {fileFormat} with an extension of {fileExtension}");
            string newFile = fileFormat + suffix + fileExtension;
            int index = 2;
            while (File.Exists(Path.Combine(directory.FullName, newFile)))
            {
                Logger.log.Debug($"File exists: {Path.Combine(directory.FullName, newFile)}");
                newFile = fileFormat + suffix + $"({index})" + fileExtension;
                index++;
            }
            try
            {
                Logger.log.Debug($"Attempting to rename to '{newFile}'");
                targetFile.MoveTo(Path.Combine(directory.FullName, newFile));
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Unable to rename {targetFile.Name} to {newFile}: {ex.Message}");
                Logger.log.Debug(ex);
            }
        }

        public bool recordingCurrentLevel;
        public IEnumerator<WaitUntil> GetFileFormat(IBeatmapLevel level = null)
        {
            Logger.log.Debug("Trying to get the file format information for this level");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            yield return new WaitUntil(() =>
            {
                Logger.log.Debug("GetFileFormat: LevelInfo is null");
                if (level == null)
                    level = BS_Utils.Plugin.LevelData?.GameplayCoreSceneSetupData?.difficultyBeatmap?.level;
                return (level != null || timer.ElapsedMilliseconds > 400);
            });
            string fileFormat = DefaultFileFormat;
            if (level != null)
            {
                fileFormat = $"{level.songName}-{level.levelAuthorName}";
                CurrentFileFormat = fileFormat;
            }
            else
            {
                Logger.log.Warn("Couldn't get level info, using default recording file format");
                CurrentFileFormat = string.Empty;
            }
            Logger.log.Debug($"Starting recording, file format: {fileFormat}");
            TryStartRecording(fileFormat);
        }

        public IEnumerator<WaitUntil> GameStatusSetup()
        {
            // TODO: Limit wait by tries/current scene so it doesn't go forever.
            yield return new WaitUntil(() =>
            {
                return !(!BS_Utils.Plugin.LevelData.IsSet || GameStatus.GpModSO == null) || (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MenuCore");
            });
            GameStatus.Setup();
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }


        private void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            appendText.Clear();
            try
            {
                Logger.log.Debug($"Max modified score is {GameStatus.MaxModifiedScore}");
                float scorePercent = ((float)levelCompletionResults.rawScore / GameStatus.MaxModifiedScore) * 100f;
                string scoreStr = scorePercent.ToString("F3");
                appendText.Append($"-{scoreStr.Substring(0, scoreStr.Length - 1)}");
                PlayerLevelStatsData stats = PlayerData.playerData.GetPlayerLevelStatsData(
                    GameStatus.LevelInfo.levelID, GameStatus.difficultyBeatmap.difficulty, GameStatus.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
                if (stats.playCount == 0)
                    appendText.Append("-1st");
                else
                    Logger.log.Debug($"PlayCount for {GameStatus.LevelInfo.levelID} is {stats.playCount}");
                if (levelCompletionResults.fullCombo)
                    appendText.Append("-FC");

                if (levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared)
                {

                    if (levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Quit ||
                        levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Restart)
                        appendText.Append("-QUIT");
                    else
                        appendText.Append("-FAILED");
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error appending file name: {ex}");
                Logger.log.Debug(ex);
            }
            TryStopRecording();
            recordingCurrentLevel = false;
        }

        #region OBS Event Handlers

        private void Obs_RecordingStateChanged(ObsWebSocket sender, OBS.WebSocket.NET.Types.OutputState type)
        {
            Logger.log.Info($"Recording State Changed: {type.ToString()}");
            switch (type)
            {
                case OBS.WebSocket.NET.Types.OutputState.Starting:
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Started:
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Stopping:
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Stopped:
                    var toAppend = appendText.ToString();
                    if (!string.IsNullOrEmpty(toAppend))
                    {
                        AppendLastRecordingName(toAppend);
                        appendText.Clear();
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion


        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {

        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {

        }

        /// <summary>
        /// Called every frame if the script is enabled.
        /// </summary>
        private void Update()
        {

        }

        /// <summary>
        /// Called every frame after every other enabled script's Update().
        /// </summary>
        private void LateUpdate()
        {

        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {

        }
        #endregion
    }
}
