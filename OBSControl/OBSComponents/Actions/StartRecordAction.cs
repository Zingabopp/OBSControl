using OBSControl.Utilities;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents.Actions
{
    public class StartRecordAction : ObsAction
    {
        private readonly bool _stopPreviousRecording;

        public int Timeout { get; set; } = 5000; // TODO: What if transition > 5 seconds?
        private AsyncEventListenerWithArg<OutputState, OutputState, OutputState> RecordStateListener { get; }

        public StartRecordAction(OBSWebsocket obs, bool stopPreviousRecording)
            : base(obs)
        {
            _stopPreviousRecording = stopPreviousRecording;

            RecordStateListener = new AsyncEventListenerWithArg<OutputState, OutputState, OutputState>((s, state, expectedState) =>
            {
                if (state == expectedState)
                    return new EventListenerResult<OutputState>(state, true);
                else
                    return new EventListenerResult<OutputState>(state, false);
            }, OutputState.Started, Timeout);
        }

        public override ControlEventType EventType => ControlEventType.StartRecord;



        protected async override Task ActionAsync(CancellationToken cancellationToken)
        {
            try
            {
                obs.RecordingStateChanged -= OnRecordingStateChanged;
                obs.RecordingStateChanged += OnRecordingStateChanged;
                RecordStateListener.Reset(OutputState.Started, cancellationToken);
                RecordStateListener.StartListening();
                bool isRecording = (await obs.GetRecordingStatus(cancellationToken)) == OutputState.Started;
                if (isRecording)
                {
                    if (!_stopPreviousRecording)
                        return;
                    var stopAction = new StopRecordAction(obs);
                    await stopAction.ExecuteAsync(cancellationToken);
                }
                await obs.StartRecording(cancellationToken).ConfigureAwait(false);
                await RecordStateListener.Task.ConfigureAwait(false);
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Logger.log?.Debug($"Stop recording was canceled in 'StopRecordAction'.");
            }
            catch (ErrorResponseException ex)
            {
                Logger.log?.Error($"Error trying to stop recording: {ex.Message}");
                if (ex.Message != "recording not active")
                    Logger.log?.Debug(ex);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.log?.Error($"Unexpected exception trying to stop recording: {ex.Message}");
                Logger.log?.Debug(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                RecordStateListener.TrySetCanceled();
            }
        }

        private void OnRecordingStateChanged(object sender, OutputStateChangedEventArgs e)
        {
            RecordStateListener.OnEvent(this, e.OutputState);
        }

        protected override void Cleanup()
        {
            obs.RecordingStateChanged -= OnRecordingStateChanged;
            RecordStateListener.TrySetCanceled();
        }
    }
}
