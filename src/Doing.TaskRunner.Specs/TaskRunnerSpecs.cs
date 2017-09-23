using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Doing.BDDExtensions;
using FluentAssertions;
using NUnit.Framework;
#pragma warning disable 4014

namespace Doing.TaskRunner.Specs
{
    public class TaskRunnerSpecs : FeatureSpecifications
    {
        public class When_Is_Running: TaskRunnerSpecs
        {
            public override void Given()
            {
                Task GetTaskFor(string key) => Task.Run(() =>
                {
                    if (_startStopwatch.IsRunning)
                        _startStopwatch.Stop();
                    _executionCounter++;
                });
                _runner = new TaskRunner(_someKey, GetTaskFor, _someSpinInterval, _someCancellationToken);
                _startStopwatch.Start();
            }

            public override void When()
            {
                _runner.Run();
                Task.Delay(GetTimeoutFor(expectedExecutionTimes: 3, interval: _someSpinInterval)).Wait();
                _runner.Stop();
            }

            [Test]
            public void Should_run_the_task() =>
                _executionCounter.Should().BePositive();

            [Test]
            public void Should_run_the_task_just_beginning() =>
                _startStopwatch.ElapsedMilliseconds.Should().BeLessThan(_someSpinInterval);

            [Test]
            public void Should_run_the_task_once_per_interval() =>
                _executionCounter.Should().Be(3); // Counting the beggining run, but can not reach the fourth execution


            Stopwatch _startStopwatch = new Stopwatch();
        }

        public class When_Is_Stopped : TaskRunnerSpecs
        {
            public override void Given()
            {
                Task GetTaskFor(string key) => Task.Run(() => _executionCounter++);
                _runner = new TaskRunner(_someKey, GetTaskFor, _someSpinInterval, _someCancellationToken);
                _runner.Run();
            }

            public override void When()
            {
                Task.Delay(GetTimeoutFor(expectedExecutionTimes: 3, interval: _someSpinInterval)).Wait();
                _runner.Stop();
                Task.Delay(_someSpinInterval * 2).Wait();
            }

            [Test]
            public void Should_not_run_the_task_more_times_later() =>
                _executionCounter.Should().Be(3); // Counting the beggining run
        }


        static int GetTimeoutFor(int expectedExecutionTimes, int interval) => interval * expectedExecutionTimes - interval / 2;

        int _executionCounter;
        TaskRunner _runner;

        static string _someKey = "some key";
        static int _someSpinInterval = 100;
        static CancellationToken _someCancellationToken = CancellationToken.None;
    }
}