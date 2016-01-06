using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace RxLite
{
    public static class Reflection
    {
        private static readonly ExpressionRewriter ExpressionRewriter = new ExpressionRewriter();

        public static Expression Rewrite(Expression expression)
        {
            return ExpressionRewriter.Visit(expression);
        }

        public static string ExpressionToPropertyNames(Expression expression)
        {
            Contract.Requires(expression != null);

            var sb = new StringBuilder();

            foreach (var exp in expression.GetExpressionChain())
            {
                if (exp.NodeType != ExpressionType.Parameter)
                {
                    // Indexer expression
                    if (exp.NodeType == ExpressionType.Index)
                    {
                        var ie = (IndexExpression) exp;
                        sb.Append(ie.Indexer.Name);
                        sb.Append('[');

                        foreach (var argument in ie.Arguments)
                        {
                            sb.Append(((ConstantExpression) argument).Value);
                            sb.Append(',');
                        }
                        sb.Replace(',', ']', sb.Length - 1, 1);
                    }
                    else if (exp.NodeType == ExpressionType.MemberAccess)
                    {
                        var me = (MemberExpression) exp;
                        sb.Append(me.Member.Name);
                    }
                }

                sb.Append('.');
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        public static Func<object, object[], object> GetValueFetcherForProperty(MemberInfo member)
        {
            Contract.Requires(member != null);

            var field = member as FieldInfo;
            if (field != null)
            {
                return (obj, args) => field.GetValue(obj);
            }

            var property = member as PropertyInfo;
            if (property != null)
            {
                return property.GetValue;
            }

            return null;
        }

        public static Func<object, object[], object> GetValueFetcherOrThrow(MemberInfo member)
        {
            var ret = GetValueFetcherForProperty(member);

            if (ret == null)
            {
                throw new ArgumentException($"Type '{member.DeclaringType}' must have a property '{member.Name}'");
            }
            return ret;
        }

        public static Action<object, object, object[]> GetValueSetterForProperty(MemberInfo member)
        {
            Contract.Requires(member != null);

            var field = member as FieldInfo;
            if (field != null)
            {
                return (obj, val, args) => field.SetValue(obj, val);
            }

            var property = member as PropertyInfo;
            if (property != null)
            {
                return property.SetValue;
            }

            return null;
        }

        public static Action<object, object, object[]> GetValueSetterOrThrow(MemberInfo member)
        {
            var ret = GetValueSetterForProperty(member);

            if (ret == null)
            {
                throw new ArgumentException($"Type '{member.DeclaringType}' must have a property '{member.Name}'");
            }
            return ret;
        }

        public static bool TryGetValueForPropertyChain<TValue>(out TValue changeValue, object current,
            IEnumerable<Expression> expressionChain)
        {
            var expressions = expressionChain as IList<Expression> ?? expressionChain.ToList();
            foreach (var expression in expressions.SkipLast(1))
            {
                if (current == null)
                {
                    changeValue = default(TValue);
                    return false;
                }

                current = GetValueFetcherOrThrow(expression.GetMemberInfo())(current, expression.GetArgumentsArray());
            }

            if (current == null)
            {
                changeValue = default(TValue);
                return false;
            }

            var lastExpression = expressions.Last();
            changeValue =
                (TValue)
                    GetValueFetcherOrThrow(lastExpression.GetMemberInfo())(current, lastExpression.GetArgumentsArray());
            return true;
        }

        public static bool TryGetAllValuesForPropertyChain(out IObservedChange<object, object>[] changeValues,
            object current, IEnumerable<Expression> expressionChain)
        {
            var currentIndex = 0;
            var expressions = expressionChain as IList<Expression> ?? expressionChain.ToList();
            changeValues = new IObservedChange<object, object>[expressions.Count()];

            foreach (var expression in expressions.SkipLast(1))
            {
                if (current == null)
                {
                    changeValues[currentIndex] = null;
                    return false;
                }

                var sender = current;
                current = GetValueFetcherOrThrow(expression.GetMemberInfo())(current, expression.GetArgumentsArray());
                var box = new ObservedChange<object, object>(sender, expression, current);

                changeValues[currentIndex] = box;
                currentIndex++;
            }

            if (current == null)
            {
                changeValues[currentIndex] = null;
                return false;
            }

            var lastExpression = expressions.Last();
            changeValues[currentIndex] = new ObservedChange<object, object>(current, lastExpression,
                GetValueFetcherOrThrow(lastExpression.GetMemberInfo())(current, lastExpression.GetArgumentsArray()));

            return true;
        }

        public static bool TrySetValueToPropertyChain<TValue>(object target, IEnumerable<Expression> expressionChain,
            TValue value, bool shouldThrow = true)
        {
            var expressions = expressionChain as IList<Expression> ?? expressionChain.ToList();
            foreach (var expression in expressions.SkipLast(1))
            {
                var getter = shouldThrow
                    ? GetValueFetcherOrThrow(expression.GetMemberInfo())
                    : GetValueFetcherForProperty(expression.GetMemberInfo());

                target = getter(target, expression.GetArgumentsArray());
            }

            if (target == null)
                return false;

            var lastExpression = expressions.Last();
            var setter = shouldThrow
                ? GetValueSetterOrThrow(lastExpression.GetMemberInfo())
                : GetValueSetterForProperty(lastExpression.GetMemberInfo());

            if (setter == null)
                return false;
            setter(target, value, lastExpression.GetArgumentsArray());
            return true;
        }

        public static Type GetEventArgsTypeForEvent(Type type, string eventName)
        {
            var ti = type;
            var ei = ti.GetRuntimeEvent(eventName);
            if (ei == null)
            {
                throw new Exception($"Couldn't find {type.FullName}.{eventName}");
            }

            // Find the EventArgs type parameter of the event via digging around via reflection
            var eventArgsType =
                ei.EventHandlerType.GetRuntimeMethods().First(x => x.Name == "Invoke").GetParameters()[1].ParameterType;
            return eventArgsType;
        }

        public static void ThrowIfMethodsNotOverloaded(string callingTypeName, object targetObject,
            params string[] methodsToCheck)
        {
            var missingMethod = methodsToCheck
                .Select(x =>
                {
                    var methods = targetObject.GetType().GetTypeInfo().DeclaredMethods;
                    return Tuple.Create(x, methods.FirstOrDefault(y => y.Name == x));
                })
                .FirstOrDefault(x => x.Item2 == null);

            if (missingMethod != null)
            {
                throw new Exception(string.Format("Your class must implement {0} and call {1}.{0}", missingMethod.Item1,
                    callingTypeName));
            }
        }

        internal static Expression GetViewExpression(object view, Expression vmExpression)
        {
            var controlProperty = (MemberInfo) view.GetType().GetRuntimeField(vmExpression.GetMemberInfo().Name)
                                  ?? view.GetType().GetRuntimeProperty(vmExpression.GetMemberInfo().Name);
            if (controlProperty == null)
            {
                throw new Exception(
                    $"Tried to bind to control but it wasn't present on the object: {view.GetType().FullName}.{vmExpression.GetMemberInfo().Name}");
            }

            return Expression.MakeMemberAccess(Expression.Parameter(view.GetType()), controlProperty);
        }
    }

    public static class ReflectionExtensions
    {
        public static bool IsStatic(this PropertyInfo This)
        {
            return (This.GetMethod ?? This.SetMethod).IsStatic;
        }
    }
}