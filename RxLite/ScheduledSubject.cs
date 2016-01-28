using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace RxLite
{
    public class ScheduledSubject<T> : ISubject<T>
    {
        private readonly IObserver<T> _defaultObserver;
        private readonly IScheduler _scheduler;
        private readonly ISubject<T> _subject;
        private IDisposable _defaultObserverSub = Disposable.Empty;

        private int _observerRefCount;

        public ScheduledSubject(
            IScheduler scheduler, IObserver<T> defaultObserver = null, ISubject<T> defaultSubject = null)
        {
            this._scheduler = scheduler;
            this._defaultObserver = defaultObserver;
            this._subject = defaultSubject ?? new Subject<T>();

            if (defaultObserver != null)
            {
                this._defaultObserverSub = this._subject.ObserveOn(this._scheduler).Subscribe(this._defaultObserver);
            }
        }

        public void OnCompleted()
        {
            this._subject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this._subject.OnError(error);
        }

        public void OnNext(T value)
        {
            this._subject.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            Interlocked.Exchange(ref this._defaultObserverSub, Disposable.Empty).Dispose();

            Interlocked.Increment(ref this._observerRefCount);

            return new CompositeDisposable(
                this._subject.ObserveOn(this._scheduler).Subscribe(observer), Disposable.Create(
                    () =>
                        {
                            if (Interlocked.Decrement(ref this._observerRefCount) <= 0 && this._defaultObserver != null)
                            {
                                this._defaultObserverSub =
                                    this._subject.ObserveOn(this._scheduler).Subscribe(this._defaultObserver);
                            }
                        }));
        }

        public void Dispose()
        {
            (this._subject as IDisposable)?.Dispose();
        }
    }
}