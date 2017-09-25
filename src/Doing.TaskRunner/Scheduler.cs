using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Doing.TaskRunner
{
    public class Scheduler
    {
        private readonly int _spinInterval;
        private readonly Action<Exception> _handledExceptionFunc;
        private readonly CancellationToken _cancellationToken;
        private readonly IDictionary<string, Task> _scheduledTasks = new Dictionary<string, Task>();
        private readonly IDictionary<string, TaskRunner> _taskRunners = new Dictionary<string, TaskRunner>();

        /// <summary>
        /// Creates an instance of Scheduler
        /// </summary>
        /// <param name="spinInterval">Amount in milliseconds to wait between a signal emit and other.</param>
        /// <param name="cancellationToken">The cancellation token that will be checked prior to completing the returned task.</param>
        public Scheduler(int spinInterval, Action<Exception> handledExceptionFunc, CancellationToken cancellationToken)
        {
            _spinInterval = spinInterval;
            _handledExceptionFunc = handledExceptionFunc;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Calls a task for ever waiting the established time interval between calls, until the cancellation token is cancelled.
        /// </summary>
        /// <param name="key">string to be used as identifier against other siblings scheduled tasks.</param>
        /// <param name="task">Task to be managed.</param>
        public void ScheduleForever(string key, Task task)
        {
            _scheduledTasks.Add(key, task);
            var taskRunner = new TaskRunner(key, taskKey => _scheduledTasks[taskKey], _spinInterval, _handledExceptionFunc, _cancellationToken);
            taskRunner.Run();
            _taskRunners.Add(key, taskRunner);
        }
    }
}