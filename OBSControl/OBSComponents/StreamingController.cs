using OBSControl.Wrappers;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#nullable enable
namespace OBSControl.OBSComponents
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    [DisallowMultipleComponent]
    public class StreamingController : OBSComponent
    {
        public bool IsStreaming => StreamingState == OutputState.Started;
        public OutputState StreamingState { get; protected set; }

        private readonly object startLock = new object();
        private readonly object stopLock = new object();
        private TaskCompletionSource<bool>? StartCompletion;
        private TaskCompletionSource<bool>? StopCompletion;

        private async Task<bool> InternalStartStreaming(CancellationToken cancellationToken, TaskCompletionSource<bool> taskCompletion)
        {
            OBSWebsocket? obs = Obs?.Obs;
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
                    await obs.StartStreaming(cancellationToken).ConfigureAwait(false);
                    success = true;
                    // Do not set completion here, will be done in OnStreamingStateChanged.
                }
                catch (OperationCanceledException ex)
                {
                    taskCompletion.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error trying to start streaming: {ex.Message}.");
                    Logger.log?.Debug(ex);
                    taskCompletion.TrySetException(ex);
                }
            }
            if (!success)
            {
                taskCompletion.TrySetResult(success);
                Logger.log?.Error($"Unable to start streaming: {message}");
            }
            return success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ErrorResponseException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<bool> StartStreaming(CancellationToken cancellationToken = default)
        {
            //using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, AllTasksCancelSource.Token);
            //cts.Token.ThrowIfCancellationRequested();
            TaskCompletionSource<bool> taskCompletion;
            bool taskStarted = false;
            lock (startLock)
            {
                if (StartCompletion != null)
                    taskCompletion = StartCompletion;
                else
                {
                    taskCompletion = new TaskCompletionSource<bool>();
                    taskStarted = true;
                    StartCompletion = taskCompletion;
                }
            }
            bool result;
            if (taskStarted)
            {
                using CancellationTokenRegistration registration = cancellationToken.Register(() => taskCompletion.TrySetCanceled(cancellationToken));
                await InternalStartStreaming(cancellationToken, taskCompletion).ConfigureAwait(false);
                result = await taskCompletion.Task;
            }
            else
                result = await taskCompletion.Task;
            return result;
        }

        private async Task<bool> InternalStopStreaming(CancellationToken cancellationToken, TaskCompletionSource<bool> taskCompletion)
        {
            OBSWebsocket? obs = Obs?.Obs;
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
                    await obs.StopStreaming(cancellationToken).ConfigureAwait(false);
                    success = true;
                    // Do not set completion here, will be done in OnStreamingStateChanged.
                }
                catch (OperationCanceledException ex)
                {
                    taskCompletion.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error trying to stop streaming: {ex.Message}.");
                    Logger.log?.Debug(ex);
                    taskCompletion.TrySetException(ex);
                }
            }
            if (!success)
            {
                taskCompletion.TrySetResult(success);
                Logger.log?.Error($"Unable to stop streaming: {message}");
            }
            return success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ErrorResponseException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<bool> StopStreaming(CancellationToken cancellationToken = default)
        {
            //using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, AllTasksCancelSource.Token);
            //cts.Token.ThrowIfCancellationRequested();
            TaskCompletionSource<bool> taskCompletion;
            bool taskStarted = false;
            lock (stopLock)
            {
                if (StopCompletion != null)
                    taskCompletion = StopCompletion;
                else
                {
                    taskCompletion = new TaskCompletionSource<bool>();
                    taskStarted = true;
                    StopCompletion = taskCompletion;
                }
            }
            bool result;
            if (taskStarted)
            {
                using CancellationTokenRegistration registration = cancellationToken.Register(() => taskCompletion.TrySetCanceled(cancellationToken));
                await InternalStopStreaming(cancellationToken, taskCompletion).ConfigureAwait(false);
                result = await taskCompletion.Task;
            }
            else
                result = await taskCompletion.Task;
            return result;
        }

        #region OBS Event Handlers

        private void OnStreamingStateChanged(object sender, OutputState type)
        {
            Logger.log?.Debug($"Streaming State Changed: {type}");
            StreamingState = type;
            TaskCompletionSource<bool>? startCompletion = StartCompletion;
            TaskCompletionSource<bool>? stopCompletion = StopCompletion;
            if (type == OutputState.Stopped || type == OutputState.Stopping)
            {
                if (type == OutputState.Stopped)
                {
                    Logger.log?.Debug($"Setting streaming stop completion.");
                    stopCompletion?.TrySetResult(true);
                }
                lock (startLock)
                {
                    if (startCompletion != null)
                        startCompletion.TrySetResult(false);
                    StartCompletion = null;
                }
            }
            if (type == OutputState.Started || type == OutputState.Starting)
            {
                if (type == OutputState.Started)
                {
                    Logger.log?.Debug($"Setting streaming start completion.");
                    startCompletion?.TrySetResult(true);
                }
                lock (stopLock)
                {
                    if (stopCompletion != null)
                        stopCompletion.TrySetResult(false);
                    StopCompletion = null;
                }
            }
        }

        #endregion

        #region Setup/Teardown
        public override async Task InitializeAsync(OBSController obs)
        {
            await base.InitializeAsync(obs);
            OBSWebsocket? websocket = obs.GetConnectedObs();
            if (websocket != null)
            {
                OutputStatus status = await websocket.GetStreamingStatus().ConfigureAwait(false);
                StreamingState = status.IsStreaming ? OutputState.Started : OutputState.Stopped;
            }
        }
        protected override void SetEvents(OBSController obs)
        {
            if (obs == null) return;
            RemoveEvents(obs);
            base.SetEvents(obs);
            obs.StreamingStateChanged += OnStreamingStateChanged;
        }
        protected override void RemoveEvents(OBSController obs)
        {
            if (obs == null) return;
            base.RemoveEvents(obs);
            obs.StreamingStateChanged -= OnStreamingStateChanged;
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
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        //private void Awake()
        //{
        //    if (instance != null)
        //    {
        //        GameObject.DestroyImmediate(this);
        //        return;
        //    }
        //    GameObject.DontDestroyOnLoad(this);
        //    instance = this;
        //}

        ///// <summary>
        ///// Called when the script becomes enabled and active
        ///// </summary>
        //private void OnEnable()
        //{
        //}

        ///// <summary>
        ///// Called when the script becomes disabled or when it is being destroyed.
        ///// </summary>
        //private void OnDisable()
        //{
        //}

        ///// <summary>
        ///// Called when the script is being destroyed.
        ///// </summary>
        //private void OnDestroy()
        //{
        //}
        #endregion
    }
}
