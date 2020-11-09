using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using OBSControl.OBSComponents;
using OBSControl.UI.Formatters;
using UnityEngine;
using UnityEngine.UI;
#nullable enable

namespace OBSControl.UI
{
    [HotReload(@"C:\Users\Jared\source\repos\Zingabopp\OBSControl\OBSControl\UI\ControlScreen.bsml")]

    public partial class ControlScreen : BSMLAutomaticViewController
    {
        private string _connectionState = "Disconnected";
        internal ControlScreenCoordinator? ParentCoordinator;
        private OBSController? _obsController;
        protected OBSController OBSController
        {
            get => _obsController ??= OBSController.instance!;
            set
            {
                if (value != null)
                {
                    if (value != _obsController)
                    {
                        if (_obsController != null)
                            RemoveEvents(_obsController);
                        _obsController = value;
                    }
                }
                else
                    _obsController = value;
            }
        }
        protected SceneController SceneController = null!;
        protected RecordingController RecordingController = null!;
        protected StreamingController StreamingController = null!;
        public ControlScreen()
        {
            OBSController = OBSController.instance!;
            SetComponents(OBSController);
            SetConnectionState(OBSController.IsConnected);
            Logger.log?.Warn($"Created Main: {this.ContentFilePath}");
            CurrentScene = SceneController?.CurrentScene ?? string.Empty;
        }

        protected void SetComponents(OBSController obs)
        {
            SceneController = obs.GetOBSComponent<SceneController>()!;
            RecordingController = obs.GetOBSComponent<RecordingController>()!;
            StreamingController = obs.GetOBSComponent<StreamingController>()!;
        }

        protected void SetEvents(OBSController obs)
        {
            if (obs == null) return;
            RemoveEvents(obs);
            SetComponents(obs);
            obs.ConnectionStateChanged += OnConnectionStateChanged;
            obs.Heartbeat += OnHeartbeat;
            obs.RecordingStateChanged += OnRecordingStateChanged;
            obs.StreamingStateChanged += OnStreamingStateChanged;
            obs.StreamStatus += OnStreamStatus;
            if (SceneController != null)
            {
                SceneController.SceneChanged += OnSceneChange;
            }
        }

        protected void RemoveEvents(OBSController obs)
        {
            if (obs == null) return;
            obs.ConnectionStateChanged -= OnConnectionStateChanged;
            obs.Heartbeat -= OnHeartbeat;
            obs.RecordingStateChanged -= OnRecordingStateChanged;
            obs.StreamingStateChanged -= OnStreamingStateChanged;
            obs.StreamStatus -= OnStreamStatus;
            if (SceneController != null)
            {
                SceneController.SceneChanged -= OnSceneChange;
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            SetEvents(OBSController);
            base.DidActivate(firstActivation, type);
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            RemoveEvents(OBSController);
            base.DidDeactivate(deactivationType);
        }

        protected override void OnDestroy()
        {
            RemoveEvents(OBSController);
            base.OnDestroy();
        }

        private void OnConnectionStateChanged(object sender, bool e)
        {
            SetConnectionState(e);
        }

        private void OnSceneChange(object sender, string? sceneName)
        {
            CurrentScene = sceneName ?? string.Empty;
        }

        private void OnHeartbeat(object sender, OBSWebsocketDotNet.HeartBeatEventArgs e)
        {
            IsRecording = e.Recording;
            IsStreaming = e.Streaming;
            RenderTotalFrames = e.Stats.RenderTotalFrames;
            RenderMissedFrames = e.Stats.RenderMissedFrames;
            OutputTotalFrames = e.Stats.OutputTotalFrames;
            OutputSkippedFrames = e.Stats.OutputSkippedFrames;
            FreeDiskSpace = e.Stats.FreeDiskSpace;
        }

        private int _renderMissedFramesOffset;

        public int RenderMissedFramesOffset
        {
            get { return _renderMissedFramesOffset; }
            set
            {
                if (_renderMissedFramesOffset == value) return;
                _renderMissedFramesOffset = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(RenderMissedFrames));
            }
        }


        [UIAction(nameof(ResetRenderMissedFrames))]
        public void ResetRenderMissedFrames()
        {
            RenderMissedFramesOffset = _renderMissedFrames;
        }

        // For this method of setting the ResourceName, this class must be the first class in the file.
        //public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);
        [UIComponent(nameof(TabSelector))]
        public TabSelector TabSelector { get; protected set; } = null!;

        #region Properties
        [UIValue(nameof(BoolFormatter))]
        public BoolFormatter BoolFormatter = new BoolFormatter();

        private bool _windowExpanded;

        [UIValue(nameof(WindowExpanded))]
        public bool WindowExpanded
        {
            get { return _windowExpanded; }
            set
            {
                if (_windowExpanded == value) return;
                _windowExpanded = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(WindowCollapsed));
            }
        }

        [UIValue(nameof(WindowCollapsed))]
        public bool WindowCollapsed => !WindowExpanded;

        private bool _windowLocked;

        [UIValue(nameof(WindowLocked))]
        public bool WindowLocked
        {
            get { return _windowLocked; }
            set
            {
                if (_windowLocked == value) return;
                _windowLocked = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(WindowUnlocked));
                ParentCoordinator?.SetControlScreenLock(value);
            }
        }
        [UIValue(nameof(WindowUnlocked))]
        public bool WindowUnlocked => !_windowLocked;

        private bool _isConnected;
        [UIValue(nameof(IsConnected))]
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ConnectedTextColor));
                NotifyPropertyChanged(nameof(ConnectButtonText));
                NotifyPropertyChanged(nameof(RecordButtonInteractable));

            }
        }
        private bool _connectButtonInteractable = true;
        [UIValue(nameof(ConnectButtonInteractable))]
        public bool ConnectButtonInteractable
        {
            get => _connectButtonInteractable;
            set
            {
                if (_connectButtonInteractable == value) return;
                _connectButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue(nameof(ConnectButtonText))]
        public string ConnectButtonText
        {
            get
            {
                return IsConnected ? "Disconnect" : "Connect";
            }
        }

        [UIValue(nameof(ConnectedTextColor))]
        public string ConnectedTextColor
        {
            get
            {
                return IsConnected switch
                {
                    true => "green",
                    false => "red"
                };
            }
        }

        [UIValue(nameof(ConnectionState))]
        public string ConnectionState
        {
            get => _connectionState;
            set
            {
                if (_connectionState == value)
                    return;
                _connectionState = value;
                NotifyPropertyChanged();
            }
        }

        private int _renderTotalFrames;

        [UIValue(nameof(RenderTotalFrames))]
        public int RenderTotalFrames
        {
            get { return _renderTotalFrames; }
            set
            {
                if (_renderTotalFrames == value)
                    return;
                _renderTotalFrames = value;
                NotifyPropertyChanged();
            }
        }

        private int _renderMissedFrames;

        [UIValue(nameof(RenderMissedFrames))]
        public int RenderMissedFrames
        {
            get { return _renderMissedFrames - _renderMissedFramesOffset; }
            set
            {
                if (_renderMissedFrames == value)
                    return;
                _renderMissedFrames = value;
                NotifyPropertyChanged();
            }
        }

        private int _outputTotalFrames;

        [UIValue(nameof(OutputTotalFrames))]
        public int OutputTotalFrames
        {
            get { return _outputTotalFrames; }
            set
            {
                if (_outputTotalFrames == value)
                    return;
                _outputTotalFrames = value;
                RecordingOutputFrames = value - StreamingOutputFrames;
                NotifyPropertyChanged();
            }
        }

        private int _outputSkippedFrames;

        [UIValue(nameof(OutputSkippedFrames))]
        public int OutputSkippedFrames
        {
            get { return _outputSkippedFrames; }
            set
            {
                if (_outputSkippedFrames == value)
                    return;
                _outputSkippedFrames = value;
                NotifyPropertyChanged();
            }
        }

        private string? _currentScene;
        [UIValue(nameof(CurrentScene))]
        public string CurrentScene
        {
            get { return _currentScene ?? string.Empty; }
            set
            {
                if (_currentScene == value) return;
                _currentScene = value;
                NotifyPropertyChanged();
            }
        }


        #endregion

        #region Actions
        [UIAction(nameof(ConnectButtonClicked))]
        public async void ConnectButtonClicked()
        {
            ConnectButtonInteractable = false;
            OBSController? controller = OBSController.instance ?? throw new InvalidOperationException("OBSController does not exist.");
            if (controller != null && controller.Obs != null)
            {
                try
                {
                    if (IsConnected)
                        controller.Obs.Disconnect();
                    else
                        await controller.TryConnect(CancellationToken.None);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    Logger.log?.Warn($"Error {(IsConnected ? "disconnecting from " : "connecting to ")} OBS: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
#pragma warning restore CA1031 // Do not catch general exception types
                finally
                {
                    await Task.Delay(2000);
                    ConnectButtonInteractable = true;
                }
            }
            else
            {
                Logger.log?.Warn($"Cannot connect to OBS: {(controller == null ? "OBSController" : "OBS Websocket")} is null.");
                await Task.Delay(2000);
                ConnectButtonInteractable = true;
            }
        }

        [UIAction(nameof(LockWindow))]
        public void LockWindow()
        {
            WindowLocked = true;
        }

        [UIAction(nameof(UnlockWindow))]
        public void UnlockWindow()
        {
            WindowLocked = false;
        }

        [UIAction(nameof(ExpandWindow))]
        public void ExpandWindow()
        {
            WindowExpanded = true;
        }

        [UIAction(nameof(CollapseWindow))]
        public void CollapseWindow()
        {
            WindowExpanded = false;
        }
        #endregion

        public void SetConnectionState(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionState = isConnected switch
            {
                true => "Connected",
                false => "Disconnected"
            };
        }

    }
}
