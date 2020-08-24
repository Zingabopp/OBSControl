using BS_Utils.Utilities;
using IPA.Utilities;
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
    [DisallowMultipleComponent]
    public partial class RecordingController : OBSComponent
    {
        //private OBSWebsocket? _obs => OBSController.instance?.GetConnectedObs();
        internal readonly HarmonyPatchInfo ReadyToStartPatch = HarmonyManager.GetReadyToStartPatch();
        public const string LevelStartingSourceName = "RecordingController";
        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
        public const string DefaultDateTimeFormat = "yyyyMMddHHmmss";
        #region Encapsulated Fields
        private SceneController? _sceneController;
        private RecordingData? _lastLevelData;
        private RecordStopOption _recordStopOption;
        private RecordStartOption _recordStartOption;

        #endregion
        public bool recordingCurrentLevel;
        private bool validLevelData;
        private string? RenameStringOverride;

        // private static readonly FieldAccessor<AudioTimeSyncController, float>.Accessor SyncControllerTimeScale = FieldAccessor<AudioTimeSyncController, float>.GetAccessor("_timeScale");
        #region Properties

        private string? CurrentFileFormat { get; set; }

        /// <summary>
        /// Data about the last level played. If not null when a level is finished, <see cref="RecordingData.MultipleLastLevels"/> will be set to true.
        /// Should be set to null after it's used to rename a recording.
        /// </summary>
        protected RecordingData? LastLevelData
        {
            get => validLevelData ? _lastLevelData : null;
            set
            {
                validLevelData = _lastLevelData == null;
                _lastLevelData = value;
            }
        }
        public OutputState RecordingState { get; private set; }
        public DateTime RecordStartTime { get; private set; }
        #endregion

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

        public RecordStartOption RecordStartOption
        {
            get => _recordStartOption;
            set => _recordStartOption = value;
        }

        public RecordStopOption RecordStopOption
        {
            get
            {
                return AutoStop ? _recordStopOption : RecordStopOption.None;
            }
            set
            {
                _recordStopOption = value;
            }
        }
        /// <summary>
        /// When should record stop be triggered, if at all.
        /// </summary>
        public bool AutoStop
        {
            get
            {
                return (RecordStartSource, AutoStopOnManual) switch
                {
                    (RecordActionSourceType.Manual, true) => true,
                    (RecordActionSourceType.Manual, false) => false,
                    (RecordActionSourceType.ManualOBS, true) => true,
                    (RecordActionSourceType.ManualOBS, false) => false,
                    (RecordActionSourceType.Auto, _) => true,
                    _ => false

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

        public async Task TryStartRecordingAsync(RecordActionSourceType startType, RecordStartOption recordStartOption, string? fileFormat = null)
        {
            OBSWebsocket? obs = Obs.GetConnectedObs();
            Logger.log?.Debug($"TryStartRecording");
            if (obs == null)
            {
                Logger.log?.Error($"Unable to start recording, obs instance not found.");
                return;
            }
            if (OutputState == OutputState.Started || OutputState == OutputState.Starting)
            {
                Logger.log?.Warn($"Cannot start recording, already started.");
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
                RecordStartSource = startType;
                RecordStartOption = recordStartOption;
                await obs.StartRecording().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OutputState state = OutputState;
                if (!(state == OutputState.Starting || OutputState == OutputState.Started))
                {
                    RecordStartSource = RecordActionSourceType.None;
                    RecordStartOption = RecordStartOption.None;
                }
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
            if (LastLevelData == null)
            {
                if (GameStatus.DifficultyBeatmap != null)
                {
                    RecordingData recordingData = new RecordingData(new BeatmapLevelWrapper(GameStatus.DifficultyBeatmap));
                    LastLevelData = recordingData;
                }
            }
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



        #region Setup/Teardown


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

            obs.RecordingStateChanged += OnObsRecordingStateChanged;
            obs.OBSComponentChanged += OnOBSComponentChanged;
            StartLevelPatch.LevelStarting += OnLevelStarting;
            HandleStandardLevelDidFinishPatch.LevelDidFinish += OnLevelDidFinish;
            BSEvents.gameSceneActive += OnGameSceneActive;
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }


        protected override void RemoveEvents(OBSController obs)
        {
            if (obs == null) return;
            base.RemoveEvents(obs);
            obs.RecordingStateChanged -= OnObsRecordingStateChanged;
            obs.OBSComponentChanged -= OnOBSComponentChanged;
            StartLevelPatch.LevelStarting -= OnLevelStarting;
            HandleStandardLevelDidFinishPatch.LevelDidFinish -= OnLevelDidFinish;
            BSEvents.gameSceneActive -= OnGameSceneActive;
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
        }

        private async Task SceneSequenceCallback(SceneStage sceneStage)
        {
            Logger.log?.Debug($"RecordingController: SceneStage - {sceneStage}.");
            if (sceneStage == SceneStage.IntroStarted && RecordStartOption == RecordStartOption.SceneSequence)
            {
                await TryStartRecordingAsync(RecordActionSourceType.Auto, RecordStartOption.SceneSequence);
            }
            else if (sceneStage == SceneStage.OutroFinished)
            {
                if (RecordStopOption == RecordStopOption.SceneSequence)
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
            ReadyToStartPatch.ApplyPatch();
            SetEvents(Obs);
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            ReadyToStartPatch.RemovePatch();
            RemoveEvents(Obs);
        }
        #endregion
    }

}
