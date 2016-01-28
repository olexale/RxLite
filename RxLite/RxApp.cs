using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;

namespace RxLite
{
    public static class RxApp
    {
        static RxApp()
        {
            DefaultExceptionHandler = Observer.Create<Exception>(
                ex =>
                    {
                        // NB: If you're seeing this, it means that an
                        // ObservableAsPropertyHelper or the CanExecute of a
                        // ReactiveCommand ended in an OnError. Instead of silently
                        // breaking, ReactiveUI will halt here if a debugger is attached.
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }

                        MainThreadScheduler.Schedule(
                            () =>
                                {
                                    throw new Exception(
                                        "An OnError occurred on an object (usually ObservableAsPropertyHelper) that would break a binding or command. To prevent this, Subscribe to the ThrownExceptions property of your objects",
                                        ex);
                                });
                    });

            if (MainThreadScheduler == null)
            {
                MainThreadScheduler = DefaultScheduler.Instance;
            }
        }

        /// <summary>
        ///     This Observer is signaled whenever an object that has a
        ///     ThrownExceptions property doesn't Subscribe to that Observable. Use
        ///     Observer.Create to set up what will happen - the default is to crash
        ///     the application with an error message.
        /// </summary>
        public static IObserver<Exception> DefaultExceptionHandler { get; set; }

        /// <summary>
        ///     MainThreadScheduler is the scheduler used to schedule work items that
        ///     should be run "on the UI thread". In normal mode, this will be
        ///     DispatcherScheduler, and in Unit Test mode this will be Immediate,
        ///     to simplify writing common unit tests.
        /// </summary>
        public static IScheduler MainThreadScheduler { get; set; }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        internal static void EnsureInitialized()
        {
            // NB: This method only exists to invoke the static constructor
        }
    }
}