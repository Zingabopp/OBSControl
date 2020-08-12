using System;
using System.Collections.Generic;
using System.Web.UI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using OBSControl.OBSComponents;
using OBSControl.UI.Formatters;
using OBSWebsocketDotNet.Types;
using UnityEngine;

namespace OBSControl.UI
{
    public partial class ControlScreen
    {
        protected StreamStatus CurrentStreamStatus;
        [UIValue(nameof(TimeFormatter))]
        public readonly TimeFormatter TimeFormatter = new TimeFormatter();
        #region Properties

        [UIValue(nameof(StreamingTextColor))]
        public string StreamingTextColor
        {
            get
            {
                return IsStreaming switch
                {
                    true => "green",
                    false => "red"
                };
            }
        }

        [UIValue(nameof(IsNotStreaming))]
        public bool IsNotStreaming
        {
            get => !_isStreaming;
            set => IsStreaming = !value;
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
                NotifyPropertyChanged(nameof(IsNotStreaming));
                NotifyPropertyChanged(nameof(StreamingTextColor));
            }
        }

        private bool _streamButtonInteractable = true;
        [UIValue(nameof(StreamButtonInteractable))]
        public bool StreamButtonInteractable
        {
            get { return _streamButtonInteractable && IsConnected; }
            set
            {
                if (_streamButtonInteractable == value) return;
                _streamButtonInteractable = value;
#if DEBUG
                Logger.log?.Debug($"Stream Interactable Changed: {value}");
#endif
                NotifyPropertyChanged();
            }
        }

        [UIValue(nameof(StreamTime))]
        public int StreamTime => CurrentStreamStatus?.TotalStreamTime ?? 0;
        [UIValue(nameof(Bitrate))]
        public float Bitrate => (CurrentStreamStatus?.KbitsPerSec ?? 0) / 1024f;
        [UIValue(nameof(Strain))]
        public float Strain => CurrentStreamStatus?.Strain ?? 0;
        [UIValue(nameof(StreamingDroppedFrames))]
        public int StreamingDroppedFrames => CurrentStreamStatus?.DroppedFrames ?? 0; 
        [UIValue(nameof(StreamingOutputFrames))]
        public int StreamingOutputFrames => CurrentStreamStatus?.TotalFrames ?? 0;


        #endregion

        #region Actions

        [UIAction(nameof(StartStreaming))]
        public async void StartStreaming()
        {
            StreamButtonInteractable = false;
            try
            {
                await OBSController.instance.Obs.StartStreaming();
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error stopping streaming: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            if (GetOutputStateIsSettled(StreamingController.instance.OutputState))
                StartCoroutine(DelayedStreamInteractableEnable(false));
        }

        [UIAction(nameof(StopStreaming))]
        public async void StopStreaming()
        {
            StreamButtonInteractable = false;
            try
            {
                await StreamingController.instance.StopStreaming();
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error stopping streaming: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            if (GetOutputStateIsSettled(StreamingController.instance.OutputState))
                StartCoroutine(DelayedStreamInteractableEnable(true));
        }
        #endregion

        #region Event Handlers
        private void OnStreamingStateChanged(object sender, OutputState e)
        {
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                bool enabled = GetOutputStateIsSettled(e);
                if (enabled)
                    StartCoroutine(DelayedStreamInteractableEnable(e == OutputState.Stopped));
                else
                    StreamButtonInteractable = false;
            });
        }

        private void OnStreamStatus(object sender, StreamStatus e)
        {
            CurrentStreamStatus = e;
            NotifyPropertyChanged(nameof(StreamTime));
            NotifyPropertyChanged(nameof(Bitrate));
            NotifyPropertyChanged(nameof(Strain));
            NotifyPropertyChanged(nameof(StreamingDroppedFrames));
            NotifyPropertyChanged(nameof(StreamingOutputFrames));
        }
        #endregion

        private bool StreamButtonCoroutineRunning = false;
        private WaitForSeconds StreamInteractableDelay = new WaitForSeconds(2f);
        protected IEnumerator<WaitForSeconds> DelayedStreamInteractableEnable(bool stopped)
        {
            if (StreamInteractableDelay == null)
            {
                Logger.log?.Warn("StreamInteractableDelay was null.");
                StreamInteractableDelay = new WaitForSeconds(2f);
            }
            if (StreamButtonCoroutineRunning) yield break;
            StreamButtonCoroutineRunning = true;
            yield return StreamInteractableDelay;
            StreamButtonInteractable = true;
            StreamButtonCoroutineRunning = false;
            CurrentStreamStatus = null;
            NotifyPropertyChanged(nameof(Bitrate));
            NotifyPropertyChanged(nameof(Strain));
        }
    }
}
