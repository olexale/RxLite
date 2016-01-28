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
                        var ie = (IndexExpression)exp;
                        sb.Append(ie.Indexer.Name);
                        sb.Append('[');

                        foreach (var argument in ie.Arguments)
                        {
                            sb.Append(((ConstantExpression)argument).Value);
                            sb.Append(',');
                        }
                        sb.Replace(',', ']', sb.Length - 1, 1);
                    }
                    else if (exp.NodeType == ExpressionType.MemberAccess)
                    {
                        var me = (MemberExpression)exp;
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

        public static bool TryGetValueForPropertyChain<TValue>(
            out TValue changeValue, object current, IEnumerable<Expression> expressionChain)
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

        public static bool TrySetValueToPropertyChain<TValue>(
            object target, IEnumerable<Expression> expressionChain, TValue value, bool shouldThrow = true)
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
            {
                return false;
            }

            var lastExpression = expressions.Last();
            var setter = shouldThrow
                             ? GetValueSetterOrThrow(lastExpression.GetMemberInfo())
                             : GetValueSetterForProperty(lastExpression.GetMemberInfo());

            if (setter == null)
            {
                return false;
            }
            setter(target, value, lastExpression.GetArgumentsArray());
            return true;
        }
    }
}