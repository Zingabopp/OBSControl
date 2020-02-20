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


        #region Setup/Teardown

        private void CreateObsInstance()
        {
            var newObs = new ObsWebSocket();
            newObs.Timeout = new TimeSpan(0, 0, 30);
            newObs.Connected += OnConnect;
            newObs.StreamingStateChanged += Obs_StreamingStateChanged;
            newObs.StreamStatus += Obs_StreamStatus;
            obs = newObs;
        }

        private HashSet<EventHandler<OBS.WebSocket.NET.Types.OutputState>> _recordingStateChangedHandlers = new HashSet<EventHandler<OBS.WebSocket.NET.Types.OutputState>>();
        public event EventHandler<OBS.WebSocket.NET.Types.OutputState> RecordingStateChanged
        {
            add
            {
                bool firstSubscriber = _recordingStateChangedHandlers.Count == 0;
                _recordingStateChangedHandlers.Add(value);
                if (firstSubscriber && _recordingStateChangedHandlers != null)
                    _obs.RecordingStateChanged += OnRecordingStateChanged;
            }
            remove
            {
                _recordingStateChangedHandlers.Remove(value);
                if (_recordingStateChangedHandlers.Count == 0)
                    _obs.RecordingStateChanged -= OnRecordingStateChanged;
            }
        }

        protected void OnRecordingStateChanged(ObsWebSocket sender, OBS.WebSocket.NET.Types.OutputState outputState)
        {
            foreach (var handler in _recordingStateChangedHandlers)
            {
                handler.Invoke(this, outputState);
            }
        }
        private void DestroyObsInstance(ObsWebSocket target)
        {
            Logger.log.Debug("Disconnecting from obs instance.");
            if (target.IsConnected)
            {
                //target.Api.SetFilenameFormatting(DefaultFileFormat);
                target.Disconnect();
            }
            target.Connected -= OnConnect;
            target.RecordingStateChanged += OnRecordingStateChanged;
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


        private void Obs_StreamStatus(ObsWebSocket sender, OBS.WebSocket.NET.Types.StreamStatus status)
        {
            Logger.log.Info($"Stream Time: {status.TotalStreamTime.ToString()} sec");
            Logger.log.Info($"Bitrate: {(status.KbitsPerSec / 1024f).ToString("N2")} Mbps");
            Logger.log.Info($"FPS: {status.FPS.ToString()} FPS");
            Logger.log.Info($"Strain: {(status.Strain * 100).ToString()} %");
            Logger.log.Info($"DroppedFrames: {status.DroppedFrames.ToString()} frames");
            Logger.log.Info($"TotalFrames: {status.TotalFrames.ToString()} frames");
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
