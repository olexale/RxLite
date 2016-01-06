using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace RxLite
{
    /// <summary>
    ///     This class is the final fallback for WhenAny, and will simply immediately
    ///     return the value of the type at the time it was created. It will also
    ///     warn the user that this is probably not what they want to do
    /// </summary>
    public class POCOObservableForProperty : ICreatesObservableForProperty
    {
        private static readonly Dictionary<Type, bool> HasWarned = new Dictionary<Type, bool>();

        public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged = false)
        {
            return 1;
        }

        public IObservable<IObservedChange<object, object>> GetNotificationForProperty(object sender,
            Expression expression, bool beforeChanged = false)
        {
            var type = sender.GetType();
            if (!HasWarned.ContainsKey(type))
            {
                HasWarned[type] = true;
            }

            return Observable.Return(new ObservedChange<object, object>(sender, expression), RxApp.MainThreadScheduler)
                .Concat(Observable.Never<IObservedChange<object, object>>());
        }
    }
}