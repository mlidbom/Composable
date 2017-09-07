using System;
using System.Linq.Expressions;
using Composable.Testing.Contracts;

namespace Composable.Testing.System.Linq
{
    ///<summary>Extracts member names from expressions</summary>
    static class ExpressionUtil
    {
        public static string ExtractMemberPath<TValue>(Expression<Func<TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberPath((LambdaExpression)func);
        }

        static string ExtractMemberPath(LambdaExpression lambda)
        {
            Contract.Argument(() => lambda).NotNull();
            var body = lambda.Body;
            MemberExpression memberExpression;

            var expression = body as UnaryExpression;
            if (expression != null)
            {
                memberExpression = (MemberExpression)expression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)body;
            }

            // ReSharper disable once PossibleNullReferenceException
            return $"{memberExpression.Member.DeclaringType.FullName}.{memberExpression.Member.Name}" ;
        }
    }
}