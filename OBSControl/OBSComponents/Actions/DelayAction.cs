using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents.Actions
{
    public sealed class DelayAction : ControlAction
    {
        public override ControlEventType EventType => ControlEventType.Delay;
        public readonly TimeSpan Delay;

        public DelayAction(TimeSpan delay)
        {
            Delay = delay;
        }

        public DelayAction(int milliseconds)
        {
            Delay = TimeSpan.FromMilliseconds(milliseconds);
        }

        protected override Task ActionAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(Delay, cancellationToken);
        }

        protected override void Cleanup()
        {
        }
    }
}
