using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RxLite
{
    public interface IReactiveObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        new event PropertyChangingEventHandler PropertyChanging;

        new event PropertyChangedEventHandler PropertyChanged;

        void RaisePropertyChanging(PropertyChangingEventArgs args);

        void RaisePropertyChanged(PropertyChangedEventArgs args);
    }

    [Preserve(AllMembers = true)]
    public static class IReactiveObjectExtensions
    {
        private static readonly ConditionalWeakTable<IReactiveObject, IExtensionState<IReactiveObject>> State =
            new ConditionalWeakTable<IReactiveObject, IExtensionState<IReactiveObject>>();

        public static IObservable<IReactivePropertyChangedEventArgs<TSender>> GetChangedObservable<TSender>(
            this TSender This) where TSender : IReactiveObject
        {
            var val = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));
            return val.Changed.Cast<IReactivePropertyChangedEventArgs<TSender>>();
        }

        public static IObservable<IReactivePropertyChangedEventArgs<TSender>> GetChangingObservable<TSender>(
            this TSender This) where TSender : IReactiveObject
        {
            var val = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));
            return val.Changing.Cast<IReactivePropertyChangedEventArgs<TSender>>();
        }

        public static IObservable<Exception> GetThrownExceptionsObservable<TSender>(this TSender This)
            where TSender : IReactiveObject
        {
            var s = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));
            return s.ThrownExceptions;
        }

        public static void raisePropertyChanging<TSender>(this TSender This, string propertyName)
            where TSender : IReactiveObject
        {
            Contract.Requires(propertyName != null);

            var s = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));

            s.RaisePropertyChanging(propertyName);
        }

        internal static void raisePropertyChanged<TSender>(this TSender This, string propertyName)
            where TSender : IReactiveObject
        {
            Contract.Requires(propertyName != null);

            var s = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));

            s.RaisePropertyChanged(propertyName);
        }

        public static IDisposable SuppressChangeNotifications<TSender>(this TSender This)
            where TSender : IReactiveObject
        {
            var s = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));

            return s.SuppressChangeNotifications();
        }

        public static bool AreChangeNotificationsEnabled<TSender>(this TSender This) where TSender : IReactiveObject
        {
            var s = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));

            return s.AreChangeNotificationsEnabled();
        }

        public static IDisposable DelayChangeNotifications<TSender>(this TSender This) where TSender : IReactiveObject
        {
            var s = State.GetValue(This, key => (IExtensionState<IReactiveObject>)new ExtensionState<TSender>(This));

            return s.DelayChangeNotifications();
        }

        /// <summary>
        ///     RaiseAndSetIfChanged fully implements a Setter for a read-write
        ///     property on a ReactiveObject, using CallerMemberName to raise the notification
        ///     and the ref to the backing field to set the property.
        /// </summary>
        /// <typeparam name="TObj">The type of the This.</typeparam>
        /// <typeparam name="TRet">The type of the return value.</typeparam>
        /// <param name="This">The <see cref="ReactiveObject" /> raising the notification.</param>
        /// <param name="backingField">
        ///     A Reference to the backing field for this
        ///     property.
        /// </param>
        /// <param name="newValue">The new value.</param>
        /// <param name="propertyName">
        ///     The name of the property, usually
        ///     automatically provided through the CallerMemberName attribute.
        /// </param>
        /// <returns>The newly set value, normally discarded.</returns>
        public static TRet RaiseAndSetIfChanged<TObj, TRet>(
            this TObj This, ref TRet backingField, TRet newValue, [CallerMemberName] string propertyName = null)
            where TObj : IReactiveObject
        {
            Contract.Requires(propertyName != null);

            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return newValue;
            }

            This.raisePropertyChanging(propertyName);
            backingField = newValue;
            This.raisePropertyChanged(propertyName);
            return newValue;
        }

        /// <summary>
        ///     Use this method in your ReactiveObject classes when creating custom
        ///     properties where raiseAndSetIfChanged doesn't suffice.
        /// </summary>
        /// <param name="This">The instance of ReactiveObject on which the property has changed.</param>
        /// <param name="propertyName">
        ///     A string representing the name of the property that has been changed.
        ///     Leave <c>null</c> to let the runtime set to caller member name.
        /// </param>
        public static void RaisePropertyChanged<TSender>(
            this TSender This, [CallerMemberName] string propertyName = null) where TSender : IReactiveObject
        {
            This.raisePropertyChanged(propertyName);
        }

        /// <summary>
        ///     Use this method in your ReactiveObject classes when creating custom
        ///     properties where raiseAndSetIfChanged doesn't suffice.
        /// </summary>
        /// <param name="This">The instance of ReactiveObject on which the property has changed.</param>
        /// <param name="propertyName">
        ///     A string representing the name of the property that has been changed.
        ///     Leave <c>null</c> to let the runtime set to caller member name.
        /// </param>
        public static void RaisePropertyChanging<TSender>(
            this TSender This, [CallerMemberName] string propertyName = null) where TSender : IReactiveObject
        {
            This.raisePropertyChanging(propertyName);
        }

        // Filter a list of change notifications, returning the last change for each PropertyName in original order.
        private static IEnumerable<IReactivePropertyChangedEventArgs<TSender>> dedup<TSender>(
            IList<IReactivePropertyChangedEventArgs<TSender>> batch)
        {
            if (batch.Count <= 1)
            {
                return batch;
            }

            var seen = new HashSet<string>();
            var unique = new LinkedList<IReactivePropertyChangedEventArgs<TSender>>();

            for (var i = batch.Count - 1; i >= 0; i--)
            {
                if (seen.Add(batch[i].PropertyName))
                {
                    unique.AddFirst(batch[i]);
                }
            }

            return unique;
        }

        private class ExtensionState<TSender> : IExtensionState<TSender>
            where TSender : IReactiveObject
        {
            private readonly ISubject<IReactivePropertyChangedEventArgs<TSender>> _changedSubject;
            private readonly ISubject<IReactivePropertyChangedEventArgs<TSender>> _changingSubject;

            private readonly TSender _sender;
            private readonly ISubject<Unit> _startDelayNotifications;
            private readonly ISubject<Exception> _thrownExceptions;
            private long _changeNotificationsDelayed;
            private long _changeNotificationsSuppressed;

            /// <summary>
            ///     Initializes a new instance of the <see cref="ExtensionState{TSender}" /> class.
            /// </summary>
            public ExtensionState(TSender sender)
            {
                this._sender = sender;
                this._changingSubject = new Subject<IReactivePropertyChangedEventArgs<TSender>>();
                this._changedSubject = new Subject<IReactivePropertyChangedEventArgs<TSender>>();
                this._startDelayNotifications = new Subject<Unit>();
                this._thrownExceptions = new ScheduledSubject<Exception>(
                    Scheduler.Immediate, RxApp.DefaultExceptionHandler);

                this.Changed =
                    Observable.Publish(
                        this._changedSubject.Buffer(
                            this._changedSubject.Where(_ => !this.AreChangeNotificationsDelayed())
                                .Select(_ => Unit.Default)
                                .Merge(this._startDelayNotifications)).SelectMany(dedup)).RefCount();

                this.Changing =
                    Observable.Publish(
                        this._changingSubject.Buffer(
                            this._changingSubject.Where(_ => !this.AreChangeNotificationsDelayed())
                                .Select(_ => Unit.Default)
                                .Merge(this._startDelayNotifications)).SelectMany(dedup)).RefCount();
            }

            public IObservable<IReactivePropertyChangedEventArgs<TSender>> Changing { get; }

            public IObservable<IReactivePropertyChangedEventArgs<TSender>> Changed { get; }

            public IObservable<Exception> ThrownExceptions => this._thrownExceptions;

            public bool AreChangeNotificationsEnabled()
            {
                return (Interlocked.Read(ref this._changeNotificationsSuppressed) == 0);
            }

            public bool AreChangeNotificationsDelayed()
            {
                return (Interlocked.Read(ref this._changeNotificationsDelayed) > 0);
            }

            /// <summary>
            ///     When this method is called, an object will not fire change
            ///     notifications (neither traditional nor Observable notifications)
            ///     until the return value is disposed.
            /// </summary>
            /// <returns>
            ///     An object that, when disposed, reenables change
            ///     notifications.
            /// </returns>
            public IDisposable SuppressChangeNotifications()
            {
                Interlocked.Increment(ref this._changeNotificationsSuppressed);
                return Disposable.Create(() => Interlocked.Decrement(ref this._changeNotificationsSuppressed));
            }

            public IDisposable DelayChangeNotifications()
            {
                if (Interlocked.Increment(ref this._changeNotificationsDelayed) == 1)
                {
                    this._startDelayNotifications.OnNext(Unit.Default);
                }

                return Disposable.Create(
                    () =>
                        {
                            if (Interlocked.Decrement(ref this._changeNotificationsDelayed) == 0)
                            {
                                this._startDelayNotifications.OnNext(Unit.Default);
                            }
                        });
            }

            public void RaisePropertyChanging(string propertyName)
            {
                if (!this.AreChangeNotificationsEnabled())
                {
                    return;
                }

                var changing = new ReactivePropertyChangingEventArgs<TSender>(this._sender, propertyName);
                this._sender.RaisePropertyChanging(changing);

                this.NotifyObservable(changing, this._changingSubject);
            }

            public void RaisePropertyChanged(string propertyName)
            {
                if (!this.AreChangeNotificationsEnabled())
                {
                    return;
                }

                var changed = new ReactivePropertyChangedEventArgs<TSender>(this._sender, propertyName);
                this._sender.RaisePropertyChanged(changed);

                this.NotifyObservable(changed, this._changedSubject);
            }

            private void NotifyObservable<T>(T item, ISubject<T> subject)
            {
                try
                {
                    subject.OnNext(item);
                }
                catch (Exception ex)
                {
                    this._thrownExceptions.OnNext(ex);
                }
            }
        }

        private interface IExtensionState<out TSender>
            where TSender : IReactiveObject
        {
            IObservable<IReactivePropertyChangedEventArgs<TSender>> Changing { get; }

            IObservable<IReactivePropertyChangedEventArgs<TSender>> Changed { get; }

            IObservable<Exception> ThrownExceptions { get; }

            void RaisePropertyChanging(string propertyName);

            void RaisePropertyChanged(string propertyName);

            bool AreChangeNotificationsEnabled();

            IDisposable SuppressChangeNotifications();

            bool AreChangeNotificationsDelayed();

            IDisposable DelayChangeNotifications();
        }
    }
}