using System;
using System.Collections.Generic;
using System.Web.UI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using OBSControl.OBSComponents;
using OBSControl.UI.Formatters;
using UnityEngine;

namespace OBSControl.UI
{
    public partial class ControlScreen
    {

        #region Properties
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
                NotifyPropertyChanged(nameof(RecordingTextColor));
                NotifyPropertyChanged(nameof(IsNotRecording));
            }
        }

        [UIValue(nameof(RecordingTextColor))]
        public string RecordingTextColor
        {
            get
            {
                return IsRecording switch
                {
                    true => "green",
                    false => "red"
                };
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

        private bool _recordButtonInteractable = true;
        [UIValue(nameof(RecordButtonInteractable))]
        public bool RecordButtonInteractable
        {
            get { return _recordButtonInteractable && IsConnected; }
            set
            {
                if (_recordButtonInteractable == value) return;
                _recordButtonInteractable = value;

                Logger.log.Info($"Record Interactable Changed: {value}");
                NotifyPropertyChanged();
            }
        }

        private bool CoroutineRunning = false;
        private WaitForSeconds RecordInteractableDelay = new WaitForSeconds(2f);
        protected IEnumerator<WaitForSeconds> DelayedRecordInteractableEnable()
        {
            if (CoroutineRunning) yield break;
            CoroutineRunning = true;
            yield return RecordInteractableDelay;
            RecordButtonInteractable = true;
            CoroutineRunning = false;
        }
        #endregion

        #region Actions

        [UIAction(nameof(StartRecording))]
        public async void StartRecording()
        {
            RecordButtonInteractable = false;
            try
            {
                await OBSController.instance.Obs.StartRecording();
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error stopping recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            if (GetOutputStateIsSettled(RecordingController.instance.OutputState))
                StartCoroutine(DelayedRecordInteractableEnable());
        }

        [UIAction(nameof(StopRecording))]
        public async void StopRecording()
        {
            RecordButtonInteractable = false;
            try
            {
                await RecordingController.instance.TryStopRecordingAsync(null, true);
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error stopping recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            if (GetOutputStateIsSettled(RecordingController.instance.OutputState))
                StartCoroutine(DelayedRecordInteractableEnable());
        }
        #endregion

        public static bool GetOutputStateIsSettled(OBSWebsocketDotNet.Types.OutputState state)
        {
            return state switch
            {
                OBSWebsocketDotNet.Types.OutputState.Starting => false,
                OBSWebsocketDotNet.Types.OutputState.Started => true,
                OBSWebsocketDotNet.Types.OutputState.Stopping => false,
                OBSWebsocketDotNet.Types.OutputState.Stopped => true,
                OBSWebsocketDotNet.Types.OutputState.Paused => true,
                OBSWebsocketDotNet.Types.OutputState.Resumed => true,
                _ => true
            };
        }

        #region Event Handlers
        private void OnRecordingStateChanged(object sender, OBSWebsocketDotNet.Types.OutputState e)
        {
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                bool enabled = GetOutputStateIsSettled(e);
                if (enabled)
                    StartCoroutine(DelayedRecordInteractableEnable());
                else
                    RecordButtonInteractable = false;
            });
        }
        #endregion
    }
}
