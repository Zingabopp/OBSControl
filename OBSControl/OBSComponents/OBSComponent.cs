using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#nullable enable
namespace OBSControl.OBSComponents
{
    public abstract class OBSComponent : MonoBehaviour
    {
        public bool ActiveAndConnected => isActiveAndEnabled && Connected;
        public bool Connected { get; private set; }
        private OBSController? _obs;
        protected OBSController Obs
        {
            get => _obs ?? throw new InvalidOperationException("This component has not been initialized with an OBSController.");
            private set
            {
                if (value == _obs) return;
                if (_obs != null)
                {
                    RemoveEvents(_obs);
                    OBSWebsocket? websocket = _obs.Obs;
                    if (websocket != null)
                        RemoveEvents(websocket);
                }
                _obs = value;
                if (value != null)
                {
                    SetEvents(value);
                    OBSWebsocket? websocket = value.Obs;
                    if (websocket != null)
                    {
                        RemoveEvents(websocket);
                        SetEvents(websocket);
                    }
                    if (value.IsConnected)
                    {
                        try
                        {
                            _ = OnConnectAsync(AllTasksCancelSource.Token);
                        }
                        catch (Exception ex)
                        {
                            Logger.log?.Error($"Error running OnConnect after setting Obs in {GetType().Name}: {ex.Message}");
                            Logger.log?.Debug(ex);
                        }
                    }
                }
            }
        }

        private void ConnectionStateChanged(object sender, bool connected)
        {
            OBSController? obs = _obs;
            if (obs != null && obs == sender)
            {
                if (connected)
                {
                    if (AllTasksCancelSource == null)
                        AllTasksCancelSource = new CancellationTokenSource();
                    _ = OnConnectAsync(AllTasksCancelSource?.Token ?? CancellationToken.None);
                }
                else
                    OnDisconnect();
            }
        }
        private readonly object _allCancelLock = new object();
        private CancellationTokenSource? _allTasksCancelSource;
        protected CancellationTokenSource AllTasksCancelSource
        {
            get
            {
                lock (_allCancelLock)
                {
                    if (_allTasksCancelSource == null)
                        _allTasksCancelSource = new CancellationTokenSource();
                    return _allTasksCancelSource;
                }
            }
            private set
            {
                _allTasksCancelSource = value;
            }
        }
        protected void CancelAll()
        {
            CancellationTokenSource? lastSource;
            lock (_allCancelLock)
            {
                lastSource = AllTasksCancelSource;
                AllTasksCancelSource = new CancellationTokenSource();
            }
            if (lastSource != null && lastSource.IsCancellationRequested)
            {
                lastSource.Cancel();
                lastSource.Dispose();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obs"></param>
        /// <returns></returns>
        public virtual async Task InitializeAsync(OBSController obs)
        {
            Obs = obs ?? throw new ArgumentNullException(nameof(obs), "Cannot initialize an OBSComponent with a null OBSController.");
        }

        protected virtual void SetEvents(OBSController obs)
        {
            RemoveEvents(obs);
            obs.ConnectionStateChanged += ConnectionStateChanged;
            obs.ObsCreated += OnObsCreated;
            obs.DestroyingObs += OnDestroyingObs;
        }
        protected abstract void SetEvents(OBSWebsocket obs);
        protected virtual void RemoveEvents(OBSController obs)
        {
            obs.ConnectionStateChanged -= ConnectionStateChanged;
            obs.ObsCreated -= OnObsCreated;
            obs.DestroyingObs -= OnDestroyingObs;
        }
        protected abstract void RemoveEvents(OBSWebsocket obs);

        protected virtual void OnObsCreated(object sender, OBSWebsocket obs)
        {
            CancelAll();
            SetEvents(obs);
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        protected virtual void OnDestroyingObs(object sender, OBSWebsocket obs)
        {
            CancelAll();
            RemoveEvents(obs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        protected virtual async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Connected) return;
            Connected = true;
        }

        protected virtual void OnDisconnect()
        {
            CancelAll();
            Connected = false;
        }

        protected virtual void OnEnable()
        {
            lock (_allCancelLock)
            {
                CancellationTokenSource? lastSource = _allTasksCancelSource;
                if (lastSource == null || lastSource.IsCancellationRequested)
                {
                    _allTasksCancelSource = new CancellationTokenSource();
                }
            }
        }

        protected virtual void OnDisable()
        {
            CancellationTokenSource? lastSource = AllTasksCancelSource;
            if (lastSource != null)
            {
                lastSource.Cancel();
                lastSource.Dispose();
            }
        }

        public static EventHandler CreateWaitHandler(Func<object, EventArgs, bool> resultHandler, CancellationToken cancellationToken, out Task<bool> waitTask)
        {
            TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() => taskCompletion.TrySetCanceled(cancellationToken));
            taskCompletion.Task.ContinueWith((r) => tokenRegistration.Dispose());
            waitTask = taskCompletion.Task;
            return new EventHandler((s, e) =>
            {
                try
                {
                    bool result = resultHandler(s, e);
                    taskCompletion.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletion.TrySetException(ex);
                }
            });
        }

    }
}
