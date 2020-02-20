using OBS.WebSocket.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private ObsWebSocket obs => OBSController.instance.Obs;
        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";

        private string ToDateTimeFileFormat(DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }

        public string RecordingFolder { get; protected set; }
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
            });
        }

        private string CurrentFileFormat { get; set; }

        public void TryStopRecording(string renameTo = "")
        {
            Task.Run(() =>
            {
                obs.Api.StopRecording();
                RenameString = renameTo;
                recordingCurrentLevel = false;
            });
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
            var targetFile = directory.GetFiles(fileFormat + "*").OrderByDescending(f => f.CreationTimeUtc).FirstOrDefault();
            if (targetFile == null)
            {
                Logger.log.Warn($"Couldn't find recorded file, unable to rename.");
                return;
            }
            string fileName = targetFile.Name.Substring(0, targetFile.Name.LastIndexOf('.'));
            var fileExtension = targetFile.Extension;
            Logger.log.Info($"Attempting to rename {fileFormat}.{fileExtension} to {newName} with an extension of {fileExtension}");
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
            TryStartRecording(fileFormat);
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
                newFileName = Utilities.FileRenaming.GetFilenameString(Plugin.config.Value.RecordingFileFormat, GameStatus.difficultyBeatmap, resultsWrapper);
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error generating new file name: {ex}");
                Logger.log.Debug(ex);
            }
            TryStopRecording(newFileName);
        }

        #region OBS Event Handlers

        private void Obs_RecordingStateChanged(object sender, OBS.WebSocket.NET.Types.OutputState type)
        {
            Logger.log.Info($"Recording State Changed: {type.ToString()}");
            switch (type)
            {
                case OBS.WebSocket.NET.Types.OutputState.Starting:
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Started:
                    recordingCurrentLevel = true;
                    Task.Run(() => obs.Api.SetFilenameFormatting(DefaultFileFormat));
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Stopping:
                    recordingCurrentLevel = false;
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Stopped:
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
                GameObject.DestroyImmediate(this);
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

        HarmonyPatches.HarmonyPatchInfo LevelDelayPatch;
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
            TryStopRecording();
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            if (LevelDelayPatch.IsApplied)
                LevelDelayPatch.RemovePatch();
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
