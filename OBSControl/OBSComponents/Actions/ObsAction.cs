using OBSWebsocketDotNet;
using System;

namespace OBSControl.OBSComponents.Actions
{
    public abstract class ObsAction : ControlAction
    {
        protected readonly OBSWebsocket obs;
        protected ObsAction(OBSWebsocket obs)
        {
            this.obs = obs ?? throw new ArgumentNullException(nameof(obs));
        }
    }
}
