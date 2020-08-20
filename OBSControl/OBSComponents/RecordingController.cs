using BS_Utils.Utilities;
using OBSControl.HarmonyPatches;
using OBSControl.Wrappers;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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

    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    [DisallowMultipleComponent]
    public class RecordingController : OBSComponent
    {
        //private OBSWebsocket? _obs => OBSController.instance?.GetConnectedObs();

        public const string LevelStartingSourceName = "RecordingController";
        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
        public const string DefaultDateTimeFormat = "yyyyMMddHHmmss";
        private SceneController? _sceneController;
        #region Options
        public bool AutoStopOnManual => Plugin.config?.AutoStopOnManual ?? true;
        /// <summary>
        /// True if delayed stop is enabled, does not affect SceneSequence recordings.
        /// </summary>
        public bool DelayedStopEnabled => RecordingStopDelay > 0;
        public bool DelayedStartEnabled => RecordingStartDelay > 0;

        public float RecordingStartDelay => Plugin.config?.LevelStartDelay ?? 0;

        public float RecordingStopDelay => Plugin.config?.RecordingStopDelay ?? 0;
        /// <summary>
        /// If not recording with SceneSequence, start recording when the song is started.
        /// </summary>
        public bool RecordOnSongStart => false;

        private RecordStartOption _recordStartOption;
        public RecordStartOption RecordStartOption => RecordStartOption.SongStart;

        private RecordStopOption _recordStopOption;
        public RecordStopOption RecordStopOption
        {
            get
            {
                return StopRecordAction switch
                {
                    RecordActionType.Auto => RecordStopOption.SceneSequence,
                    RecordActionType.NoAction => RecordStopOption.None,
                    RecordActionType.Immediate => _recordStopOption,
                    RecordActionType.None => _recordStopOption,
                    _ => _recordStopOption
                };
            }
            set
            {
                _recordStopOption = value;
            }
        }
        /// <summary>
        /// When should record stop be triggered, if at all.
        /// </summary>
        public RecordActionType StopRecordAction
        {
            get
            {
                return (RecordStartSource, AutoStopOnManual, DelayedStopEnabled) switch
                {
                    (RecordActionSourceType.Manual, true, true) => RecordActionType.Delayed,
                    (RecordActionSourceType.Manual, true, false) => RecordActionType.Immediate,
                    (RecordActionSourceType.Manual, false, _) => RecordActionType.NoAction,
                    (RecordActionSourceType.ManualOBS, true, true) => RecordActionType.Delayed,
                    (RecordActionSourceType.ManualOBS, true, false) => RecordActionType.Immediate,
                    (RecordActionSourceType.ManualOBS, false, _) => RecordActionType.NoAction,
                    (RecordActionSourceType.Auto, _, _) => RecordActionType.Auto,
                    (RecordActionSourceType.None, true, true) => RecordActionType.Delayed,
                    (RecordActionSourceType.None, true, false) => RecordActionType.Immediate,
                    (RecordActionSourceType.None, false, _) => RecordActionType.NoAction,
                    _ => RecordActionType.None

                };
            }
        }

        /// <summary>
        /// Directory OBS should record to.
        /// </summary>
        public string? RecordingFolder { get; protected set; }
        #endregion

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
        /// <summary>
        /// Time of the last recording state update (UTC) from the OBS OnRecordingStateChanged event.
        /// </summary>
        public DateTime LastRecordingStateUpdate { get; protected set; }
        public bool WaitingToStop { get; private set; }
        public Task? StopRecordingTask { get; private set; }

        /// <summary>
        /// Source that started current/last recording.
        /// </summary>
        public RecordActionSourceType RecordStartSource { get; protected set; }
        private string ToDateTimeFileFormat(DateTime dateTime)
        {
            return dateTime.ToString(DefaultDateTimeFormat);
        }

        public async Task TryStartRecordingAsync(RecordActionSourceType startType, string? fileFormat = null)
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

            if (fileFormat == null || fileFormat.Length == 0)
            {
                fileFormat = ToDateTimeFileFormat(DateTime.Now);
            }

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
                RecordStartSource = startType;
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

        public async Task TryStopRecordingAsync(string? renameTo = null)
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Error($"Unable to stop recording, OBSWebsocket is unavailable.");
                return;
            }
            try
            {
                WaitingToStop = true;
                RenameStringOverride = renameTo;
                await obs.StopRecording().ConfigureAwait(false);
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

        private string? RenameStringOverride;
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

        public IEnumerator<WaitUntil> GameStatusSetup()
        {
            // TODO: Limit wait by tries/current scene so it doesn't go forever.
            WaitUntil waitForData = new WaitUntil(() =>
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MenuCore")
                    return false;
                return BS_Utils.Plugin.LevelData.IsSet && GameStatus.GpModSO != null;
            });
            yield return waitForData;
            GameStatus.Setup();
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }

        public async Task<RecordingSettings> SetupRecording(string? fileFormat, string? outputDirectory, CancellationToken cancellationToken)
        {
            OBSWebsocket? obs = Obs.Obs;
            RecordingSettings settings = new RecordingSettings();
            if (obs == null || !obs.IsConnected)
            {
                Logger.log?.Error($"Unable to setup recording, OBSWebsocket is not connected.");
                return settings;
            }
            try
            {
                if (fileFormat != null && fileFormat.Length > 0)
                {
                    string? previousFileFormat = await obs.GetFilenameFormatting(cancellationToken).ConfigureAwait(false);
                    settings.PreviousFileFormat = previousFileFormat;
                    await obs.SetFilenameFormatting(fileFormat, cancellationToken);
                    settings.FileFormatSet = true;
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error setting up recording file format: {ex.Message}");
                Logger.log?.Debug(ex);

            }
            try
            {
                if (outputDirectory != null && outputDirectory.Length > 0)
                {
                    string? previousOutputDir = await obs.GetRecordingFolder(cancellationToken);
                    settings.PreviousOutputDirectory = previousOutputDir;
                    outputDirectory = Path.GetFullPath(outputDirectory);
                    Directory.CreateDirectory(outputDirectory);
                    await obs.SetRecordingFolder(outputDirectory).ConfigureAwait(false);
                    settings.OutputDirectorySet = true;
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error setting recording directory path: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            return settings;
        }

        public async Task<Output[]> GetOutputsAsync(CancellationToken cancellationToken = default)
        {
            OBSWebsocket? obs = Obs.Obs;
            if (obs == null || !obs.IsConnected)
            {
                Logger.log?.Error($"Unable to get output list, OBSWebsocket is not connected.");
                return Array.Empty<Output>();
            }
            try
            {
                Output[]? outputList = await obs.ListOutputs(cancellationToken).ConfigureAwait(false);
                if (outputList == null || outputList.Length == 0)
                    Logger.log?.Warn("No Outputs listed");
                return outputList ?? Array.Empty<Output>();
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting list of outputs: {ex.Message}");
                Logger.log?.Debug(ex);
                return Array.Empty<Output>();
            }
        }

        public async Task<string?> GetCurrentRecordFile(CancellationToken cancellationToken = default)
        {
            Output[] outputList = await GetOutputsAsync(cancellationToken).ConfigureAwait(false);
            FileOutput[] fileOutputs = outputList.Where(o => o is FileOutput).Select(fo => (FileOutput)fo).ToArray();
            if (fileOutputs.Length > 1)
            {
                FileOutput chosenOutput = fileOutputs.FirstOrDefault(f => f.Active && !string.IsNullOrEmpty(f.Settings.Path));
                if (chosenOutput != null)
                {
                    Logger.log?.Warn($"Multiple file outputs received: {string.Join(", ", fileOutputs.Select(f => f.Name))}, getting the first active one with a path.");
                    return chosenOutput.Settings.Path;
                }
                chosenOutput = fileOutputs.FirstOrDefault(f => !string.IsNullOrEmpty(f.Settings.Path));
                if (chosenOutput != null)
                {
                    Logger.log?.Warn($"Multiple file outputs received: {string.Join(", ", fileOutputs.Select(f => f.Name))}, getting the first one with a path.");
                    return chosenOutput.Settings.Path;
                }
                else
                    return null;
            }
            string? path = fileOutputs[0].Settings.Path;
            if (string.IsNullOrEmpty(path))
                return null;
            else
                return path;
        }

        protected RecordingData? LastLevelData;

        protected class RecordingData
        {
            public bool MultipleLastLevels;
            public PlayerLevelStatsData? PlayerLevelStats;
            public LevelCompletionResultsWrapper LevelResults;
            public BeatmapLevelWrapper LevelData;
            public RecordingData(LevelCompletionResultsWrapper levelResults, BeatmapLevelWrapper levelData, PlayerLevelStatsData? playerLevelStats)
            {
                LevelResults = levelResults;
                LevelData = levelData;
                PlayerLevelStats = playerLevelStats;
            }
            public string GetFilenameString(string? fileFormat, string? invalidSubstitute, string? spaceReplacement)
            {
                return Utilities.FileRenaming.GetFilenameString(fileFormat,
                        LevelData,
                        LevelResults,
                        invalidSubstitute,
                        spaceReplacement);
            }
        }
        private async void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            string? newFileName = null;
            bool multipleLevelData = LastLevelData != null || (LastLevelData?.MultipleLastLevels ?? false) == true;
            try
            {
                PlayerLevelStatsData? stats = null;
                if (OBSController.instance?.PlayerData != null && GameStatus.LevelInfo != null && GameStatus.DifficultyBeatmap != null)
                {
                    stats = OBSController.instance.PlayerData.playerData.GetPlayerLevelStatsData(
                        GameStatus.LevelInfo.levelID, GameStatus.DifficultyBeatmap.difficulty, GameStatus.DifficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
                }

                LevelCompletionResultsWrapper levelResults = new LevelCompletionResultsWrapper(levelCompletionResults, stats?.playCount ?? 0, GameStatus.MaxModifiedScore);
                if (GameStatus.DifficultyBeatmap != null)
                {
                    RecordingData recordingData = new RecordingData(levelResults, new BeatmapLevelWrapper(GameStatus.DifficultyBeatmap), stats)
                    {
                        MultipleLastLevels = multipleLevelData
                    };
                    LastLevelData = recordingData;
                    newFileName = recordingData.GetFilenameString(Plugin.config.RecordingFileFormat, Plugin.config.InvalidCharacterSubstitute, Plugin.config.ReplaceSpacesWith);
                }

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error generating new file name: {ex}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            if (StopRecordAction == RecordActionType.Immediate)
                StopRecordingTask = TryStopRecordingAsync(newFileName);
            else if (StopRecordAction == RecordActionType.Delayed)
            {
                TimeSpan stopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                if (stopDelay > TimeSpan.Zero)
                    await Task.Delay(stopDelay);
                await TryStopRecordingAsync(newFileName);
            }
        }

        #region OBS Event Handlers


        private void Obs_RecordingStateChanged(object sender, OutputState type)
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
                    recordingCurrentLevel = true;
                    Task.Run(() => Obs.GetConnectedObs()?.SetFilenameFormatting(DefaultFileFormat));
                    break;
                case OutputState.Stopping:
                    recordingCurrentLevel = false;
                    break;
                case OutputState.Stopped:
                    recordingCurrentLevel = false;
                    string? renameString = RenameStringOverride ??
                        LastLevelData?.GetFilenameString(Plugin.config.RecordingFileFormat, Plugin.config.InvalidCharacterSubstitute, Plugin.config.ReplaceSpacesWith);
                    if (renameString != null)
                        RenameLastRecording(renameString);
                    RenameStringOverride = null;
                    LastLevelData = null;
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
            StartLevelPatch.LevelStarting += OnLevelStarting;
        }


        protected override void RemoveEvents(OBSController obs)
        {
            if (obs == null) return;
            base.RemoveEvents(obs);
            obs.RecordingStateChanged -= Obs_RecordingStateChanged;
            obs.OBSComponentChanged -= OnOBSComponentChanged;
            StartLevelPatch.LevelStarting -= OnLevelStarting;
        }

        private void OnLevelStarting(object sender, LevelStartingEventArgs e)
        {
            Logger.log?.Debug($"RecordingController OnLevelStarting.");
            switch (RecordStartOption)
            {
                case RecordStartOption.None:
                    break;
                case RecordStartOption.SceneSequence:
                    break;
                case RecordStartOption.SongStart:
                    Logger.log?.Debug($"RecordingController OnLevelStarting: Setting SongStart event.");
                    BSEvents.gameSceneLoaded -= OnGameSceneActive;
                    BSEvents.gameSceneLoaded += OnGameSceneActive;
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

        private async void OnGameSceneActive()
        {
            //Logger.log?.Debug($"RecordingController OnGameSceneActive.");
            //var timeControllers = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>();
            //var songControllers = Resources.FindObjectsOfTypeAll<SongController>();
            //if (songControllers.Length > 1)
            //    Logger.log?.Error($"{songControllers.Length} SongControllers exist.");
            //var songController = songControllers?.FirstOrDefault();
            //var pauseController = Resources.FindObjectsOfTypeAll<GamePause>().FirstOrDefault();
            //if (songController != null)
            //{
            //    await Task.Delay(500);
            //    songController.StopSong();
            //    Logger.log?.Debug($"RecordingController song stopped? delaying by 10s.");
            //    await Task.Delay(10000);
            //    songController.StartSong();
            //}
            //else
            //    Logger.log?.Debug("timeController is null.");
        }

        private async Task SceneSequenceCallback(SceneStage sceneStage)
        {
            Logger.log?.Debug($"RecordingController: SceneStage - {sceneStage}.");
            if (sceneStage == SceneStage.IntroStarted)
            {
                await TryStartRecordingAsync(RecordActionSourceType.Auto);
            }
            else if (sceneStage == SceneStage.Game)
                StartCoroutine(GameStatusSetup());
            else if (sceneStage == SceneStage.OutroFinished)
            {
                //TimeSpan recordStopDelay = TimeSpan.FromSeconds(Plugin.config?.RecordingStopDelay ?? 0);
                //if (recordStopDelay > TimeSpan.Zero)
                //    await Task.Delay(recordStopDelay); // TODO: this also forces scene sequence to wait, do I even need this with the scene settings?
                if (StopRecordAction == RecordActionType.Auto)
                    await TryStopRecordingAsync();
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

    public struct RecordingSettings
    {
        public static RecordingSettings None => new RecordingSettings();

        public string? PreviousOutputDirectory;
        public bool OutputDirectorySet;

        public string? PreviousFileFormat;
        public bool FileFormatSet;
    }
}
