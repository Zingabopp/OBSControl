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
#nullable enable
namespace OBSControl
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class OBSController
        : MonoBehaviour
    {
        private OBSWebsocket? _obs;
        public OBSWebsocket? Obs
        {
            get { return _obs; }
            protected set
            {
                if (_obs == value)
                    return;
                Logger.log?.Info($"obs.set");
                if (_obs != null)
                {

                }
                _obs = value;
            }
        }

        public OutputState CurrentRecordingState { get; private set; }

        //private static float PlayerHeight;

        //        private PlayerSpecificSettings _playerSettings;
        //        private PlayerSpecificSettings PlayerSettings
        //        {
        //            get
        //            {
        //                if (_playerSettings == null)
        //                {
        //                    _playerSettings = GameStatus.gameSetupData?.playerSpecificSettings;
        //                    if (_playerSettings != null)
        //                    {
        //                        Logger.log?.Debug("Found PlayerSettings");
        //                    }
        //                    else
        //                        Logger.log?.Warn($"Unable to find PlayerSettings");
        //                }
        //#if DEBUG
        //                else
        //                    Logger.log?.Debug("PlayerSettings already exists, don't need to find it");
        //#endif
        //                return _playerSettings;
        //            }
        //        }

        private PlayerDataModel? _playerData;
        public PlayerDataModel? PlayerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
                    if (_playerData != null)
                    {
                        Logger.log?.Debug("Found PlayerData");
                    }
                    else
                        Logger.log?.Warn($"Unable to find PlayerData");
                }
#if DEBUG
                else
                    Logger.log?.Debug("PlayerData already exists, don't need to find it");
#endif
                return _playerData;
            }
        }

        private bool OnConnectTriggered = false;
        public string? RecordingFolder;

        public static OBSController? instance { get; private set; }
        public bool IsConnected => Obs?.IsConnected ?? false;

        private PluginConfig Config => Plugin.config;
        public event EventHandler? DestroyingObs;

        #region Setup/Teardown

        private void CreateObsInstance()
        {
            Logger.log?.Debug("CreateObsInstance()");
            var newObs = new OBSWebsocket();
            //newObs. = new TimeSpan(0, 0, 30);
            newObs.Connected += OnConnect;
            newObs.Disconnected += OnDisconnect;
            newObs.StreamingStateChanged += Obs_StreamingStateChanged;
            newObs.StreamStatus += Obs_StreamStatus;
            newObs.SceneListChanged += OnObsSceneListChanged;
            Obs = newObs;
            Logger.log?.Debug("CreateObsInstance finished");
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
        }

        private HashSet<EventHandler<OutputState>> _recordingStateChangedHandlers = new HashSet<EventHandler<OutputState>>();
        public event EventHandler<OutputState> RecordingStateChanged
        {
            add
            {
                bool firstSubscriber = _recordingStateChangedHandlers.Count == 0;
                _recordingStateChangedHandlers.Add(value);
                if (firstSubscriber && _recordingStateChangedHandlers != null && _obs != null)
                    _obs.RecordingStateChanged += OnRecordingStateChanged;
            }
            remove
            {
                _recordingStateChangedHandlers.Remove(value);
                if (_recordingStateChangedHandlers.Count == 0 && _obs != null)
                    _obs.RecordingStateChanged -= OnRecordingStateChanged;
            }
        }

        protected void OnRecordingStateChanged(object sender, OutputStateChangedEventArgs outputState)
        {
            CurrentRecordingState = outputState.OutputState;
            foreach (var handler in _recordingStateChangedHandlers)
            {
                handler.Invoke(this, outputState.OutputState);
            }
        }
        private void DestroyObsInstance(OBSWebsocket? target)
        {
            if (target == null)
                return;
            Logger.log?.Debug("Disconnecting from obs instance.");
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
            target.SceneListChanged -= OnObsSceneListChanged;
        }

        public string? lastTryConnectMessage;
        public async Task<bool> TryConnect()
        {
            string message;
            string? serverAddress = Config.ServerAddress;
            if (serverAddress == null || serverAddress.Length == 0)
            {
                Logger.log?.Error($"ServerAddress cannot be null or empty.");
                return false;
            }
            if (Obs != null && !Obs.IsConnected)
            {
                try
                {
                    await Obs.Connect(serverAddress, Config.ServerPassword).ConfigureAwait(false);
                    message = $"Finished attempting to connect to {Config.ServerAddress}";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log?.Info(message);
                        lastTryConnectMessage = message;
                    }
                }
                catch (AuthFailureException)
                {
                    message = $"Authentication failed connecting to server {Config.ServerAddress}.";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log?.Info(message);
                        lastTryConnectMessage = message;
                    }
                    return false;
                }
                catch (ErrorResponseException ex)
                {
                    message = $"Failed to connect to server {Config.ServerAddress}: {ex.Message}.";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log?.Info(message);
                        lastTryConnectMessage = message;
                    }
                    Logger.log?.Debug(ex);
                    return false;
                }
                catch (Exception ex)
                {
                    message = $"Failed to connect to server {Config.ServerAddress}: {ex.Message}.";
                    if (message != lastTryConnectMessage)
                    {
                        Logger.log?.Info(message);
                        Logger.log?.Debug(ex);
                        lastTryConnectMessage = message;
                    }
                    return false;
                }
                if (Obs.IsConnected)
                    Logger.log?.Info($"Connected to OBS @ {Config.ServerAddress}");
            }
            else
                Logger.log?.Info("TryConnect: OBS is already connected.");
            return Obs?.IsConnected ?? false;
        }

        private async Task RepeatTryConnect()
        {
            OBSWebsocket? obs = Obs;
            if (obs == null)
            {
                Logger.log?.Error($"Obs instance is null in RepeatTryConnect()");
                return;
            }
            try
            {
                if (string.IsNullOrEmpty(Plugin.config.ServerAddress))
                {
                    Logger.log?.Error("The ServerAddress in the config is null or empty. Unable to connect to OBS.");
                    return;
                }
                Logger.log?.Info($"Attempting to connect to {Config.ServerAddress}");
                while (!(await TryConnect().ConfigureAwait(false)))
                {
                    await Task.Delay(5000).ConfigureAwait(false);
                }

                Logger.log?.Info($"OBS {(await obs.GetVersion().ConfigureAwait(false)).OBSStudioVersion} is connected.");
                Logger.log?.Info($"OnConnectTriggered: {OnConnectTriggered}");
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error in RepeatTryConnect: {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }

        #endregion
        public event EventHandler? Connected;
        #region Event Handlers
        private async void OnConnect(object sender, EventArgs e)
        {
            OBSWebsocket? obs = _obs;
            if (obs == null) return;
            OnConnectTriggered = true;
            Logger.log?.Info($"OnConnect: Connected to OBS.");
            try
            {
                string[] availableScenes = (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
                HMMainThreadDispatcher.instance.Enqueue(() =>
                {
                    Plugin.config.UpdateSceneOptions(availableScenes);
                });
                var thing = await obs.ListOutputs();
                var recordingOutput = thing.FirstOrDefault(o => o is FileOutput) as FileOutput;
                if (recordingOutput != null)
                    CurrentRecordingState = recordingOutput.Active ? OutputState.Started : OutputState.Stopped;
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting scene list: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private async void OnObsSceneListChanged(object sender, EventArgs e)
        {
            OBSWebsocket? obs = _obs;
            if (obs == null) return;
            try
            {
                string[] availableScenes = (await obs.GetSceneList().ConfigureAwait(false)).Scenes.Select(s => s.Name).ToArray();
                Logger.log?.Info($"OBS scene list changed: {string.Join(", ", availableScenes)}");
                HMMainThreadDispatcher.instance.Enqueue(() =>
                {
                    Plugin.config.UpdateSceneOptions(availableScenes);
                });
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error getting scene list: {ex.Message}");
                Logger.log?.Debug(ex);
            }
        }

        private void Obs_StreamingStateChanged(object sender, OutputStateChangedEventArgs e)
        {
            Logger.log?.Info($"Streaming State Changed: {e.OutputState.ToString()}");
        }


        private void Obs_StreamStatus(object sender, StreamStatusEventArgs status)
        {
            Logger.log?.Info($"Stream Time: {status.TotalStreamTime.ToString()} sec");
            Logger.log?.Info($"Bitrate: {(status.KbitsPerSec / 1024f).ToString("N2")} Mbps");
            Logger.log?.Info($"FPS: {status.FPS.ToString()} FPS");
            Logger.log?.Info($"Strain: {(status.Strain * 100).ToString()} %");
            Logger.log?.Info($"DroppedFrames: {status.DroppedFrames.ToString()} frames");
            Logger.log?.Info($"TotalFrames: {status.TotalFrames.ToString()} frames");
        }

        #endregion

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            Logger.log?.Debug("OBSController Awake()");
            if (instance != null)
            {
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this);
            instance = this;
            CreateObsInstance();
        }

        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {
            Logger.log?.Debug("OBSController Start()");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            RepeatTryConnect();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {
            OBSComponents.RecordingController.instance?.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            OBSComponents.RecordingController.instance?.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            instance = null;
            if (OBSComponents.RecordingController.instance != null)
            {
                Destroy(OBSComponents.RecordingController.instance);
            }
            DestroyObsInstance(Obs);
        }
        #endregion
    }
}
