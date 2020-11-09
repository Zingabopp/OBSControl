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
        public event EventHandler<bool>? ActiveChanged;
        public event EventHandler? Destroyed;
        public bool ActiveAndConnected => isActiveAndEnabled && Connected;
        public bool Connected { get; private set; }
        private OBSController? _obs;
        protected T? GetService<T>() where T : OBSComponent, new() => _obs?.GetOBSComponent<T>();
        protected OBSController Obs
        {
            get => _obs ??= OBSController.instance ?? throw new InvalidOperationException("This component has not been initialized with an OBSController.");
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
                            _ = OnConnectAsync(CancellationToken.None);
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
            if (obs != null && obs == (System.Object)sender)
            {
                if (connected)
                {
                    if (AllTasksCancelSource == null)
                        AllTasksCancelSource = new CancellationTokenSource();
                    _ = OnConnectAsync(CancellationToken.None);
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
            Logger.log?.Debug($"Canceling all previous tasks for {GetType().Name}.");
            CancellationTokenSource? lastSource;
            lock (_allCancelLock)
            {
                lastSource = AllTasksCancelSource;
                AllTasksCancelSource = new CancellationTokenSource();
            }
            if (lastSource != null)
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
        public virtual Task InitializeAsync(OBSController obs)
        {
            Logger.log?.Debug($"Initializing {this.GetType().Name}.");
            Obs = obs ?? throw new ArgumentNullException(nameof(obs), "Cannot initialize an OBSComponent with a null OBSController.");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Sets events for this OBSComponent. Calls <see cref="RemoveEvents(OBSController)"/> first.
        /// Overrides must call 'base.SetEvents(OBSController)' before setting any events.
        /// </summary>
        /// <param name="obs"></param>
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
#if DEBUG
            Logger.log?.Debug($"{GetType().Name}: OnObsCreated.");
#endif
            CancelAll();
            SetEvents(obs);
            // TODO: What was this going to be for?
            //try
            //{
                
            //}
            //catch (Exception ex)
            //{

            //}
        }

        protected virtual void OnDestroyingObs(object sender, OBSWebsocket obs)
        {
#if DEBUG
            Logger.log?.Debug($"{GetType().Name}: OnDestroyingObs.");
#endif
            CancelAll();
            RemoveEvents(obs);
        }

        /// <summary>
        /// Run when an <see cref="OBSController"/> that is connected is assigned to this <see cref="OBSComponent"/>
        /// and when the assigned <see cref="OBSController"/> reports that it has connected to OBS.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        protected virtual Task OnConnectAsync(CancellationToken cancellationToken)
        {
#if DEBUG
            Logger.log?.Debug($"{GetType().Name}: OnConnectAsync.");
#endif
            cancellationToken.ThrowIfCancellationRequested();
            if (!Connected)
                Connected = true;
            gameObject.SetActive(true);
            enabled = true;
            return Task.FromResult(string.Empty);
        }

        /// <summary>
        /// Run when OBSController reports it has disconnected from OBS.
        /// </summary>
        protected virtual void OnDisconnect()
        {
#if DEBUG
            Logger.log?.Debug($"{GetType().Name}: OnDisconnect.");
#endif
            CancelAll();
            gameObject.SetActive(false);
            enabled = false;
            Connected = false;
        }

        protected virtual void Awake()
        {
        }

        protected virtual void Start() { }

        protected virtual void OnEnable()
        {
            Logger.log?.Debug($"Enabling '{gameObject.name}'.");
            lock (_allCancelLock)
            {
                CancellationTokenSource? lastSource = _allTasksCancelSource;
                if (lastSource == null || lastSource.IsCancellationRequested)
                {
                    _allTasksCancelSource = new CancellationTokenSource();
                    lastSource?.Cancel();
                    lastSource?.Dispose();
                }
            }
            ActiveChanged?.Invoke(this, true);
        }

        protected virtual void OnDisable()
        {
            Logger.log?.Debug($"Disabling '{gameObject.name}'.");
            CancellationTokenSource? lastSource = AllTasksCancelSource;
            if (lastSource != null)
            {
                lastSource.Cancel();
            }
            ActiveChanged?.Invoke(this, false);
        }

        protected virtual void OnDestroy()
        {
            Logger.log?.Debug($"Destroying '{gameObject.name}'.");
            Destroyed?.Invoke(this, EventArgs.Empty);
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
