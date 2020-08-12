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
	public class StreamingController : MonoBehaviour
    {
        public static StreamingController? instance { get; private set; }
        private OBSController? _obs => OBSController.instance;
        public OutputState OutputState { get; protected set; }


        public async Task StartStreaming()
        {
            OBSWebsocket? obs = _obs?.Obs;
            string? message = null;
            bool success = false;
            if (obs == null)
                message = "OBS is null";
            else if (!obs.IsConnected)
                message = "Not connected to OBS.";
            else
            {
                try
                {
                    await obs.StartStreaming().ConfigureAwait(false);
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error trying to start streaming: {ex.Message}.");
                    Logger.log?.Debug(ex);
                    return;
                }
            }
            if (!success)
                Logger.log?.Error($"Unable to start streaming: {message}");
        }
        public async Task StopStreaming()
        {
            OBSWebsocket? obs = _obs?.Obs;
            string? message = null;
            bool success = false;
            if (obs == null)
                message = "OBS is null";
            else if (!obs.IsConnected)
                message = "Not connected to OBS.";
            else
            {
                try
                {
                    await obs.StopStreaming().ConfigureAwait(false);
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error trying to stop streaming: {ex.Message}.");
                    Logger.log?.Debug(ex);
                    return;
                }
            }
            if (!success)
                Logger.log?.Error($"Unable to stop streaming: {message}");
        }

        #region OBS Event Handlers

        private void OnStreamingStateChanged(object sender, OutputState type)
        {
            Logger.log?.Info($"Streaming State Changed: {type}");
            OutputState = type;
        }

        #endregion

        protected void SetEvents(OBSController obs)
        {
            if (obs == null) return;
            RemoveEvents(obs);
            obs.StreamingStateChanged += OnStreamingStateChanged;
        }
        protected void RemoveEvents(OBSController obs)
        {
            if (obs == null) return;
            obs.StreamingStateChanged -= OnStreamingStateChanged;
        }

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
        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {
            if (_obs != null)
                SetEvents(_obs);
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            if (_obs != null)
                RemoveEvents(_obs);
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
