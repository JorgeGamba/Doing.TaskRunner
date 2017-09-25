using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Doing.BDDExtensions;
using FluentAssertions;
using NUnit.Framework;

namespace Doing.TaskRunner.Specs
{
    public class TaskRunnerSpecs : FeatureSpecifications
    {
        void HandleExceptionFunc(Exception ex) => _handledException = ex;

        public override void When()
        {
            _runner = new TaskRunner(_someKey, _getTaskFunc, _someSpinInterval, HandleExceptionFunc, _cancellationTokenSource.Token);
            _result = _runner.Run();
            Task.Delay(GetTimeoutFor(expectedExecutionTimes: 5, interval: _someSpinInterval)).Wait();
            _runner.Stop();
            _processException = Catch.Exception(() =>
                _result.Wait()
            );
        }

        public class When_follows_a_regular_life_cycle : TaskRunnerSpecs
        {
            public override void Given() =>
                _getTaskFunc = key => Task.Run(() =>
                {
                    if (_startStopwatch.IsRunning)
                        _startStopwatch.Stop();
                    _executionCounter++;
                });

            [Test]
            public void Should_complete_the_process() => 
                _result.IsCompleted.Should().BeTrue();

            [Test]
            public void Should_complete_the_process_successfully() => 
                _result.Status.Should().Be(TaskStatus.RanToCompletion);

            [Test]
            public void Should_the_process_not_throw_any_exception()
            {
                _processException.Should().BeNull();
                _result.Exception.Should().BeNull();
            }

            [Test]
            public void Should_not_pass_any_exception_to_the_handleExceptionFunc() =>
                _handledException.Should().BeNull();

            [Test]
            public void Shouldrun_the_task() =>
                _executionCounter.Should().BePositive();

            [Test]
            public void Shouldrun_the_taskJustBeginning() =>
                _startStopwatch.ElapsedMilliseconds.Should().BeLessThan(_someSpinInterval);

            [Test]
            public void Shouldrun_the_taskOncePerInterval() =>
                _executionCounter.Should().Be(5); // Counting the beggining run, but can not reach the fourth execution


            Stopwatch _startStopwatch = new Stopwatch();
        }

        public class When_a_common_exception_happens : TaskRunnerSpecs
        {
            Exception _someCommonException = new Exception("Some Common Exception");

            public override void Given() =>
                _getTaskFunc = key => Task.Run(() =>
                {
                    _executionCounter++;
                    if (_executionCounter == 2)
                        throw _someCommonException;
                });

            [Test]
            public void Should_complete_the_process() =>
                _result.IsCompleted.Should().BeTrue();

            [Test]
            public void Should_complete_the_process_successfully() =>
                _result.Status.Should().Be(TaskStatus.RanToCompletion);

            [Test]
            public void Should_the_process_not_throw_any_exception()
            {
                _processException.Should().BeNull();
                _result.Exception.Should().BeNull();
            }

            [Test]
            public void ShouldPassTheException_to_the_handleExceptionFunc() =>
                _handledException.Should().NotBeNull();

            [Test]
            public void ShouldPassTheSameCauseException_to_the_handleExceptionFunc() =>
                _handledException.Should().Be(_someCommonException);

            [Test]
            public void Shouldrun_the_taskOncePerIntervalEvenTheFailing() =>
                _executionCounter.Should().Be(5); // Counting the beggining run
        }

        public class When_a_TaskCanceledException_happens : TaskRunnerSpecs
        {
            public override void Given() =>
                _getTaskFunc = key => Task.Run(() =>
                {
                    _executionCounter++;
                    if (_executionCounter == 2)
                        throw new TaskCanceledException();
                });

            [Test]
            public void Should_complete_the_process() =>
                _result.IsCompleted.Should().BeTrue();

            [Test]
            public void Should_complete_the_processAsCanceled() =>
                _result.Status.Should().Be(TaskStatus.Canceled);

            [Test]
            public void Should_the_process_throw_an_exception() =>
                _processException.Should().NotBeNull();

            [Test]
            public void Should_the_process_throw_an_exception_Of_type_TaskCanceledException() =>
                _processException.InnerException.Should().BeOfType<TaskCanceledException>();

            [Test]
            public void Should_not_pass_any_exception_to_the_handleExceptionFunc() =>
                _handledException.Should().BeNull();

            [Test]
            public void Should_not_run_the_task_more_times_later() =>
                _executionCounter.Should().Be(2); // Counting the beggining run
        }

        public class When_is_cancelled_by_CancellationToken_happens : TaskRunnerSpecs
        {
            public override void Given() =>
                _getTaskFunc = key => Task.Run(() =>
                {
                    _executionCounter++;
                    if (_executionCounter == 2)
                        _cancellationTokenSource.Cancel();
                });

            [Test]
            public void Should_complete_the_process() =>
                _result.IsCompleted.Should().BeTrue();

            [Test]
            public void Should_complete_the_processAsCanceled() =>
                _result.Status.Should().Be(TaskStatus.Canceled);

            [Test]
            public void Should_the_process_throw_an_exception() =>
                _processException.Should().NotBeNull();

            [Test]
            public void Should_the_process_throw_an_exception_Of_type_TaskCanceledException() =>
                _processException.InnerException.Should().BeOfType<TaskCanceledException>();

            [Test]
            public void Should_not_pass_any_exception_to_the_handleExceptionFunc() =>
                _handledException.Should().BeNull();

            [Test]
            public void Should_not_run_the_task_more_times_later() =>
                _executionCounter.Should().Be(2); // Counting the beggining run
        }

        public class When_an_OperationCanceledException_happens : TaskRunnerSpecs
        {
            public override void Given() =>
                _getTaskFunc = key => Task.Run(() =>
                {
                    _executionCounter++;
                    if (_executionCounter == 2)
                        throw new OperationCanceledException();
                });

            [Test]
            public void Should_complete_the_process() =>
                _result.IsCompleted.Should().BeTrue();

            [Test]
            public void Should_complete_the_processAsCanceled() =>
                _result.Status.Should().Be(TaskStatus.Canceled);

            [Test]
            public void Should_the_process_throw_an_exception() =>
                _processException.Should().NotBeNull();

            [Test]
            public void Should_the_process_throw_an_exceptionOfTypeCanceledException_the_process() =>
                _processException.InnerException.Should().BeOfType<TaskCanceledException>();

            [Test]
            public void Should_not_pass_any_exception_to_the_handleExceptionFunc() =>
                _handledException.Should().BeNull();

            [Test]
            public void Should_not_run_the_task_more_times_later() =>
                _executionCounter.Should().Be(2); // Counting the beggining run
        }

        public class When_is_Stopped : TaskRunnerSpecs
        {
            public override void Given() =>
                _getTaskFunc = key => Task.Run(() =>
                {
                    _executionCounter++;
                    if (_executionCounter == 2)
                        _runner.Stop();
                });

            [Test]
            public void Should_complete_the_process() =>
                _result.IsCompleted.Should().BeTrue();

            [Test]
            public void Should_complete_the_process_successfully() =>
                _result.Status.Should().Be(TaskStatus.RanToCompletion);

            [Test]
            public void Should_the_process_not_throw_any_exception()
            {
                _processException.Should().BeNull();
                _result.Exception.Should().BeNull();
            }

            [Test]
            public void Should_not_pass_any_exception_to_the_handleExceptionFunc() =>
                _handledException.Should().BeNull();

            [Test]
            public void Should_not_run_the_task_more_times_later() =>
                _executionCounter.Should().Be(2); // Counting the beggining run
        }


        static int GetTimeoutFor(int expectedExecutionTimes, int interval) => interval * expectedExecutionTimes - interval / 2;

        Func<string, Task> _getTaskFunc;
        int _executionCounter;
        TaskRunner _runner;
        Exception _processException;
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        Task _result;
        Exception _handledException;

        static string _someKey = "some key";
        static int _someSpinInterval = 100;
    }
}