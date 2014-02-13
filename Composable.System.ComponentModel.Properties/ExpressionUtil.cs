#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

#endregion

namespace Composable.System.ComponentModel.Properties
{
    [Obsolete("Please use the class in Composable.System.Linq.Expressions.ExpressionUtil instead.")]
    public static class ExpressionUtil
    {
        public static string ExtractMemberName(LambdaExpression lambda)
        {
            Contract.Requires(lambda != null);
            var body = lambda.Body;
            MemberExpression memberExpression;

            if(body is UnaryExpression)
            {
                memberExpression = (MemberExpression)((UnaryExpression)body).Operand;
            }
            else
            {
                memberExpression = (MemberExpression)body;
            }

            return memberExpression.Member.Name;
        }
    }
}