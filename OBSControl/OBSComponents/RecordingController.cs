using OBSControl.Wrappers;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace OBSControl.OBSComponents
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class RecordingController : MonoBehaviour
    {
        public static RecordingController instance { get; private set; }
        private OBSWebsocket obs => OBSController.instance.Obs;
        HarmonyPatches.HarmonyPatchInfo LevelDelayPatch;
        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";
        public bool WaitingToStop { get; private set; }
        public Task StopRecordingTask { get; private set; }
        private string ToDateTimeFileFormat(DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }

        public string RecordingFolder { get; protected set; }
        public async Task TryStartRecordingAsync(string fileFormat = DefaultFileFormat)
        {
            Logger.log.Debug($"TryStartRecording");
            try
            {
                RecordingFolder = await obs.GetRecordingFolder().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting recording folder from OBS: {ex.Message}");
                Logger.log?.Debug(ex);
                return;
            }

            int tries = 0;
            string currentFormat = null;
            do
            {
                if (tries > 0)
                {
                    Logger.log.Debug($"({tries}) Failed to set OBS's FilenameFormatting to {fileFormat} retrying in 50ms");
                    await Task.Delay(50);
                }
                tries++;
                try
                {
                    await obs.SetFilenameFormatting(fileFormat).ConfigureAwait(false);
                    currentFormat = await obs.GetFilenameFormatting().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error getting current filename format from OBS: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
            } while (currentFormat != fileFormat && tries < 10);
            CurrentFileFormat = fileFormat;
            string startScene = Plugin.config.StartSceneName;
            string gameScene = Plugin.config.GameSceneName;
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
                    Logger.log.Info($"Setting intro OBS scene to '{startScene}'");
                    await obs.SetCurrentScene(startScene).ConfigureAwait(false);
                    await obs.SetTransitionDuration(transitionDuration).ConfigureAwait(false);
                    await obs.StartRecording().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(Plugin.config.StartSceneDuration)).ConfigureAwait(false);
                    Logger.log.Info($"Setting game OBS scene to '{gameScene}'");
                    await obs.SetCurrentScene(gameScene).ConfigureAwait(false);
                }
                else
                {
                    if (!string.IsNullOrEmpty(gameScene))
                        await obs.SetCurrentScene(gameScene).ConfigureAwait(false);
                    await obs.StartRecording().ConfigureAwait(false);
                }

            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error starting recording in OBS: {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }

        public async Task AudioFun()
        {
            float volume = (await obs.GetVolume("Desktop")).Volume;
            float volumeFade = 0f;
            while(volumeFade < volume)
            {
                await obs.SetVolume("Desktop", volumeFade);
                volumeFade += 0.05f;
            }
        }


        public async Task<string[]> GetAvailableScenes()
        {
            try
            {
                return (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error validating scenes: {ex.Message}");
                Logger.log.Debug(ex);
                return Array.Empty<string>();
            }
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
                string[] availableScenes = (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
                Logger.log.Debug($"Available scenes: {string.Join(", ", availableScenes)}");
                return scenes.All(s => availableScenes.Contains(s));
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error validating scenes: {ex.Message}");
                Logger.log.Debug(ex);
                return false;
            }
        }

        private string CurrentFileFormat { get; set; }

        public async Task TryStopRecordingAsync(string renameTo, bool stopImmediate = false)
        {
            string endScene = Plugin.config.EndSceneName;
            string gameScene = Plugin.config.GameSceneName;
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
                        Logger.log.Info($"Setting outro OBS scene to '{endScene}'");
                        await obs.SetCurrentScene(endScene);
                        await Task.Delay(TimeSpan.FromSeconds(Plugin.config.EndSceneDuration));
                    }
                }
                await obs.StopRecording().ConfigureAwait(false);
                if(!stopImmediate && validOutro)
                {
                    Logger.log.Info($"Setting game OBS scene to '{gameScene}'");
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
            catch (Exception ex)
            {
                Logger.log?.Error($"Unexpected exception trying to stop recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            finally
            {
                WaitingToStop = false;
                StopRecordingTask = null;
            }
        }

        private string RenameString;
        public void RenameLastRecording(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                Logger.log.Warn($"Unable to rename last recording, provided new name is null or empty.");
                return;
            }
            string recordingFolder = RecordingFolder;
            string fileFormat = CurrentFileFormat;
            CurrentFileFormat = null;
            if (string.IsNullOrEmpty(recordingFolder))
            {
                Logger.log.Warn($"Unable to determine current recording folder, unable to rename.");
                return;
            }
            if (string.IsNullOrEmpty(fileFormat))
            {
                Logger.log.Warn($"Last recorded filename not stored, unable to rename.");
                return;
            }

            DirectoryInfo directory = new DirectoryInfo(recordingFolder);
            if (!directory.Exists)
            {
                Logger.log.Warn($"Recording directory doesn't exist, unable to rename.");
                return;
            }
            FileInfo targetFile = directory.GetFiles(fileFormat + "*").OrderByDescending(f => f.CreationTimeUtc).FirstOrDefault();
            if (targetFile == null)
            {
                Logger.log.Warn($"Couldn't find recorded file, unable to rename.");
                return;
            }
            string fileName = targetFile.Name.Substring(0, targetFile.Name.LastIndexOf('.'));
            string fileExtension = targetFile.Extension;
            Logger.log.Info($"Attempting to rename {fileFormat}{fileExtension} to {newName} with an extension of {fileExtension}");
            string newFile = newName + fileExtension;
            int index = 2;
            while (File.Exists(Path.Combine(directory.FullName, newFile)))
            {
                Logger.log.Debug($"File exists: {Path.Combine(directory.FullName, newFile)}");
                newFile = newName + $"({index})" + fileExtension;
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

        public void StartRecordingLevel(IDifficultyBeatmap level = null)
        {
            string fileFormat = ToDateTimeFileFormat(DateTime.Now);
            Logger.log.Debug($"Starting recording, file format: {fileFormat}");
            TryStartRecordingAsync(fileFormat);
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
            string newFileName = null;
            try
            {

                PlayerLevelStatsData stats = OBSController.instance.PlayerData.playerData.GetPlayerLevelStatsData(
                    GameStatus.LevelInfo.levelID, GameStatus.difficultyBeatmap.difficulty, GameStatus.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);

                Wrappers.LevelCompletionResultsWrapper resultsWrapper = new Wrappers.LevelCompletionResultsWrapper(levelCompletionResults, stats.playCount, GameStatus.MaxModifiedScore);
                newFileName = Utilities.FileRenaming.GetFilenameString(Plugin.config.RecordingFileFormat,
                    new BeatmapLevelWrapper(GameStatus.difficultyBeatmap), resultsWrapper);
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error generating new file name: {ex}");
                Logger.log.Debug(ex);
            }
            StopRecordingTask = TryStopRecordingAsync(newFileName, false);
        }

        #region OBS Event Handlers

        private void Obs_RecordingStateChanged(object sender, OutputState type)
        {
            Logger.log.Info($"Recording State Changed: {type.ToString()}");
            switch (type)
            {
                case OutputState.Starting:
                    recordingCurrentLevel = true;
                    break;
                case OutputState.Started:
                    recordingCurrentLevel = true;
                    Task.Run(() => obs.SetFilenameFormatting(DefaultFileFormat));
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
            LevelDelayPatch = HarmonyPatches.HarmonyManager.GetLevelDelayPatch();
            OBSController.instance.RecordingStateChanged += Obs_RecordingStateChanged;
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
