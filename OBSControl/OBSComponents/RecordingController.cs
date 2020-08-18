using OBSControl.HarmonyPatches;
using OBSControl.Wrappers;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
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

    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    [DisallowMultipleComponent]
    public class RecordingController : OBSComponent
    {
        //private OBSWebsocket? _obs => OBSController.instance?.GetConnectedObs();

        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
        public const string DefaultDateTimeFormat = "yyyyMMddHHmmss";
        private SceneController? _sceneController;
        protected SceneController? SceneController
        {
            get => _sceneController;
            set
            {
                Logger.log?.Debug($"Setting SceneController{(value == null ? " to <NULL>" : "")}.");
                if (value == _sceneController) return;
                if (_sceneController != null)
                {
                    _sceneController.SceneStageChanged -= OnSceneStageChanged;
                }
                _sceneController = value;
                if (_sceneController != null)
                {
#if DEBUG
                    Logger.log?.Debug($"RecordingController: Connected to SceneController.");
#endif
                    _sceneController.SceneStageChanged -= OnSceneStageChanged;
                    _sceneController.SceneStageChanged += OnSceneStageChanged;
                }
            }
        }
        public OutputState OutputState { get; protected set; }
        public bool WaitingToStop { get; private set; }
        public Task? StopRecordingTask { get; private set; }
        public RecordActionType StopRecordAction
        {
            get
            {
                PluginConfig config = Plugin.config;
                bool stopOnManual = config?.AutoStopOnManual ?? true;
                bool delayedStop = (config?.RecordingStopDelay ?? 0) > 0;
                switch (RecordStopType)
                {
                    case RecordActionSourceType.None:
                        break;
                    case RecordActionSourceType.ManualOBS:
                        break;
                    case RecordActionSourceType.Manual:
                        break;
                    case RecordActionSourceType.Auto:
                        break;
                    default:
                        break;
                }
                return (RecordStopType, stopOnManual, delayedStop) switch
                {
                    (RecordActionSourceType.Manual, true, true) => RecordActionType.Delayed,
                    (RecordActionSourceType.Manual, true, false) => RecordActionType.Immediate,
                    (RecordActionSourceType.Manual, false, _) => RecordActionType.NoAction,

                };
            }
        }
        public RecordActionSourceType RecordStartType { get; protected set; }
        public RecordActionSourceType RecordStopType { get; protected set; }
        private string ToDateTimeFileFormat(DateTime dateTime)
        {
            return dateTime.ToString(DefaultDateTimeFormat);
        }

        public string? RecordingFolder { get; protected set; }
        public async Task TryStartRecordingAsync(string fileFormat = DefaultFileFormat)
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            Logger.log?.Debug($"TryStartRecording");
            if (obs == null)
            {
                Logger.log?.Error($"Unable to start recording, obs instance not found.");
                return;
            }
            try
            {
                RecordingFolder = await obs.GetRecordingFolder().ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting recording folder from OBS: {ex.Message}");
                Logger.log?.Debug(ex);
                return;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            int tries = 0;
            string? currentFormat = null;
            do
            {
                if (tries > 0)
                {
                    Logger.log?.Debug($"({tries}) Failed to set OBS's FilenameFormatting to {fileFormat} retrying in 50ms");
                    await Task.Delay(50);
                }
                tries++;
                try
                {
                    await obs.SetFilenameFormatting(fileFormat).ConfigureAwait(false);
                    currentFormat = await obs.GetFilenameFormatting().ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error getting current filename format from OBS: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
            } while (currentFormat != fileFormat && tries < 10);
            CurrentFileFormat = fileFormat;
            try
            {
                await obs.StartRecording().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error starting recording in OBS: {ex.Message}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public async Task<string[]> GetAvailableScenes()
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Error($"Unable to get scenes, obs instance is null.");
                return Array.Empty<string>();
            }
            try
            {
                return (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error validating scenes: {ex.Message}");
                Logger.log?.Debug(ex);
                return Array.Empty<string>();
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public bool ValidateScenes(IEnumerable<string> availableScenes, params string[] scenes)
        {
            if (availableScenes == null || scenes == null || scenes.Length == 0)
                return false;
            bool valid = true;
            foreach (var scene in availableScenes)
            {
                if (string.IsNullOrEmpty(scene))
                {
                    valid = false;
                    continue;
                }
                else if (!availableScenes.Contains(scene))
                {
                    valid = false;
                    Logger.log?.Warn($"Scene '{scene}' is not available.");
                    continue;
                }

            }
            return valid;
        }

        public async Task<bool> ValidateScenesAsync(params string[] scenes)
        {
            try
            {
                string[] availableScenes = await GetAvailableScenes().ConfigureAwait(false);
                Logger.log?.Debug($"Available scenes: {string.Join(", ", availableScenes)}");
                return scenes.All(s => availableScenes.Contains(s));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error validating scenes: {ex.Message}");
                Logger.log?.Debug(ex);
                return false;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private string? CurrentFileFormat { get; set; }

        public async Task TryStopRecordingAsync(string? renameTo, bool stopImmediate = false)
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Error($"Unable to stop recording, obs instance is null.");
                return;
            }
            string endScene = Plugin.config.EndSceneName ?? string.Empty;
            string gameScene = Plugin.config.GameSceneName ?? string.Empty;
            string restingScene = Plugin.config.RestingSceneName ?? string.Empty;
            string[] availableScenes = await GetAvailableScenes().ConfigureAwait(false);
            if (!availableScenes.Contains(endScene))
                endScene = string.Empty;
            if (!availableScenes.Contains(gameScene))
                gameScene = string.Empty;
            if (!ValidateScenes(availableScenes, restingScene))
                restingScene = gameScene;
            bool validOutro = ValidateScenes(availableScenes, endScene, gameScene);

            try
            {
                WaitingToStop = true;
                RenameString = renameTo;
                float delay = Plugin.config.RecordingStopDelay;
                if (!stopImmediate)
                {
                    if (delay > 0)
                        await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);

                    if (validOutro)
                    {
                        Logger.log?.Info($"Setting outro OBS scene to '{endScene}'");
                        await obs.SetCurrentScene(endScene);
                        await Task.Delay(TimeSpan.FromSeconds(Plugin.config.EndSceneDuration));
                    }
                }
                await obs.StopRecording().ConfigureAwait(false);
                if (!stopImmediate && validOutro)
                {
                    await Task.Delay(100).ConfigureAwait(false); // To ensure recording has fully stopped.
                    Logger.log?.Info($"Setting game OBS scene to '{restingScene}'");
                    await obs.SetCurrentScene(restingScene).ConfigureAwait(false);
                }
                recordingCurrentLevel = false;
            }
            catch (ErrorResponseException ex)
            {
                Logger.log?.Error($"Error trying to stop recording: {ex.Message}");
                if (ex.Message != "recording not active")
                    Logger.log?.Debug(ex);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Unexpected exception trying to stop recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                WaitingToStop = false;
                StopRecordingTask = null;
            }
        }

        private string? RenameString;
        public void RenameLastRecording(string? newName)
        {
            if (newName == null)
            {
                Logger.log?.Warn($"Unable to rename last recording, provided new name is null.");
                return;
            }
            if (newName.Length == 0)
            {
                Logger.log?.Info($"Skipping file rename, no RecordingFileFormat provided.");
                return;
            }
            string? recordingFolder = RecordingFolder;
            string? fileFormat = CurrentFileFormat;
            CurrentFileFormat = null;
            if (string.IsNullOrEmpty(recordingFolder))
            {
                Logger.log?.Warn($"Unable to determine current recording folder, unable to rename.");
                return;
            }
            if (string.IsNullOrEmpty(fileFormat))
            {
                Logger.log?.Warn($"Last recorded filename not stored, unable to rename.");
                return;
            }

            DirectoryInfo directory = new DirectoryInfo(recordingFolder);
            if (!directory.Exists)
            {
                Logger.log?.Warn($"Recording directory doesn't exist, unable to rename.");
                return;
            }
            FileInfo targetFile = directory.GetFiles(fileFormat + "*").OrderByDescending(f => f.CreationTimeUtc).FirstOrDefault();
            if (targetFile == null)
            {
                Logger.log?.Warn($"Couldn't find recorded file, unable to rename.");
                return;
            }
            string fileName = targetFile.Name.Substring(0, targetFile.Name.LastIndexOf('.'));
            string fileExtension = targetFile.Extension;
            Logger.log?.Info($"Attempting to rename {fileFormat}{fileExtension} to {newName} with an extension of {fileExtension}");
            string newFile = newName + fileExtension;
            int index = 2;
            while (File.Exists(Path.Combine(directory.FullName, newFile)))
            {
                Logger.log?.Debug($"File exists: {Path.Combine(directory.FullName, newFile)}");
                newFile = newName + $"({index})" + fileExtension;
                index++;
            }
            try
            {
                Logger.log?.Debug($"Attempting to rename to '{newFile}'");
                targetFile.MoveTo(Path.Combine(directory.FullName, newFile));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Unable to rename {targetFile.Name} to {newFile}: {ex.Message}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public bool recordingCurrentLevel;

        public async Task StartRecordingLevel()
        {
            string fileFormat = ToDateTimeFileFormat(DateTime.Now);
            Logger.log?.Debug($"Starting recording, file format: {fileFormat}");
            await TryStartRecordingAsync(fileFormat);
        }

        public IEnumerator<WaitUntil> GameStatusSetup()
        {
            // TODO: Limit wait by tries/current scene so it doesn't go forever.
            WaitUntil waitForData = new WaitUntil(() =>
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MenuCore")
                    return false;
                return !(!BS_Utils.Plugin.LevelData.IsSet || GameStatus.GpModSO == null);
            });
            yield return waitForData;
            GameStatus.Setup();
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }

        private async void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            string? newFileName = null;
            try
            {
                PlayerLevelStatsData? stats = null;
                if (OBSController.instance?.PlayerData != null && GameStatus.LevelInfo != null && GameStatus.DifficultyBeatmap != null)
                {
                    stats = OBSController.instance.PlayerData.playerData.GetPlayerLevelStatsData(
                        GameStatus.LevelInfo.levelID, GameStatus.DifficultyBeatmap.difficulty, GameStatus.DifficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
                }

                LevelCompletionResultsWrapper resultsWrapper = new Wrappers.LevelCompletionResultsWrapper(levelCompletionResults, stats?.playCount ?? 0, GameStatus.MaxModifiedScore);
                if (GameStatus.DifficultyBeatmap != null)
                {
                    newFileName = Utilities.FileRenaming.GetFilenameString(Plugin.config.RecordingFileFormat,
                        new BeatmapLevelWrapper(GameStatus.DifficultyBeatmap),
                        resultsWrapper,
                        Plugin.config.InvalidCharacterSubstitute,
                        Plugin.config.ReplaceSpacesWith);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error generating new file name: {ex}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            if (ShouldStopRecordingImmediate())
                StopRecordingTask = TryStopRecordingAsync(newFileName, true);
            else
            {
                TimeSpan stopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                if (stopDelay > TimeSpan.Zero)
                    await Task.Delay(stopDelay);
                await TryStopRecordingAsync(newFileName, false);
            }
        }

        #region OBS Event Handlers

        public bool ShouldStopRecordingImmediate()
        {
            bool stopImmediate = Plugin.config.RecordingStopDelay <= 0;

            stopImmediate = StopRecordAction switch
            {
                RecordActionType.None => false,
                RecordActionType.NoAction => Plugin.config?.AutoStopOnManual ?? true,
                RecordActionType.Immediate => true,
                RecordActionType.Auto => true,
                _ => true
            };

            return stopImmediate;
        }

        private void Obs_RecordingStateChanged(object sender, OutputState type)
        {
            Logger.log?.Info($"Recording State Changed: {type}");
            OutputState = type;
            switch (type)
            {
                case OutputState.Starting:
                    recordingCurrentLevel = true;
                    break;
                case OutputState.Started:
                    recordingCurrentLevel = true;
                    Task.Run(() => Obs.GetConnectedObs()?.SetFilenameFormatting(DefaultFileFormat));
                    break;
                case OutputState.Stopping:
                    recordingCurrentLevel = false;
                    break;
                case OutputState.Stopped:
                    recordingCurrentLevel = false;
                    RenameLastRecording(RenameString);
                    RenameString = null;
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Setup/Teardown


        private void OnSceneStageChanged(object sender, SceneStageChangedEventArgs e)
        {
#if DEBUG
            Logger.log?.Debug($"RecordingController: OnSceneStageChanged - {e.SceneStage}.");
#endif
            e.AddCallback(SceneSequenceCallback);
        }
        private void OnOBSComponentChanged(object sender, OBSComponentChangedEventArgs e)
        {
            if (e.AddedComponent is SceneController addedSceneController)
            {
                SceneController = addedSceneController;
            }
            if (e.RemovedComponent is SceneController removedSceneController
                && removedSceneController == SceneController)
            {
                SceneController = null;
            }
        }

        public override async Task InitializeAsync(OBSController obs)
        {
            await base.InitializeAsync(obs).ConfigureAwait(false);
            SceneController = obs.GetOBSComponent<SceneController>();
        }

        protected override void SetEvents(OBSController obs)
        {
            if (obs == null) return;
            base.SetEvents(obs);

            obs.RecordingStateChanged += Obs_RecordingStateChanged;
            obs.OBSComponentChanged += OnOBSComponentChanged;
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
            StartLevelPatch.LevelStarting += OnLevelStarting;
        }


        protected override void RemoveEvents(OBSController obs)
        {
            if (obs == null) return;
            base.RemoveEvents(obs);
            obs.RecordingStateChanged -= Obs_RecordingStateChanged;
            obs.OBSComponentChanged -= OnOBSComponentChanged;
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            StartLevelPatch.LevelStarting -= OnLevelStarting;
        }

        private void OnLevelStarting(object sender, LevelStartEventArgs e)
        {
            Logger.log?.Debug($"RecordingController OnLevelStarting.");
            e.SetResponse(LevelStartResponse.Delayed);
        }

        private async Task SceneSequenceCallback(SceneStage sceneStage)
        {
            Logger.log?.Debug($"RecordingController: SceneStage - {sceneStage}.");
            if (sceneStage == SceneStage.Intro)
            {
                await StartRecordingLevel();
            }
            else if (sceneStage == SceneStage.Game)
                StartCoroutine(GameStatusSetup());
            else if (sceneStage == SceneStage.Outro)
            {
                TimeSpan recordStopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                if (recordStopDelay > TimeSpan.Zero)
                    await Task.Delay(recordStopDelay); // TODO: this also forces scene sequence to wait, do I even need this with the scene settings?
                await TryStopRecordingAsync(RenameString, false);
            }
        }

        protected override void SetEvents(OBSWebsocket obs)
        {
        }
        protected override void RemoveEvents(OBSWebsocket obs)
        {
        }
        #endregion

        #region Monobehaviour Messages
        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            SetEvents(Obs);
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            RemoveEvents(Obs);
        }
        #endregion
    }
}
