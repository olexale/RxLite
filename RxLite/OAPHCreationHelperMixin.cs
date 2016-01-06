using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reactive.Concurrency;

namespace RxLite
{
    public static class OAPHCreationHelperMixin
    {
        private static ObservableAsPropertyHelper<TRet> ObservableToProperty<TObj, TRet>(
            this TObj This,
            IObservable<TRet> observable,
            Expression<Func<TObj, TRet>> property,
            TRet initialValue = default(TRet),
            IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            Contract.Requires(This != null);
            Contract.Requires(observable != null);
            Contract.Requires(property != null);

            var expression = Reflection.Rewrite(property.Body);

            if (expression.GetParent().NodeType != ExpressionType.Parameter)
            {
                throw new ArgumentException("Property expression must be of the form 'x => x.SomeProperty'");
            }

            var name = expression.GetMemberInfo().Name;
            var ret = new ObservableAsPropertyHelper<TRet>(observable,
                _ => This.raisePropertyChanged(name),
                _ => This.raisePropertyChanging(name),
                initialValue, scheduler);

            return ret;
        }

        /// <summary>
        ///     Converts an Observable to an ObservableAsPropertyHelper and
        ///     automatically provides the onChanged method to raise the property
        ///     changed notification.
        /// </summary>
        /// <param name="source">The ReactiveObject that has the property</param>
        /// <param name="property">
        ///     An Expression representing the property (i.e.
        ///     'x => x.SomeProperty'
        /// </param>
        /// <param name="initialValue">The initial value of the property.</param>
        /// <param name="scheduler">
        ///     The scheduler that the notifications will be
        ///     provided on - this should normally be a Dispatcher-based scheduler
        ///     (and is by default)
        /// </param>
        /// <returns>
        ///     An initialized ObservableAsPropertyHelper; use this as the
        ///     backing field for your property.
        /// </returns>
        public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
            this IObservable<TRet> This,
            TObj source,
            Expression<Func<TObj, TRet>> property,
            TRet initialValue = default(TRet),
            IScheduler scheduler = null)
            where TObj : IReactiveObject
        {
            return source.ObservableToProperty(This, property, initialValue, scheduler);
        }

        /// <summary>
        ///     Converts an Observable to an ObservableAsPropertyHelper and
        ///     automatically provides the onChanged method to raise the property
        ///     changed notification.
        /// </summary>
        /// <param name="source">The ReactiveObject that has the property</param>
        /// <param name="property">
        ///     An Expression representing the property (i.e.
        ///     'x => x.SomeProperty'
        /// </param>
        /// <param name="initialValue">The initial value of the property.</param>
        /// <param name="scheduler">
        ///     The scheduler that the notifications will be
        ///     provided on - this should normally be a Dispatcher-based scheduler
        ///     (and is by default)
        /// </param>
        /// <returns>
        ///     An initialized ObservableAsPropertyHelper; use this as the
        ///     backing field for your property.
        /// </returns>
        public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
            this IObservable<TRet> This,
            TObj source,
            Expression<Func<TObj, TRet>> property,
            out ObservableAsPropertyHelper<TRet> result,
            TRet initialValue = default(TRet),
            IScheduler scheduler = null)
            where TObj : ReactiveObject
        {
            var ret = source.ObservableToProperty(This, property, initialValue, scheduler);

            result = ret;
            return ret;
        }
    }
}