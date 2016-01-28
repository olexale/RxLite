using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;

namespace RxLite
{
    /// <summary>
    ///     IObservedChange is a generic interface that is returned from WhenAny()
    ///     Note that it is used for both Changing (i.e.'before change')
    ///     and Changed Observables.
    /// </summary>
    public interface IObservedChange<out TSender, out TValue>
    {
        /// <summary>
        ///     The object that has raised the change.
        /// </summary>
        TSender Sender { get; }

        /// <summary>
        ///     The expression of the member that has changed on Sender.
        /// </summary>
        Expression Expression { get; }

        /// <summary>
        ///     The value of the property that has changed. IMPORTANT NOTE: This
        ///     property is often not set for performance reasons, unless you have
        ///     explicitly requested an Observable for a property via a method such
        ///     as ObservableForProperty. To retrieve the value for the property,
        ///     use the GetValue() extension method.
        /// </summary>
        TValue Value { get; }
    }

    /// <summary>
    ///     A data-only version of IObservedChange
    /// </summary>
    public class ObservedChange<TSender, TValue> : IObservedChange<TSender, TValue>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ObservedChange{TSender, TValue}" /> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="expression">Expression describing the member.</param>
        /// <param name="value">The value.</param>
        public ObservedChange(TSender sender, Expression expression, TValue value = default(TValue))
        {
            this.Sender = sender;
            this.Expression = expression;
            this.Value = value;
        }

        /// <summary>
        /// </summary>
        public TSender Sender { get; }

        /// <summary>
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        /// </summary>
        public TValue Value { get; }
    }

    /// <summary>
    ///     IReactiveNotifyPropertyChanged represents an extended version of
    ///     INotifyPropertyChanged that also exposes typed Observables.
    /// </summary>
    public interface IReactiveNotifyPropertyChanged<out TSender>
    {
        /// <summary>
        ///     Represents an Observable that fires *before* a property is about to
        ///     be changed. Note that this should not fire duplicate change notifications if a
        ///     property is set to the same value multiple times.
        /// </summary>
        IObservable<IReactivePropertyChangedEventArgs<TSender>> Changing { get; }

        /// <summary>
        ///     Represents an Observable that fires *after* a property has changed.
        ///     Note that this should not fire duplicate change notifications if a
        ///     property is set to the same value multiple times.
        /// </summary>
        IObservable<IReactivePropertyChangedEventArgs<TSender>> Changed { get; }

        /// <summary>
        ///     When this method is called, an object will not fire change
        ///     notifications (neither traditional nor Observable notifications)
        ///     until the return value is disposed.
        /// </summary>
        /// <returns>
        ///     An object that, when disposed, reenables change
        ///     notifications.
        /// </returns>
        IDisposable SuppressChangeNotifications();
    }

    /// <summary>
    ///     IReactivePropertyChangedEventArgs is a generic interface that
    ///     is used to wrap the NotifyPropertyChangedEventArgs and gives
    ///     information about changed properties. It includes also
    ///     the sender of the notification.
    ///     Note that it is used for both Changing (i.e.'before change')
    ///     and Changed Observables.
    /// </summary>
    public interface IReactivePropertyChangedEventArgs<out TSender>
    {
        /// <summary>
        ///     The name of the property that has changed on Sender.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        ///     The object that has raised the change.
        /// </summary>
        TSender Sender { get; }
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="TSender"></typeparam>
    public class ReactivePropertyChangingEventArgs<TSender> : PropertyChangingEventArgs,
                                                              IReactivePropertyChangedEventArgs<TSender>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ReactivePropertyChangingEventArgs{TSender}" /> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="propertyName">Name of the property.</param>
        public ReactivePropertyChangingEventArgs(TSender sender, string propertyName)
            : base(propertyName)
        {
            this.Sender = sender;
        }

        /// <summary>
        /// </summary>
        public TSender Sender { get; }
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="TSender"></typeparam>
    public class ReactivePropertyChangedEventArgs<TSender> : PropertyChangedEventArgs,
                                                             IReactivePropertyChangedEventArgs<TSender>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ReactivePropertyChangedEventArgs{TSender}" /> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="propertyName">Name of the property.</param>
        public ReactivePropertyChangedEventArgs(TSender sender, string propertyName)
            : base(propertyName)
        {
            this.Sender = sender;
        }

        /// <summary>
        /// </summary>
        public TSender Sender { get; }
    }

    /// <summary>
    ///     This interface is implemented by RxUI objects which are given
    ///     IObservables as input - when the input IObservables OnError, instead of
    ///     disabling the RxUI object, we catch the IObservable and pipe it into
    ///     this property.
    ///     Normally this IObservable is implemented with a ScheduledSubject whose
    ///     default Observer is RxApp.DefaultExceptionHandler - this means, that if
    ///     you aren't listening to ThrownExceptions and one appears, the exception
    ///     will appear on the UI thread and crash the application.
    /// </summary>
    public interface IHandleObservableErrors
    {
        /// <summary>
        ///     Fires whenever an exception would normally terminate ReactiveUI
        ///     internal state.
        /// </summary>
        IObservable<Exception> ThrownExceptions { get; }
    }

    public interface IReactiveCommand : IHandleObservableErrors, ICommand, IDisposable
    {
        /// <summary>
        ///     Gets a value indicating whether this instance can execute observable.
        /// </summary>
        /// <value><c>true</c> if this instance can execute observable; otherwise, <c>false</c>.</value>
        IObservable<bool> CanExecuteObservable { get; }

        /// <summary>
        ///     Gets a value indicating whether this instance is executing. This
        ///     Observable is guaranteed to always return a value immediately (i.e.
        ///     it is backed by a BehaviorSubject), meaning it is safe to determine
        ///     the current state of the command via IsExecuting.First()
        /// </summary>
        /// <value><c>true</c> if this instance is executing; otherwise, <c>false</c>.</value>
        IObservable<bool> IsExecuting { get; }
    }

    /// <summary>
    ///     IReactiveCommand represents an ICommand which also notifies when it is
    ///     executed (i.e. when Execute is called) via IObservable. Conceptually,
    ///     this represents an Event, so as a result this IObservable should never
    ///     OnComplete or OnError.
    ///     In previous versions of ReactiveUI, this interface was split into two
    ///     separate interfaces, one to handle async methods and one for "standard"
    ///     commands, but these have now been merged - every ReactiveCommand is now
    ///     a ReactiveAsyncCommand.
    /// </summary>
    public interface IReactiveCommand<T> : IObservable<T>, IReactiveCommand
    {
        IObservable<T> ExecuteAsync(object parameter = null);
    }
}