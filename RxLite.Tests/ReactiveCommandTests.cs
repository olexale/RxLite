using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RxLite.Tests
{
    [TestFixture]
    public class ReactiveCommandTests
    {
        private static ReactiveCommand<object> CreateCommand(
            IObservable<bool> canExecute = null, IScheduler scheduler = null)
        {
            return ReactiveCommand.Create(canExecute, scheduler);
        }

        private static async Task AssertThrowsOnExecuteAsync(IReactiveCommand<Unit> command, Exception exception)
        {
            command.ThrownExceptions.Subscribe();

            var failed = false;

            try
            {
                await command.ExecuteAsync();
            }
            catch (Exception ex)
            {
                failed = ex == exception;
            }

            Assert.True(failed);
        }

        private static IObservable<Unit> ThrowAsync(Exception ex)
        {
            return Observable.Throw<Unit>(ex);
        }

        private static IObservable<Unit> ThrowSync(Exception ex)
        {
            throw ex;
        }

        [Test]
        public void CanExecuteExceptionShouldntPermabreakCommands()
        {
            var canExecute = new Subject<bool>();
            var fixture = CreateCommand(canExecute);

            var exceptions = new List<Exception>();
            var canExecuteStates = new List<bool>();
            fixture.CanExecuteObservable.Subscribe(canExecuteStates.Add);
            fixture.ThrownExceptions.Subscribe(exceptions.Add);

            canExecute.OnNext(false);
            Assert.False(fixture.CanExecute(null));

            canExecute.OnNext(true);
            Assert.True(fixture.CanExecute(null));

            canExecute.OnError(new Exception("Aieeeee!"));

            // The command should latch to false forever
            Assert.False(fixture.CanExecute(null));

            Assert.AreEqual(1, exceptions.Count);
            Assert.AreEqual("Aieeeee!", exceptions[0].Message);

            Assert.AreEqual(false, canExecuteStates[canExecuteStates.Count - 1]);
            Assert.AreEqual(true, canExecuteStates[canExecuteStates.Count - 2]);
        }

        [Test]
        public async Task ExecuteAsyncThrowsExceptionOnAsyncError()
        {
            var exception = new Exception("Aieeeee!");

            var command = ReactiveCommand.CreateAsyncObservable(_ => ThrowAsync(exception));

            await AssertThrowsOnExecuteAsync(command, exception);
        }

        [Test]
        public async Task ExecuteAsyncThrowsExceptionOnError()
        {
            var exception = new Exception("Aieeeee!");

            var command = ReactiveCommand.CreateAsyncObservable(_ => ThrowSync(exception));

            await AssertThrowsOnExecuteAsync(command, exception);
        }

        [Test]
        public void ExecuteDoesNotThrowOnAsyncError()
        {
            var command = ReactiveCommand.CreateAsyncObservable(_ => ThrowAsync(new Exception("Aieeeee!")));

            command.ThrownExceptions.Subscribe();

            command.Execute(null);
        }

        [Test]
        public void ExecuteDoesNotThrowOnError()
        {
            var command = ReactiveCommand.CreateAsyncObservable(_ => ThrowSync(new Exception("Aieeeee!")));

            command.ThrownExceptions.Subscribe();

            command.Execute(null);
        }

        [Test]
        public async void MultipleSubscribesShouldntResultInMultipleNotifications()
        {
            var input = new[] { 1, 2, 1, 2 };
            var fixture = CreateCommand();

            var oddList = new List<int>();
            var evenList = new List<int>();
            fixture.Where(x => ((int)x) % 2 != 0).Subscribe(x => oddList.Add((int)x));
            fixture.Where(x => ((int)x) % 2 == 0).Subscribe(x => evenList.Add((int)x));

            foreach (var i in input)
            {
                await fixture.ExecuteAsyncTask(i);
            }

            Assert.AreEqual(new[] { 1, 1 }, oddList);
            Assert.AreEqual(new[] { 2, 2 }, evenList);
        }

        [Test]
        public void ObservableCanExecuteIsNotNullAfterCanExecuteCalled()
        {
            var fixture = CreateCommand();

            fixture.CanExecute(null);

            Assert.IsNotNull(fixture.CanExecuteObservable);
        }

        [Test]
        public void ObservableCanExecuteIsNotNullAfterCanExecuteChangedEventAdded()
        {
            var fixture = CreateCommand(null);

            fixture.CanExecuteChanged += (sender, args) => { };

            Assert.IsNotNull(fixture.CanExecuteObservable);
        }

        [Test]
        public void ReactiveCommand_DefaultCanExecute_IsTrue()
        {
            var command = CreateCommand();
            Assert.IsTrue(command.CanExecute(true));
        }

        [Test]
        public async void ReactiveCommand_ExcecuteAsync_Works()
        {
            var command = CreateCommand();

            var result = string.Empty;
            command.Subscribe(x => result = x as string);

            await command.ExecuteAsyncTask("Test");
            Assert.AreEqual("Test", result);

            await command.ExecuteAsyncTask("Test2");
            Assert.AreEqual("Test2", result);
        }
    }
}