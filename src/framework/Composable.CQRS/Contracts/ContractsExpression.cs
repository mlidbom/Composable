using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Composable.Contracts
{
    ///<summary>Extracts values and names from the parts of a lambda expression</summary>
    static class ContractsExpression
    {
        ///<summary>Extracts the returned field,property,argument name from a lambda</summary>
        public static string ExtractName<TValue>(Expression<Func<TValue>> fetchValue) => ExtractName((LambdaExpression)fetchValue);

        static string ExtractName(LambdaExpression lambda)
        {
            var body = lambda.Body;

            if((body as UnaryExpression)?.Operand is MemberExpression innerMemberExpression)
            {
                return innerMemberExpression.Member.Name;
            }

            if(body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new InvalidAccessorLambdaException();
        }

        ///<summary>Extracts the returned field,property,argument value from a lambda</summary>
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable 8605
        public static TValue? ExtractValue<TValue>(Expression<Func<TValue>> fetchValue) => (TValue?)GetExpressionValue(fetchValue.Body);
#pragma warning restore 8605
#pragma warning restore IDE0079 // Remove unnecessary suppression

        static object? GetExpressionValue(Expression expression)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
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
