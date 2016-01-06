using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RxLite
{
    internal class ExpressionRewriter : ExpressionVisitor
    {
        public override Expression Visit(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    return VisitBinary((BinaryExpression) node);
                case ExpressionType.ArrayLength:
                    return VisitUnary((UnaryExpression) node);
                case ExpressionType.Call:
                    return VisitMethodCall((MethodCallExpression) node);
                case ExpressionType.Index:
                    return VisitIndex((IndexExpression) node);
                case ExpressionType.MemberAccess:
                    return VisitMember((MemberExpression) node);
                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression) node);
                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression) node);
                case ExpressionType.Convert:
                    return VisitUnary((UnaryExpression) node);
                default:
                    throw new NotSupportedException($"Unsupported expression type: '{node.NodeType}'");
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (!(node.Right is ConstantExpression))
            {
                throw new NotSupportedException("Array index expressions are only supported with constants.");
            }

            var left = Visit(node.Left);
            var right = Visit(node.Right);

            // Translate arrayindex into normal index expression
            return Expression.MakeIndex(left, left.Type.GetRuntimeProperty("Item"), new[] {right});
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.ArrayLength:
                    var expression = Visit(node.Operand);
                    //translate arraylength into normal member expression
                    return Expression.MakeMemberAccess(expression, expression.Type.GetRuntimeProperty("Length"));
                case ExpressionType.Convert:
                    return Visit(node.Operand);
                default:
                    return node.Update(Visit(node.Operand));
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Rewrite a method call to an indexer as an index expression
            if (node.Arguments.Any(e => !(e is ConstantExpression)) || !node.Method.IsSpecialName)
            {
                throw new NotSupportedException("Index expressions are only supported with constants.");
            }

            var instance = Visit(node.Object);
            IEnumerable<Expression> arguments = Visit(node.Arguments);

            // Translate call to get_Item into normal index expression
            return Expression.MakeIndex(instance, instance.Type.GetRuntimeProperty("Item"), arguments);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            if (node.Arguments.Any(e => !(e is ConstantExpression)))
            {
                throw new NotSupportedException("Index expressions are only supported with constants.");
            }
            return base.VisitIndex(node);
        }
    }
}