using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
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
        private OBSWebsocket _obs;
        public OBSWebsocket Obs
        {
            get { return _obs; }
            protected set
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

        private PlayerDataModel _playerData;
        public PlayerDataModel PlayerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
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
        public bool IsConnected => Obs?.IsConnected ?? false;

        private PluginConfig Config => Plugin.config;
        public event EventHandler DestroyingObs;

        #region Setup/Teardown

        private void CreateObsInstance()
        {
            Logger.log.Debug("CreateObsInstance()");
            var newObs = new OBSWebsocket();
            newObs.WSTimeout = new TimeSpan(0, 0, 30);
            newObs.Connected += OnConnect;
            newObs.StreamingStateChanged += Obs_StreamingStateChanged;
            newObs.StreamStatus += Obs_StreamStatus;
            Obs = newObs;
            Logger.log.Debug("CreateObsInstance finished");
        }

        private HashSet<EventHandler<OutputState>> _recordingStateChangedHandlers = new HashSet<EventHandler<OutputState>>();
        public event EventHandler<OutputState> RecordingStateChanged
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

        protected void OnRecordingStateChanged(OBSWebsocket sender, OutputState outputState)
        {
            foreach (var handler in _recordingStateChangedHandlers)
            {
                handler.Invoke(this, outputState);
            }
        }
        private void DestroyObsInstance(OBSWebsocket target)
        {
            if (target == null)
                return;
            Logger.log.Debug("Disconnecting from obs instance.");
            DestroyingObs?.Invoke(this, null);
            if (target.IsConnected)
            {
                //target.Api.SetFilenameFormatting(DefaultFileFormat);
                target.Disconnect();
            }
            target.Connected -= OnConnect;
            target.RecordingStateChanged -= OnRecordingStateChanged;
            target.StreamingStateChanged -= Obs_StreamingStateChanged;
            target.StreamStatus -= Obs_StreamStatus;
        }

        public string lastTryConnectMessage;
        public async Task<bool> TryConnect()
        {
            string message;
            
            if (!Obs.IsConnected)
            {
                try
                {
                    await Obs.Connect(Config.ServerAddress, Config.ServerPassword).ConfigureAwait(false);
                    message = $"Finished attempting to connect to {Config.ServerAddress}";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log.Info(message);
                        lastTryConnectMessage = message;
                    }
                }
                catch (AuthFailureException)
                {
                    message = $"Authentication failed connecting to server {Config.ServerAddress}.";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log.Info(message);
                        lastTryConnectMessage = message;
                    }
                    return false;
                }
                catch (ErrorResponseException ex)
                {
                    message = $"Failed to connect to server {Config.ServerAddress}: {ex.Message}.";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log.Info(message);
                        lastTryConnectMessage = message;
                    }
                    Logger.log.Debug(ex);
                    return false;
                }
                catch (Exception ex)
                {
                    message = $"Failed to connect to server {Config.ServerAddress}: {ex.Message}.";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log.Info(message);
                        Logger.log.Debug(ex);
                        lastTryConnectMessage = message;
                    }
                    return false;
                }
                if (Obs.IsConnected)
                    Logger.log.Info($"Connected to OBS @ {Config.ServerAddress}");
            }
            else
                Logger.log.Info("TryConnect: OBS is already connected.");
            return Obs.IsConnected;
        }

        private async Task RepeatTryConnect()
        {
            if (string.IsNullOrEmpty(Plugin.config.ServerAddress))
            {
                Logger.log.Error("The ServerAddress in the config is null or empty. Unable to connect to OBS.");
                return;
            }
            Logger.log.Info($"Attempting to connect to {Config.ServerAddress}");
            while (!(await TryConnect().ConfigureAwait(false)))
            {
                await Task.Delay(5000).ConfigureAwait(false);
            }

            Logger.log.Info($"OBS {(await Obs.GetVersion().ConfigureAwait(false)).OBSStudioVersion} is connected.");
            Logger.log.Info($"OnConnectTriggered: {OnConnectTriggered}");

        }

        #endregion

        #region OBS Commands


        public Task StartRecording()
        {
            return _obs.StartRecording();
        }

        public Task StopRecording()
        {
            return _obs.StopRecording();
        }
        #endregion

        #region Event Handlers
        private void OnConnect(object sender, EventArgs e)
        {
            OnConnectTriggered = true;
            Logger.log.Info($"OnConnect: Connected to OBS.");
        }

        private void Obs_StreamingStateChanged(OBSWebsocket sender, OutputState type)
        {
            Logger.log.Info($"Streaming State Changed: {type.ToString()}");
        }


        private void Obs_StreamStatus(OBSWebsocket sender, StreamStatus status)
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
            Logger.log.Debug("OBSController Awake()");
            if (instance != null)
                GameObject.DestroyImmediate(this);
            GameObject.DontDestroyOnLoad(this);
            instance = this;
            CreateObsInstance();
        }

        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private async void Start()
        {
            Logger.log.Debug("OBSController Start()");
            await RepeatTryConnect();
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
            if (OBSComponents.RecordingController.instance != null)
                Destroy(OBSComponents.RecordingController.instance);
            DestroyObsInstance(Obs);
        }
        #endregion
    }
}
