using System;
using System.Collections.Generic;
using System.Web.UI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using OBSControl.OBSComponents;
using OBSControl.UI.Formatters;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
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

        private int _recordingOutputFrames;

        public int RecordingOutputFrames
        {
            get { return _recordingOutputFrames; }
            set
            {
                if (_recordingOutputFrames == value) return;
                _recordingOutputFrames = value;
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
#if DEBUG
                Logger.log?.Debug($"Record Interactable Changed: {value}");
#endif
                NotifyPropertyChanged();
            }
        }

        private bool CoroutineRunning = false;
        private WaitForSeconds RecordInteractableDelay = new WaitForSeconds(2f);
        protected IEnumerator<WaitForSeconds> DelayedRecordInteractableEnable()
        {
            if (RecordInteractableDelay == null)
            {
                Logger.log?.Warn("RecordInteractableDelay was null.");
                RecordInteractableDelay = new WaitForSeconds(2f);
            }
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
            OBSWebsocket? obs = OBSController.GetConnectedObs();
            if (obs == null)
            {
                Logger.log?.Warn("Unable to update current scene. OBS not connected.");
                return;
            }
            RecordButtonInteractable = false;
            try
            {
                await RecordingController.TryStartRecordingAsync(RecordActionSourceType.Manual, RecordStartOption.Immediate);
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error stopping recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            if (GetOutputStateIsSettled(RecordingController.OutputState))
                StartCoroutine(DelayedRecordInteractableEnable());
        }

        [UIAction(nameof(StopRecording))]
        public async void StopRecording()
        {
            RecordButtonInteractable = false;
            try
            {
                await RecordingController.TryStopRecordingAsync();
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error stopping recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
            if (GetOutputStateIsSettled(RecordingController.OutputState))
                StartCoroutine(DelayedRecordInteractableEnable());
        }
#endregion

        public static bool GetOutputStateIsSettled(OutputState state)
        {
            return state switch
            {
                OutputState.Starting => false,
                OutputState.Started => true,
                OutputState.Stopping => false,
                OutputState.Stopped => true,
                OutputState.Paused => true,
                OutputState.Resumed => true,
                _ => true
            };
        }

#region Event Handlers
        private void OnRecordingStateChanged(object sender, OutputState e)
        {
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                bool enabled = GetOutputStateIsSettled(e);
                if (enabled)
                    StartCoroutine(DelayedRecordInteractableEnable());
                else
                    RecordButtonInteractable = false;
                if (e == OutputState.Started)
                    IsRecording = true;
                else if (e == OutputState.Stopped)
                    IsRecording = false;
            });
        }
#endregion
    }
}
