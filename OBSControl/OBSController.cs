using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OBS.WebSocket.NET;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Globalization;

namespace OBSControl
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class OBSController
        : MonoBehaviour
    {
        private ObsWebSocket _obs;
        private ObsWebSocket obs
        {
            get { return _obs; }
            set
            {
                if (_obs == value)
                    return;
                Logger.log.Info($"obs.set");
                if (_obs != null)
                {

                }
                _obs = value;
            }
        }

        private static float PlayerHeight;

        StringBuilder fileRenameText = new StringBuilder();

        private PlayerSpecificSettings _playerSettings;
        private PlayerSpecificSettings PlayerSettings
        {
            get
            {
                if (_playerSettings == null)
                {
                    _playerSettings = GameStatus.gameSetupData?.playerSpecificSettings;
                    if (_playerSettings != null)
                    {
                        Logger.log.Debug("Found PlayerSettings");
                    }
                    else
                        Logger.log.Warn($"Unable to find PlayerSettings");
                }
#if DEBUG
                else
                    Logger.log.Debug("PlayerSettings already exists, don't need to find it");
#endif
                return _playerSettings;
            }
        }

        private PlayerDataModelSO _playerData;
        private PlayerDataModelSO PlayerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault();
                    if (_playerSettings != null)
                    {
                        Logger.log.Debug("Found PlayerData");
                    }
                    else
                        Logger.log.Warn($"Unable to find PlayerData");
                }
#if DEBUG
                else
                    Logger.log.Debug("PlayerData already exists, don't need to find it");
#endif
                return _playerData;
            }
        }

        private bool OnConnectTriggered = false;
        public string RecordingFolder;

        public static OBSController instance { get; private set; }
        public bool IsConnected => obs?.IsConnected ?? false;

        private PluginConfig Config => Plugin.config.Value;

        private const string DefaultFileFormat = "%CCYY-%MM-%DD %hh-%mm-%ss";

        private string ToDateTimeFileFormat(DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }

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
                Logger.log.Debug($"OBS reports the Filename Format as \"{currentFormat}\"");
                CurrentFileFormat = fileFormat;
                obs.Api.StartRecording();
                await Task.Delay(1000).ConfigureAwait(false);
                obs.Api.SetFilenameFormatting(DefaultFileFormat);
            });
        }

        private string CurrentFileFormat;

        public void TryStopRecording(bool useDelay)
        {
            Task.Run(async () =>
            {
                if (useDelay)
                {
                    int delay = Config.RecordingStopDelay * 1000;
                    Logger.log.Debug($"Attempting to stop recording after {Config.RecordingStopDelay} sec.");
                    await Task.Delay(delay).ConfigureAwait(false);
                }
                else
                    Logger.log.Debug($"Attempting to stop recording immediately.");
                obs.Api.StopRecording();
            });
        }

        public void RenameLastRecording(string newNameBase)
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
            Logger.log.Info($"Attempting to rename {fileName} to {newNameBase} with an extension of {fileExtension}");
            string newFile = newNameBase + fileExtension;
            int index = 2;
            while (File.Exists(Path.Combine(directory.FullName, newFile)))
            {
                Logger.log.Debug($"File exists: {Path.Combine(directory.FullName, newFile)}");
                newFile = newNameBase + $"({index})" + fileExtension;
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
        public IEnumerator<WaitUntil> GetFileFormat(IDifficultyBeatmap diff = null)
        {
            Logger.log.Debug("Trying to get the file format information for this level");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            yield return new WaitUntil(() =>
            {
                if (diff == null)
                {
                    Logger.log.Debug("GetFileFormat: LevelInfo is null");
                    diff = BS_Utils.Plugin.LevelData?.GameplayCoreSceneSetupData?.difficultyBeatmap;
                }
                else
                    Logger.log.Debug("GetFileFormat: Obtained LevelInfo");
                return (diff != null || timer.ElapsedMilliseconds > 400);
            });
            IBeatmapLevel level = diff?.level;
            
            string fileFormat = ToDateTimeFileFormat(DateTime.Now);
            CurrentFileFormat = fileFormat;
            BaseFilename = string.Empty;
            //string fileFormat = DefaultFileFormat;
            if (level != null)
            {
                BaseFilename = $"{level.songName}-{level.levelAuthorName}";
                BaseFilename = Utilities.Utilities.GetSafeFileName(string.Join("-", level.songName, level.levelAuthorName, diff.difficulty.ToString()));
            }
            else
            {
                Logger.log.Warn("Couldn't get level info, using default recording file format");
                CurrentFileFormat = string.Empty;
            }
            Logger.log.Debug($"Starting recording, file format: {fileFormat}");
            TryStartRecording(fileFormat);
        }
        private string BaseFilename;
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

        #region Setup/Teardown

        private void CreateObsInstance()
        {
            var newObs = new ObsWebSocket
            {
                Timeout = new TimeSpan(0, 0, 30)
            };
            newObs.Connected += OnConnect;
            newObs.RecordingStateChanged += Obs_RecordingStateChanged;
            newObs.StreamingStateChanged += Obs_StreamingStateChanged;
            newObs.StreamStatus += Obs_StreamStatus;
            obs = newObs;
        }

        internal void DestroyObsInstance(ObsWebSocket target)
        {
            Logger.log.Debug("Disconnecting from obs instance.");
            if (target.IsConnected)
            {
                target.Api.SetFilenameFormatting(DefaultFileFormat);
                target.Disconnect();
            }
            target.Connected -= OnConnect;
            target.RecordingStateChanged -= Obs_RecordingStateChanged;
            target.StreamingStateChanged -= Obs_StreamingStateChanged;
            target.StreamStatus -= Obs_StreamStatus;
        }

        public void TryConnect()
        {
            Logger.log.Info($"TryConnect");
            if (!obs.IsConnected)
            {
                Logger.log.Info($"Attempting to connect to {Config.ServerIP}");
                try
                {
                    obs.Connect(Config.ServerIP, Config.ServerPassword);
                    Logger.log.Info($"Finished attempting to connect to {Config.ServerIP}");
                }
                catch (AuthFailureException)
                {
                    Logger.log.Error($"Authentication failed connecting to server {Config.ServerIP}.");
                    return;
                }
                catch (ErrorResponseException ex)
                {
                    Logger.log.Error($"Failed to connect to server {Config.ServerIP}: {ex.Message}.");
                    Logger.log.Debug(ex);
                    return;
                }
                catch (Exception ex)
                {
                    Logger.log.Error($"Failed to connect to server {Config.ServerIP}: {ex.Message}.");
                    Logger.log.Debug(ex);
                    return;
                }
                if (obs.IsConnected)
                    Logger.log.Info($"Connected to OBS @ {Config.ServerIP}");
                else
                    Logger.log.Info($"Not connected to OBS.");
            }
            else
                Logger.log.Info("TryConnect: OBS is already connected.");
        }

        private IEnumerator<WaitForSeconds> RepeatTryConnect()
        {
            var interval = new WaitForSeconds(5);
            while (!(obs?.IsConnected ?? false))
            {
                yield return interval;
                TryConnect();
            }
            Logger.log.Info($"OBS {obs.GetVersion().OBSStudioVersion} is connected.");
            Logger.log.Info($"OnConnectTriggered: {OnConnectTriggered}");
        }

        #endregion

        #region Event Handlers
        private void OnConnect(object sender, EventArgs e)
        {
            OnConnectTriggered = true;
            Logger.log.Info($"OnConnect: Connected to OBS.");
        }

        private void Obs_StreamingStateChanged(ObsWebSocket sender, OBS.WebSocket.NET.Types.OutputState type)
        {
            Logger.log.Info($"Streaming State Changed: {type.ToString()}");
        }

        private void Obs_RecordingStateChanged(ObsWebSocket sender, OBS.WebSocket.NET.Types.OutputState type)
        {
            Logger.log.Info($"Recording State Changed: {type.ToString()}");
            switch (type)
            {
                case OBS.WebSocket.NET.Types.OutputState.Starting:
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Started:
                    Logger.log.Info("Recording started.");
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Stopping:
                    break;
                case OBS.WebSocket.NET.Types.OutputState.Stopped:
                    Logger.log.Info("Recording stopped.");
                    var toRename = fileRenameText.ToString();
                    if (!string.IsNullOrEmpty(toRename))
                    {
                        RenameLastRecording(toRename);
                        fileRenameText.Clear();
                    }
                    break;
                default:
                    break;
            }
        }

        private void Obs_StreamStatus(ObsWebSocket sender, OBS.WebSocket.NET.Types.StreamStatus status)
        {
            Logger.log.Info($"Stream Time: {status.TotalStreamTime.ToString()} sec");
            Logger.log.Info($"Bitrate: {(status.KbitsPerSec / 1024f).ToString("N2")} Mbps");
            Logger.log.Info($"FPS: {status.FPS.ToString()} FPS");
            Logger.log.Info($"Strain: {(status.Strain * 100).ToString()} %");
            Logger.log.Info($"DroppedFrames: {status.DroppedFrames.ToString()} frames");
            Logger.log.Info($"TotalFrames: {status.TotalFrames.ToString()} frames");
        }

        
        
        private void OnLevelFinished(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            fileRenameText.Clear();
            bool delayRecordStop = true;
            try
            {
                fileRenameText.Append(BaseFilename);
                //if (level != null)
                //{
                //    fileFormat = $"{level.songName}-{level.levelAuthorName}";
                //    CurrentFileFormat = fileFormat;
                //}
                Logger.log.Debug($"Max modified score is {GameStatus.MaxModifiedScore}");
                float scorePercent = ((float)levelCompletionResults.rawScore / GameStatus.MaxModifiedScore) * 100f;
                string scoreStr = scorePercent.ToString("F3");
                fileRenameText.Append($"-{scoreStr.Substring(0, scoreStr.Length - 1)}");
                PlayerLevelStatsData stats = PlayerData.playerData.GetPlayerLevelStatsData(
                    GameStatus.LevelInfo.levelID, GameStatus.difficultyBeatmap.difficulty, GameStatus.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
                if (stats.playCount == 0)
                    fileRenameText.Append("-1st");
                else
                    Logger.log.Debug($"PlayCount for {GameStatus.LevelInfo.levelID} is {stats.playCount}");
                if (levelCompletionResults.fullCombo)
                    fileRenameText.Append("-FC");

                if (levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared)
                {

                    if (levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Quit ||
                        levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Restart)
                    {
                        fileRenameText.Append("-QUIT");
                        delayRecordStop = false;
                    }
                    else
                        fileRenameText.Append("-FAILED");
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error appending file name: {ex}");
                Logger.log.Debug(ex);
            }
            TryStopRecording(delayRecordStop);
            recordingCurrentLevel = false;
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
            CreateObsInstance();
            BS_Utils.Plugin.LevelDidFinishEvent -= OnLevelFinished;
            BS_Utils.Plugin.LevelDidFinishEvent += OnLevelFinished;
        }

        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {
            StartCoroutine(RepeatTryConnect());
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

        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {

        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            instance = null;
            DestroyObsInstance(obs);
        }
        #endregion
    }
}
