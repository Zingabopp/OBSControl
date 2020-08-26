using OBSControl.HarmonyPatches;
using OBSControl.Wrappers;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
#nullable enable

namespace OBSControl.OBSComponents
{
    public partial class RecordingController
    {
        /// <summary>
        /// Event handler for <see cref="StartLevelPatch.LevelStarting"/>.
        /// Sets a level start delay if using <see cref="RecordStartOption.LevelStartDelay"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLevelStarting(object sender, LevelStartingEventArgs e)
        {
            RecordStartOption recordStartOption = RecordStartOption;
            Logger.log?.Debug($"RecordingController OnLevelStarting. StartOption is {recordStartOption}");
            switch (recordStartOption)
            {
                case RecordStartOption.None:
                    break;
                case RecordStartOption.SceneSequence:
                    break;
                case RecordStartOption.SongStart:
                    break;
                case RecordStartOption.LevelStartDelay:
                    e.SetResponse(LevelStartingSourceName, (int)(RecordingStartDelay * 1000));
                    break;
                case RecordStartOption.Immediate:
                    break;
                default:
                    break;
            }
        }

        private async void OnLevelStart(object sender, LevelStartEventArgs e)
        {
            RecordStartOption recordStartOption = RecordStartOption;
            switch (e.StartResponseType)
            {
                case LevelStartResponse.None:
                    break;
                case LevelStartResponse.Immediate:
                    break;
                case LevelStartResponse.Delayed:
                    break;
                case LevelStartResponse.Handled:
                    if (recordStartOption == RecordStartOption.SceneSequence)
                        return;
                    break;
                default:
                    break;
            }
            Logger.log?.Debug($"RecordingController OnLevelStart. RecordStartOption: {RecordStartOption}.");
            if (recordStartOption == RecordStartOption.LevelStartDelay || recordStartOption == RecordStartOption.Immediate)
            {
                await TryStartRecordingAsync(RecordActionSourceType.Auto, recordStartOption, true).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Event handler for <see cref="SceneController.SceneStageChanged"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSceneStageChanged(object sender, SceneStageChangedEventArgs e)
        {
#if DEBUG
            Logger.log?.Debug($"RecordingController: OnSceneStageChanged - {e.SceneStage}.");
#endif
            e.AddCallback(SceneSequenceCallback);
        }
        #region Game Event Handlers

        /// <summary>
        /// Triggered after song ends, but before transition out of game scene.
        /// </summary>
        /// <param name="levelScenesTransitionSetupDataSO"></param>
        /// <param name="levelCompletionResults"></param>
        private async void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            Logger.log?.Debug($"RecordingController OnLevelFinished: {SceneManager.GetActiveScene().name}. RecordStopOption: {RecordStopOption}.");
            bool multipleLevelData = LastLevelData?.LevelResults != null || (LastLevelData?.MultipleLastLevels ?? false) == true;
            try
            {
                PlayerLevelStatsData? stats = null;
                IBeatmapLevel? levelInfo = GameStatus.LevelInfo;
                IDifficultyBeatmap? difficultyBeatmap = GameStatus.DifficultyBeatmap ?? LastLevelData?.LevelData?.DifficultyBeatmap;
                PlayerDataModel? playerData = OBSController.instance?.PlayerData;
                if (difficultyBeatmap != null)
                {
                    if (playerData != null && levelInfo != null)
                    {
                        stats = playerData.playerData.GetPlayerLevelStatsData(
                            levelInfo.levelID, difficultyBeatmap.difficulty, difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
                    }

                    LevelCompletionResultsWrapper levelResults = new LevelCompletionResultsWrapper(levelCompletionResults, stats?.playCount ?? 0, GameStatus.MaxModifiedScore);
                    RecordingData? recordingData = LastLevelData;
                    if (recordingData == null)
                    {
                        recordingData = new RecordingData(new BeatmapLevelWrapper(difficultyBeatmap), levelResults, stats)
                        {
                            MultipleLastLevels = multipleLevelData
                        };
                        LastLevelData = recordingData;
                    }
                    else
                    {
                        if (recordingData.LevelData == null)
                        {
                            recordingData.LevelData = new BeatmapLevelWrapper(difficultyBeatmap);
                        }
                        else if (recordingData.LevelData.DifficultyBeatmap != difficultyBeatmap)
                        {
                            Logger.log?.Debug($"Existing beatmap data doesn't match level completion beatmap data: '{recordingData.LevelData.SongName}' != '{difficultyBeatmap.level.songName}'");
                            recordingData.LevelData = new BeatmapLevelWrapper(difficultyBeatmap);
                        }
                        recordingData.LevelResults = levelResults;
                        recordingData.PlayerLevelStats = stats;
                        recordingData.MultipleLastLevels = multipleLevelData;
                    }
                }
                else
                    Logger.log?.Warn($"Beatmap data unavailable, unable to generate data for recording file rename.");

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error generating new file name: {ex}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            if (RecordStopOption == RecordStopOption.SongEnd)
            {
                try
                {
                    TimeSpan stopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                    if (stopDelay > TimeSpan.Zero)
                        await Task.Delay(stopDelay, RecordStopCancellationSource.Token);
                    StopRecordingTask = TryStopRecordingAsync();
                }
                catch (OperationCanceledException)
                {
                    Logger.log?.Debug($"Auto stop recording was canceled in 'OnLevelFinished'.");
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Exception auto stop recording in 'OnLevelFinished': {ex.Message}");
                    Logger.log?.Debug(ex);
                }
            }
        }

        private async void OnGameSceneActive()
        {
            WasInGame = true;
            Logger.log?.Debug($"RecordingController OnGameSceneActive. RecordStartOption: {RecordStartOption}.");
            StartCoroutine(GameStatusSetup());
            if (RecordStartOption == RecordStartOption.SongStart)
            {
                await TryStartRecordingAsync(RecordActionSourceType.Auto, RecordStartOption.SongStart, true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Triggered after transition out of game scene.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="_"></param>
        public async void OnLevelDidFinish()
        {
            if (!WasInGame) return;
            WasInGame = false;
            Logger.log?.Debug($"RecordingController OnLevelDidFinish: {SceneManager.GetActiveScene().name}. RecordStopOption: {RecordStopOption}.");
            try
            {
                if (RecordStopOption == RecordStopOption.ResultsView)
                {
                    TimeSpan stopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                    if (stopDelay > TimeSpan.Zero)
                        await Task.Delay(stopDelay, RecordStopCancellationSource.Token);
                    StopRecordingTask = TryStopRecordingAsync();
                }
            }
            catch (OperationCanceledException)
            {
                Logger.log?.Debug($"Auto stop recording was canceled in 'OnLevelFinished'.");
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Exception auto stop recording in 'OnLevelDidFinish': {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }
        #endregion


        #region OBS Event Handlers

        private async Task<string?> GetRecordingFileName()
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs != null)
            {
                try
                {
                    FileOutput? output = (FileOutput?)(await obs.ListOutputs().ConfigureAwait(false)).FirstOrDefault(o => o is FileOutput);
                    if (output != null)
                    {
                        Logger.log?.Debug($"Got FileOutput from OBS: {output.Name} | '{output.Settings.Path}'");
                        string? path = output?.Settings.Path;
                        if (!string.IsNullOrEmpty(path))
                        {
                            return Path.GetFileName(path);
                        }
                    }
                    else
                        Logger.log?.Warn($"Could not get file output from OBS.");
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error getting current recording file name: {ex.Message}.");
                    Logger.log?.Debug(ex);
                }
            }
            return null;
        }

        private async void OnObsRecordingStateChanged(object sender, OutputState type)
        {
            Logger.log?.Info($"Recording State Changed: {type}");
            OutputState = type;
            LastRecordingStateUpdate = DateTime.UtcNow;
            switch (type)
            {
                case OutputState.Starting:
                    recordingCurrentLevel = true;
                    break;
                case OutputState.Started:
                    RecordStartTime = DateTime.UtcNow;
                    recordingCurrentLevel = true;
                    if (RecordStartSource == RecordActionSourceType.None)
                    {
                        RecordStartSource = RecordActionSourceType.ManualOBS;
                        RecordStopOption recordStopOption = Plugin.config?.RecordStopOption ?? RecordStopOption.None;
                        RecordStopOption = recordStopOption == RecordStopOption.SceneSequence ? RecordStopOption.ResultsView : recordStopOption;
                    }
                    RecordStopCancellationSource = new CancellationTokenSource();
                    OBSWebsocket? obs = Obs.GetConnectedObs();
                    if (obs != null)
                    {
                        try
                        {
                            await obs.SetFilenameFormatting(DefaultFileFormat).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {

                            Logger.log?.Error($"Error setting default filename formatting: {ex.Message}.");
                            Logger.log?.Debug(ex);
                        }
                        if (string.IsNullOrEmpty(CurrentFileFormat))
                        {
                            string? path = await GetRecordingFileName().ConfigureAwait(false);
                            if (path != null)
                            {
                                if (string.IsNullOrEmpty(CurrentFileFormat))
                                {
                                    CurrentFileFormat = path;
                                    Logger.log?.Info($"Got currently recording filename from OBS: {path}");
                                }
                            }
                            else
                                Logger.log?.Warn($"CurrentFileFormat is null, unable to get the filename from OBS");
                        }

                    }
                    break;
                case OutputState.Stopping:
                    recordingCurrentLevel = false;
                    RecordStopCancellationSource.Cancel();
                    break;
                case OutputState.Stopped:
                    recordingCurrentLevel = false;
                    RecordStartTime = DateTime.MaxValue;
                    RecordingData? lastLevelData = LastLevelData;
                    string? renameOverride = RenameStringOverride;
                    RenameStringOverride = null;
                    LastLevelData = null;
                    RecordStartSource = RecordActionSourceType.None;
                    // RecordStartOption = RecordStartOption.None;
                    string? renameString = renameOverride ??
                        lastLevelData?.GetFilenameString(Plugin.config.RecordingFileFormat, Plugin.config.InvalidCharacterSubstitute, Plugin.config.ReplaceSpacesWith);
                    if (renameString != null)
                        RenameLastRecording(renameString);
                    else
                    {
                        Logger.log?.Info("No data to rename the recording file.");
                        CurrentFileFormat = null;
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
