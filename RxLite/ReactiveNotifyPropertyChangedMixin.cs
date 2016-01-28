using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace RxLite
{
    public static class ReactiveNotifyPropertyChangedMixin
    {
        private static readonly IEnumerable<ICreatesObservableForProperty> ObservablesForProperty =
            new List<ICreatesObservableForProperty> {
                new INPCObservableForProperty(),
                new IROObservableForProperty(),
                new POCOObservableForProperty()
            };

        static ReactiveNotifyPropertyChangedMixin()
        {
            RxApp.EnsureInitialized();
        }

        /// <summary>
        ///     ObservableForProperty returns an Observable representing the
        ///     property change notifications for a specific property on a
        ///     ReactiveObject. This method (unlike other Observables that return
        ///     IObservedChange) guarantees that the Value property of
        ///     the IObservedChange is set.
        /// </summary>
        /// <param name="property">
        ///     An Expression representing the property (i.e.
        ///     'x => x.SomeProperty.SomeOtherProperty'
        /// </param>
        /// <param name="beforeChange">
        ///     If True, the Observable will notify
        ///     immediately before a property is going to change.
        /// </param>
        /// <returns>
        ///     An Observable representing the property change
        ///     notifications for the given property.
        /// </returns>
        public static IObservable<IObservedChange<TSender, TValue>> ObservableForProperty<TSender, TValue>(
            this TSender This, Expression<Func<TSender, TValue>> property, bool beforeChange = false,
            bool skipInitial = true)
        {
            if (This == null)
            {
                throw new ArgumentNullException("Sender");
            }

            /* x => x.Foo.Bar.Baz;
             * 
             * Subscribe to This, look for Foo
             * Subscribe to Foo, look for Bar
             * Subscribe to Bar, look for Baz
             * Subscribe to Baz, publish to Subject
             * Return Subject
             * 
             * If Bar changes (notification fires on Foo), resubscribe to new Bar
             *  Resubscribe to new Baz, publish to Subject
             * 
             * If Baz changes (notification fires on Bar),
             *  Resubscribe to new Baz, publish to Subject
             */

            return SubscribeToExpressionChain<TSender, TValue>(This, property.Body, beforeChange, skipInitial);
        }

        /// <summary>
        ///     ObservableForProperty returns an Observable representing the
        ///     property change notifications for a specific property on a
        ///     ReactiveObject, running the IObservedChange through a Selector
        ///     function.
        /// </summary>
        /// <param name="property">
        ///     An Expression representing the property (i.e.
        ///     'x => x.SomeProperty'
        /// </param>
        /// <param name="selector">
        ///     A Select function that will be run on each
        ///     item.
        /// </param>
        /// <param name="beforeChange">
        ///     If True, the Observable will notify
        ///     immediately before a property is going to change.
        /// </param>
        /// <returns>
        ///     An Observable representing the property change
        ///     notifications for the given property.
        /// </returns>
        public static IObservable<TRet> ObservableForProperty<TSender, TValue, TRet>(
            this TSender This, Expression<Func<TSender, TValue>> property, Func<TValue, TRet> selector,
            bool beforeChange = false) where TSender : class
        {
            Contract.Requires(selector != null);
            return This.ObservableForProperty(property, beforeChange).Select(x => selector(x.Value));
        }

        public static IObservable<IObservedChange<TSender, TValue>> SubscribeToExpressionChain<TSender, TValue>(
            this TSender source, Expression expression, bool beforeChange = false, bool skipInitial = true)
        {
            IObservable<IObservedChange<object, object>> notifier =
                Observable.Return(new ObservedChange<object, object>(null, null, source));

            var chain = Reflection.Rewrite(expression).GetExpressionChain();
            notifier = chain.Aggregate(
                notifier, (n, expr) => n.Select(y => NestedObservedChanges(expr, y, beforeChange)).Switch());

            if (skipInitial)
            {
                notifier = notifier.Skip(1);
            }

            notifier = notifier.Where(x => x.Sender != null);

            var r = notifier.Select(
                x =>
                    {
                        // ensure cast to TValue will succeed, throw useful exception otherwise
                        var val = x.GetValue();
                        if (val != null && !(val is TValue))
                        {
                            throw new InvalidCastException($"Unable to cast from {val.GetType()} to {typeof(TValue)}.");
                        }

                        return new ObservedChange<TSender, TValue>(source, expression, (TValue)val);
                    });

            return r.DistinctUntilChanged(x => x.Value);
        }

        private static IObservedChange<object, object> ObservedChangeFor(
            Expression expression, IObservedChange<object, object> sourceChange)
        {
            if (sourceChange.Value == null)
            {
                return new ObservedChange<object, object>(sourceChange.Value, expression);
            }
            object value;
            // expression is always a simple expression
            Reflection.TryGetValueForPropertyChain(out value, sourceChange.Value, new[] { expression });
            return new ObservedChange<object, object>(sourceChange.Value, expression, value);
        }

        private static IObservable<IObservedChange<object, object>> NestedObservedChanges(
            Expression expression, IObservedChange<object, object> sourceChange, bool beforeChange)
        {
            // Make sure a change at a root node propagates events down
            var kicker = ObservedChangeFor(expression, sourceChange);

            // Handle null values in the chain
            if (sourceChange.Value == null)
            {
                return Observable.Return(kicker);
            }

            // Handle non null values in the chain
            return
                NotifyForProperty(sourceChange.Value, expression, beforeChange)
                    .Select(x => new ObservedChange<object, object>(x.Sender, expression, x.GetValue()))
                    .StartWith(kicker);
        }

        private static ICreatesObservableForProperty NotifyFactoryCache(
            Type type, string propertyName, bool beforeChanged = false)
        {
            return ObservablesForProperty.Aggregate(
                Tuple.Create(0, (ICreatesObservableForProperty)null), (acc, x) =>
                    {
                        var score = x.GetAffinityForObject(type, propertyName, beforeChanged);
                        return (score > acc.Item1) ? Tuple.Create(score, x) : acc;
                    }).Item2;
        }

        private static IObservable<IObservedChange<object, object>> NotifyForProperty(
            object sender, Expression expression, bool beforeChange)
        {
            var result = NotifyFactoryCache(sender.GetType(), expression.GetMemberInfo().Name, beforeChange);
            return result.GetNotificationForProperty(sender, expression, beforeChange);
        }
    }
}