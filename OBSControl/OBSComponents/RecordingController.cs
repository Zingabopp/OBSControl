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
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class RecordingController : MonoBehaviour
    {
        public static RecordingController? instance { get; private set; }
        private OBSWebsocket? _obs => OBSController.instance?.Obs;
        internal readonly HarmonyPatches.HarmonyPatchInfo LevelDelayPatch = HarmonyPatches.HarmonyManager.GetLevelDelayPatch();
        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
        public const string DefaultDateTimeFormat = "yyyyMMddHHmmss";
        public bool WaitingToStop { get; private set; }
        public Task? StopRecordingTask { get; private set; }
        private string ToDateTimeFileFormat(DateTime dateTime)
        {
            return dateTime.ToString(DefaultDateTimeFormat);
        }

        public string? RecordingFolder { get; protected set; }
        public async Task TryStartRecordingAsync(string fileFormat = DefaultFileFormat)
        {
            OBSWebsocket? obs = _obs;
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
#pragma warning restore CA1031 // Do not catch general exception types
            } while (currentFormat != fileFormat && tries < 10);
            CurrentFileFormat = fileFormat;
            string? startScene = Plugin.config.StartSceneName;
            string? gameScene = Plugin.config.GameSceneName;
            string[] availableScenes = await GetAvailableScenes().ConfigureAwait(false);
            if (!availableScenes.Contains(startScene))
                startScene = string.Empty;
            if (!availableScenes.Contains(gameScene))
                gameScene = string.Empty;
            bool validIntro = ValidateScenes(availableScenes, startScene, gameScene);
            try
            {
                if (validIntro)
                {
                    int transitionDuration = await obs.GetTransitionDuration().ConfigureAwait(false);
                    await obs.SetTransitionDuration(0).ConfigureAwait(false);
                    Logger.log?.Info($"Setting intro OBS scene to '{startScene}'");
                    await obs.SetCurrentScene(startScene).ConfigureAwait(false);
                    await obs.SetTransitionDuration(transitionDuration).ConfigureAwait(false);
                    await obs.StartRecording().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(Plugin.config.StartSceneDuration)).ConfigureAwait(false);
                    Logger.log?.Info($"Setting game OBS scene to '{gameScene}'");
                    await obs.SetCurrentScene(gameScene).ConfigureAwait(false);
                }
                else
                {
                    if (!string.IsNullOrEmpty(gameScene))
                        await obs.SetCurrentScene(gameScene).ConfigureAwait(false);
                    await obs.StartRecording().ConfigureAwait(false);
                }

            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error starting recording in OBS: {ex.Message}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public async Task<string[]> GetAvailableScenes()
        {
            OBSWebsocket? obs = _obs;
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
            return scenes.All(s => !string.IsNullOrEmpty(s) && availableScenes.Contains(s));
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
            OBSWebsocket? obs = _obs;
            if (obs == null)
            {
                Logger.log?.Error($"Unable to stop recording, obs instance is null.");
                return;
            }
            string endScene = Plugin.config.EndSceneName ?? string.Empty;
            string gameScene = Plugin.config.GameSceneName ?? string.Empty;
            string[] availableScenes = await GetAvailableScenes().ConfigureAwait(false);
            if (!availableScenes.Contains(endScene))
                endScene = string.Empty;
            if (!availableScenes.Contains(gameScene))
                gameScene = string.Empty;
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
                    Logger.log?.Info($"Setting game OBS scene to '{gameScene}'");
                    await obs.SetCurrentScene(gameScene).ConfigureAwait(false);
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

        public void StartRecordingLevel()
        {
            string fileFormat = ToDateTimeFileFormat(DateTime.Now);
            Logger.log?.Debug($"Starting recording, file format: {fileFormat}");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            TryStartRecordingAsync(fileFormat);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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

        private void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
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

                Wrappers.LevelCompletionResultsWrapper resultsWrapper = new Wrappers.LevelCompletionResultsWrapper(levelCompletionResults, stats?.playCount ?? 0, GameStatus.MaxModifiedScore);
                if (GameStatus.DifficultyBeatmap != null)
                {
                    newFileName = Utilities.FileRenaming.GetFilenameString(Plugin.config.RecordingFileFormat,
                        new BeatmapLevelWrapper(GameStatus.DifficultyBeatmap), resultsWrapper, 
                        Plugin.config.InvalidCharacterSubstitute, Plugin.config.ReplaceSpacesWith);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Error generating new file name: {ex}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            StopRecordingTask = TryStopRecordingAsync(newFileName, false);
        }

        #region OBS Event Handlers

        private void Obs_RecordingStateChanged(object sender, OutputState type)
        {
            Logger.log?.Info($"Recording State Changed: {type}");
            switch (type)
            {
                case OutputState.Starting:
                    recordingCurrentLevel = true;
                    break;
                case OutputState.Started:
                    recordingCurrentLevel = true;
                    Task.Run(() => _obs?.SetFilenameFormatting(DefaultFileFormat));
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


        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            if (instance != null)
            {
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this);
            instance = this;
            if (OBSController.instance != null)
                OBSController.instance.RecordingStateChanged += Obs_RecordingStateChanged;
        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
            if (!LevelDelayPatch.IsApplied)
                LevelDelayPatch.ApplyPatch();
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            if (recordingCurrentLevel)
                StopRecordingTask = TryStopRecordingAsync(string.Empty, true);
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            if (LevelDelayPatch?.IsApplied ?? false)
                LevelDelayPatch.RemovePatch();
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            instance = null;
        }
        #endregion
    }
}
