using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace Composable.System.ComponentModel.Properties
{
    public static class ExpressionUtil
    {
        public static string ExtractMemberName(LambdaExpression lambda)
        {
            Contract.Requires(lambda!=null);
            var body = lambda.Body;
            MemberExpression memberExpression;

            if (body is UnaryExpression)
            {
                memberExpression = (MemberExpression) ((UnaryExpression) body).Operand;
            }
            else
            {
                memberExpression = (MemberExpression) body;
            }

            return memberExpression.Member.Name;
        }
    }
}