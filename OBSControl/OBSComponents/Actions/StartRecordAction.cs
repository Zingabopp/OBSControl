using OBSWebsocketDotNet;
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
        public StartRecordAction(OBSWebsocket obs, bool stopPreviousRecording)
            : base(obs)
        {
            _stopPreviousRecording = stopPreviousRecording;
        }

        public override ControlEventType EventType => ControlEventType.StartRecord;



        protected override Task ActionAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
