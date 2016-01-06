using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RxLite
{
    /// <summary>
    ///     ObservableAsPropertyHelper is a class to help ViewModels implement
    ///     "output properties", that is, a property that is backed by an
    ///     Observable. The property will be read-only, but will still fire change
    ///     notifications. This class can be created directly, but is more often created via the
    ///     ToProperty and ObservableToProperty extension methods.
    /// </summary>
    public sealed class ObservableAsPropertyHelper<T> : IHandleObservableErrors, IDisposable
    {
        private readonly IConnectableObservable<T> _source;
        private IDisposable _inner;
        private T _lastValue;

        /// <summary>
        ///     Constructs an ObservableAsPropertyHelper object.
        /// </summary>
        /// <param name="observable">The Observable to base the property on.</param>
        /// <param name="onChanged">
        ///     The action to take when the property
        ///     changes, typically this will call the ViewModel's
        ///     RaisePropertyChanged method.
        /// </param>
        /// <param name="initialValue">The initial value of the property.</param>
        /// <param name="scheduler">
        ///     The scheduler that the notifications will be
        ///     provided on - this should normally be a Dispatcher-based scheduler
        ///     (and is by default)
        /// </param>
        public ObservableAsPropertyHelper(
            IObservable<T> observable,
            Action<T> onChanged,
            T initialValue = default(T),
            IScheduler scheduler = null)
            : this(observable, onChanged, null, initialValue, scheduler)
        {
        }

        /// <summary>
        ///     Constructs an ObservableAsPropertyHelper object.
        /// </summary>
        /// <param name="observable">The Observable to base the property on.</param>
        /// <param name="onChanged">
        ///     The action to take when the property
        ///     changes, typically this will call the ViewModel's
        ///     RaisePropertyChanged method.
        /// </param>
        /// <param name="onChanging">
        ///     The action to take when the property
        ///     changes, typically this will call the ViewModel's
        ///     RaisePropertyChanging method.
        /// </param>
        /// <param name="initialValue">The initial value of the property.</param>
        /// <param name="scheduler">
        ///     The scheduler that the notifications will be
        ///     provided on - this should normally be a Dispatcher-based scheduler
        ///     (and is by default)
        /// </param>
        public ObservableAsPropertyHelper(
            IObservable<T> observable,
            Action<T> onChanged,
            Action<T> onChanging = null,
            T initialValue = default(T),
            IScheduler scheduler = null)
        {
            Contract.Requires(observable != null);
            Contract.Requires(onChanged != null);

            scheduler = scheduler ?? CurrentThreadScheduler.Instance;
            onChanging = onChanging ?? (_ => { });
            _lastValue = initialValue;

            var subj = new ScheduledSubject<T>(scheduler);
            var exSubject = new ScheduledSubject<Exception>(CurrentThreadScheduler.Instance,
                RxApp.DefaultExceptionHandler);

            var firedInitial = false;
            subj.Subscribe(x =>
            {
                // Suppress a non-change between initialValue and the first value
                // from a Subscribe
                if (firedInitial && EqualityComparer<T>.Default.Equals(x, _lastValue))
                    return;

                onChanging(x);
                _lastValue = x;
                onChanged(x);
                firedInitial = true;
            }, exSubject.OnNext);

            ThrownExceptions = exSubject;

            // Fire off an initial RaisePropertyChanged to make sure bindings
            // have a value
            subj.OnNext(initialValue);
            _source = observable.DistinctUntilChanged().Multicast(subj);
        }

        /// <summary>
        ///     The last provided value from the Observable.
        /// </summary>
        public T Value
        {
            get
            {
                _inner = _inner ?? _source.Connect();
                return _lastValue;
            }
        }

        public void Dispose()
        {
            (_inner ?? Disposable.Empty).Dispose();
            _inner = null;
        }

        /// <summary>
        ///     Fires whenever an exception would normally terminate ReactiveUI
        ///     internal state.
        /// </summary>
        public IObservable<Exception> ThrownExceptions { get; }

        /// <summary>
        ///     Constructs a "default" ObservableAsPropertyHelper object. This is
        ///     useful for when you will initialize the OAPH later, but don't want
        ///     bindings to access a null OAPH at startup.
        /// </summary>
        /// <param name="initialValue">The initial (and only) value of the property.</param>
        /// <param name="scheduler">
        ///     The scheduler that the notifications will be
        ///     provided on - this should normally be a Dispatcher-based scheduler
        ///     (and is by default)
        /// </param>
        public static ObservableAsPropertyHelper<T> Default(T initialValue = default(T), IScheduler scheduler = null)
        {
            return new ObservableAsPropertyHelper<T>(Observable.Never<T>(), _ => { }, initialValue, scheduler);
        }
    }
}