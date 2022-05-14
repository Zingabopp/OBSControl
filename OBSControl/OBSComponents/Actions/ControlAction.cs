using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents.Actions
{
    public abstract class ControlAction
    {
        public event EventHandler<ControlEventArgs>? ActionStarting;
        public event EventHandler<ControlEventArgs>? ActionFinished;
        public abstract ControlEventType EventType { get; }

        protected abstract Task ActionAsync(CancellationToken cancellationToken);
        protected abstract void Cleanup();
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await RaiseActionStarting(cancellationToken);
                await ActionAsync(cancellationToken);
                await RaiseActionFinished(cancellationToken);
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Raises the <see cref="ActionStarting"/> event and runs any PreTasks added by subscribers.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task RaiseActionStarting(CancellationToken cancellationToken)
        {
            var action = ActionStarting;
            if(action == null)
                return Task.CompletedTask;
            var args = new ControlEventArgs(EventType);
            action(this, args);
            return RunPreTasks(args, cancellationToken);
        }

        /// <summary>
        /// Raises the <see cref="ActionFinished"/> event and runs any PostTasks added by subscribers.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task RaiseActionFinished(CancellationToken cancellationToken)
        {
            var action = ActionFinished;
            if (action == null)
                return Task.CompletedTask;
            var args = new ControlEventArgs(EventType);
            action(this, args);
            return RunPostTasks(args, cancellationToken);
        }

        protected Task RunPreTasks(ControlEventArgs args, CancellationToken cancellationToken)
        {
            return RunTasks(args.GetPreTasks(), cancellationToken);
        }
        protected Task RunPostTasks(ControlEventArgs args, CancellationToken cancellationToken)
        {
            return RunTasks(args.GetPostTasks(), cancellationToken);
        }

        protected async Task RunTasks(IEnumerable<TaskEntry>? tasks, CancellationToken cancellationToken)
        {
            if (tasks == null)
                return;
            _ = tasks.Where(e => e.asynchronous).Select(e => e.factory(cancellationToken));
            var syncTasks = tasks.Where(e => !e.asynchronous).Select(e => e.factory(cancellationToken));
            await Task.WhenAll(syncTasks).ConfigureAwait(false);
        }
    }
}
