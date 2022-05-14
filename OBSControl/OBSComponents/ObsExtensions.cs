using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace OBSControl.OBSComponents
{
    public static class ObsExtensions
    {
        public static async Task<OutputState> GetRecordingStatus(this OBSWebsocket obs, CancellationToken cancellationToken)
        {
            var info = await obs.ListOutputs(cancellationToken);
            var fileOutput = info.FirstOrDefault(o => o.Active && o is FileOutputInfo) as FileOutputInfo;
            if (fileOutput == null)
                return OutputState.Stopped;
            return fileOutput.Active ? OutputState.Started : OutputState.Stopped;
        }
    }
}
