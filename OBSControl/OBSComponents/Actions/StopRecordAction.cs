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
    public class StopRecordAction : ObsAction
    {
        private static readonly object _lock = new object();
        private static bool WaitingToStop;
        private static Task? StopRecordingTask;
        public override ControlEventType EventType => ControlEventType.StopRecord;

        public StopRecordAction(OBSWebsocket obs)
            : base(obs)
        {
        }

        protected async override Task ActionAsync(CancellationToken cancellationToken)
        {
            Task? existingStopRecordTask = null;
            lock(_lock)
            {
                if (WaitingToStop)
                    existingStopRecordTask = StopRecordingTask;
                else
                    WaitingToStop = true;
            }

            bool prevTaskCompleted = existingStopRecordTask?.IsCompleted ?? true;
            bool isRecording = (await obs.GetRecordingStatus(cancellationToken)) == OutputState.Started;
            if (!isRecording)
                return;

            try
            {
                await obs.StopRecording(cancellationToken);
                await Task.Delay(500, cancellationToken);
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

            }
        }

        protected override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
