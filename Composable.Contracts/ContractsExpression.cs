using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Composable.Contracts
{
    public static class ContractsExpression
    {
        public static string ExtractName<TValue>(Expression<Func<TValue>> fetchValue)
        {
            return ExtractName((LambdaExpression)fetchValue);
        }

        private static string ExtractName(LambdaExpression lambda)
        {
            var body = lambda.Body;

            var unaryExpression = body as UnaryExpression;
            if(unaryExpression != null)
            {
                var innerMemberExpression = unaryExpression.Operand as MemberExpression;
                if(innerMemberExpression != null)
                {
                    return innerMemberExpression.Member.Name;
                }
            }

            var memberExpression = body as MemberExpression;
            if(memberExpression != null)
            {
                return memberExpression.Member.Name;
            }

            throw new InvalidAccessorLambdaException();
        }

        public static TValue ExtractValue<TValue>(Expression<Func<TValue>> fetchValue)
        {
            return (TValue)GetExpressionValue(fetchValue.Body);
        }

        private static object GetExpressionValue(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)expression).Value;
                case ExpressionType.MemberAccess:
                {
                    var me = (MemberExpression)expression;
                    var obj = (me.Expression != null ? GetExpressionValue(me.Expression) : null);
                    var fieldInfo = me.Member as FieldInfo;
                    if (fieldInfo != null)
                        return fieldInfo.GetValue(obj);
                    var propertyInfo = me.Member as PropertyInfo;
                    if (propertyInfo != null)
                        return propertyInfo.GetValue(obj, null);
                    throw new InvalidAccessorLambdaException();
                }
                case ExpressionType.Convert:
                {
                    var ue = (UnaryExpression)expression;
                    var operand = GetExpressionValue(ue.Operand);
                    if(ue.Type.IsInstanceOfType(operand))
                    {
                        return operand;
                    }
                    throw new InvalidAccessorLambdaException();
                }
                default:
                throw new InvalidAccessorLambdaException();
            }
        }
    }
}
