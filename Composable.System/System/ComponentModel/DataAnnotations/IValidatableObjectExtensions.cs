using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Linq;
using System.Linq.Expressions;
using Composable.Contracts;

namespace Composable.System.ComponentModel.DataAnnotations
{
    ///<summary>Extensions for <see cref="IValidatableObject"/> intended to make type safe implementations easy.</summary>
    public static class IValidatableObjectExtensions
    {
        static string ExtractMemberName(Expression<Func<object>> accessor)
        {
            Contract.Argument(() => accessor).NotNull();

            Expression expr = accessor.Body;
            while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked)
                expr = ((UnaryExpression)expr).Operand;

            if (!(expr is MemberExpression))
                throw new ArgumentException("Arguments must be of the form '() => SomeMember'.");
            return ((MemberExpression)expr).Member.Name;
        }

        ///<summary>Creates an <see cref="ValidationResult"/> by extracting the invalid member(s) name from the supplied expression(s)</summary>///<summary>Enumerates the lines in a streamreader.</summary>
        static ValidationResult CreateValidationResult(this IValidatableObject me, string message, IEnumerable<Expression<Func<object>>> members)
        {
            Contract.Argument(() => me, () => message, () => members).NotNull();
            return new ValidationResult(message, members.Select(ExtractMemberName).ToList());
        }

        ///<summary>Creates an <see cref="ValidationResult"/> by extracting the invalid member(s) name from the supplied expression(s)</summary>///<summary>Enumerates the lines in a streamreader.</summary>
        public static ValidationResult CreateValidationResult(this IValidatableObject me, string message, params Expression<Func<object>>[] members)
        {
            Contract.Argument(() => me, () => message, () => members).NotNull();
            return me.CreateValidationResult(message, (IEnumerable<Expression<Func<object>>>)members);
        }
    }
}
