using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OBSControl.OBSComponents.Actions
{
    public struct TaskEntry
    {
        public readonly Func<CancellationToken, Task> factory;
        public readonly bool asynchronous;
        public TaskEntry(Func<CancellationToken, Task> taskFactory, bool asynchronous)
        {
            this.factory = taskFactory;
            this.asynchronous = asynchronous;
        }
    }

    public class ControlEventArgs : EventArgs
    {
        private List<TaskEntry>? preTasks;
        private List<TaskEntry>? postTasks;

        public ControlEventType EventType { get; }

        public ControlEventArgs(ControlEventType eventType)
        {
            EventType = eventType;
        }
        /// <summary>
        /// Adds a task to run before the ControlAction executes. If <paramref name="asynchronous"/>
        /// is true, ControlAction execution will not wait for the task to finish.
        /// </summary>
        /// <param name="taskFactory"></param>
        /// <param name="asynchronous"></param>
        public void AddPreTask(Func<CancellationToken, Task> taskFactory, bool asynchronous = false)
        {
            if (preTasks == null)
                preTasks = new List<TaskEntry>() { new TaskEntry(taskFactory, asynchronous) };
            else
                preTasks.Add(new TaskEntry(taskFactory, asynchronous));
        }

        /// <summary>
        /// Adds a task to run after the ControlAction executes. If <paramref name="asynchronous"/>
        /// is true, ControlAction will not wait for the task to finish before 'completing'.
        /// </summary>
        /// <param name="taskFactory"></param>
        /// <param name="asynchronous"></param>
        public void AddPostTask(Func<CancellationToken, Task> taskFactory, bool asynchronous = false)
        {
            if (postTasks == null)
                postTasks = new List<TaskEntry>() { new TaskEntry(taskFactory, asynchronous) };
            else
                postTasks.Add(new TaskEntry(taskFactory, asynchronous));
        }

        public IEnumerable<TaskEntry>? GetPreTasks() => preTasks;
        public IEnumerable<TaskEntry>? GetPostTasks() => postTasks;
    }
}
