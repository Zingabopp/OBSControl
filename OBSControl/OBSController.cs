using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OBS.WebSocket.NET;

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

        private void CreateObsInstance()
        {
            var newObs = new ObsWebSocket();
            newObs.Timeout = new TimeSpan(0, 0, 30);
            newObs.Connected += OnConnect;
            newObs.RecordingStateChanged += Obs_RecordingStateChanged;
            newObs.StreamingStateChanged += Obs_StreamingStateChanged;
            newObs.StreamStatus += Obs_StreamStatus;
            obs = newObs;
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

        private void DestroyObsInstance(ObsWebSocket target)
        {
            Logger.log.Debug("Disconnecting from old obs instance.");
            if (target.IsConnected)
                target.Disconnect();
            target.Connected -= OnConnect;
            target.RecordingStateChanged -= Obs_RecordingStateChanged;
            target.StreamingStateChanged -= Obs_StreamingStateChanged;
            target.StreamStatus -= Obs_StreamStatus;
        }

        private void Obs_StreamingStateChanged(ObsWebSocket sender, OBS.WebSocket.NET.Types.OutputState type)
        {
            Logger.log.Info($"Streaming State Changed: {type.ToString()}");
        }

        private void Obs_RecordingStateChanged(ObsWebSocket sender, OBS.WebSocket.NET.Types.OutputState type)
        {
            Logger.log.Info($"Recording State Changed: {type.ToString()}");
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
        private bool OnConnectTriggered = false;
        private void OnConnect(object sender, EventArgs e)
        {
            OnConnectTriggered = true;
            Logger.log.Info($"OnConnect: Connected to OBS.");
        }

        public static OBSController instance { get; private set; }
        public bool IsConnected => obs?.IsConnected ?? false;

        private PluginConfig Config => Plugin.config.Value;

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
            if (obs.IsConnected)
                obs.Disconnect();
        }
        #endregion
    }
}
