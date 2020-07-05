using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using OBSControl.OBSComponents;
using OBSControl.UI.Formatters;

namespace OBSControl.UI
{
    [HotReload(@"C:\Users\Jared\source\repos\Zingabopp\OBSControl\OBSControl\UI\ControlScreen_Main.bsml")]

    public class ControlScreen_Main : BSMLAutomaticViewController
    {
        private string connectionState;
        public ControlScreen_Main()
        {
            OBSController.instance.ConnectionStateChanged += Instance_ConnectionStateChanged;
            OBSController.instance.Heartbeat += Instance_Heartbeat;
            SetConnectionState(OBSController.instance.IsConnected);
            Logger.log.Warn($"Created Main: {this.ContentFilePath}");
        }

        private void Instance_Heartbeat(object sender, OBSWebsocketDotNet.Types.Heartbeat e)
        {
            IsRecording = e.Recording;
            IsStreaming = e.Streaming;
            RenderTotalFrames = e.Stats.RenderTotalFrames;
            RenderMissedFrames = e.Stats.RenderMissedFrames;
            OutputTotalFrames = e.Stats.OutputTotalFrames;
            OutputSkippedFrames = e.Stats.OutputSkippedFrames;
            FreeDiskSpace = e.Stats.FreeDiskSpace;
        }

        private void Instance_ConnectionStateChanged(object sender, bool e)
        {
            SetConnectionState(e);
        }

        
        // For this method of setting the ResourceName, this class must be the first class in the file.
        //public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);
        [UIComponent(nameof(TabSelector))]
        public TabSelector TabSelector { get; protected set; }

        #region Properties
        [UIValue(nameof(BoolFormatter))]
        public BoolFormatter BoolFormatter = new BoolFormatter();

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
            }
        }

        [UIValue(nameof(ConnectionState))]
        public string ConnectionState
        {
            get => connectionState;
            set
            {
                if (connectionState == value)
                    return;
                connectionState = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isRecording;

        [UIValue(nameof(IsRecording))]
        public bool IsRecording
        {
            get { return _isRecording; }
            set
            {
                if (_isRecording == value)
                    return;
                _isRecording = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotRecording));
            }
        }
        [UIValue(nameof(IsNotRecording))]
        public bool IsNotRecording
        {
            get => !_isRecording;
            set => IsRecording = !value;
        }

        private bool _isStreaming;

        [UIValue(nameof(IsStreaming))]
        public bool IsStreaming
        {
            get { return _isStreaming; }
            set
            {
                if (_isStreaming == value)
                    return;
                _isStreaming = value;
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
            get { return _renderMissedFrames; }
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

        private double _freeDiskSpace;
        [UIValue(nameof(FreeDiskSpace))]
        public double FreeDiskSpace
        {
            get { return _freeDiskSpace; }
            set
            {
                if (_freeDiskSpace == value)
                    return;
                _freeDiskSpace = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Actions

        [UIAction(nameof(StartRecording))]
        public void StartRecording()
        {
            RecordingController.instance.StartRecordingLevel();
        }

        [UIAction(nameof(StopRecording))]
        public void StopRecording()
        {
            RecordingController.instance.TryStopRecordingAsync(null, true);
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
