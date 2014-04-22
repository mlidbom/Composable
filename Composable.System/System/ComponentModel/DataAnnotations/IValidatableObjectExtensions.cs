using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace Composable.System.ComponentModel.DataAnnotations
{
    public static class IValidatableObjectExtensions
    {
        private static string ExtractMemberName(Expression<Func<object>> accessor)
        {
            Expression expr = accessor.Body;
            while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked)
                expr = ((UnaryExpression)expr).Operand;

            if (!(expr is MemberExpression))
                throw new ArgumentException("Arguments must be of the form '() => SomeMember'.");
            return ((MemberExpression)expr).Member.Name;
        }

        public static ValidationResult CreateValidationResult(this IValidatableObject me, string message, IEnumerable<Expression<Func<object>>> members)
        {
            Contract.Requires(me != null && message != null && members != null);
            return new ValidationResult(message, members.Select(ExtractMemberName).ToList());
        }

        public static ValidationResult CreateValidationResult(this IValidatableObject me, string message, params Expression<Func<object>>[] members)
        {
            Contract.Requires(me != null && message != null && members != null);
            return me.CreateValidationResult(message, (IEnumerable<Expression<Func<object>>>)members);
        }
    }
}
