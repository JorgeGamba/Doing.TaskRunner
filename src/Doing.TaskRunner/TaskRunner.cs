using System;
using System.Threading;
using System.Threading.Tasks;

namespace Doing.TaskRunner
{
    public class TaskRunner
    {
        private readonly string _key;
        private readonly Func<string, Task> _getTaskFor;
        private readonly int _spinInterval;
        private readonly Action<Exception> _handle;
        private readonly CancellationToken _cancellationToken;
        private bool _running = true;

        public TaskRunner(string key, Func<string, Task> getTaskFunc, int spinInterval, Action<Exception> handleExceptionFunc, CancellationToken cancellationToken)
        {
            _key = key;
            _getTaskFor = getTaskFunc;
            _spinInterval = spinInterval;
            _handle = handleExceptionFunc;
            _cancellationToken = cancellationToken;
        }

        public async Task Run()
        {
            while (_running)
            {
                var task = _getTaskFor(_key);
                try
                {
                    await task.ConfigureAwait(false);

                }
                catch (OperationCanceledException ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _handle(ex);
                }
                await Task.Delay(_spinInterval, _cancellationToken);
            }
        }

        public void Stop() =>
            _running = false;
    }
}