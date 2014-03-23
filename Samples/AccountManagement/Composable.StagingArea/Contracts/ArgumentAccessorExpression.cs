using System;
using System.Linq.Expressions;

namespace Composable.Contracts
{
    internal static class ArgumentAccessorExpression
    {
        public static string ExtractArgumentName<TValue>(Expression<Func<TValue>> func)
        {
            return ExtractArgumentName((LambdaExpression)func);
        }

        private static string ExtractArgumentName(LambdaExpression lambda)
        {
            var body = lambda.Body;

            var unaryExpression = body as UnaryExpression;
            if(unaryExpression != null)
            {
                var innerMemberExpression = unaryExpression.Operand as MemberExpression;
                if(innerMemberExpression != null)
                {
                    Console.WriteLine(innerMemberExpression.Member.MemberType);
                    return innerMemberExpression.Member.Name;
                }
            }

            var memberExpression = body as MemberExpression;
            if(memberExpression != null)
            {
                return memberExpression.Member.Name;
            }

            throw new InvalidArgumentAccessorLambda();
        }
    }
}
