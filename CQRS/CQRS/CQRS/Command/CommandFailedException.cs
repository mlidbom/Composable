using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Composable.CQRS.Command
{
    public class CommandFailedException : Exception
    {
        private List<string> _invalidMembers;

        public IEnumerable<string> InvalidMembers { get { return _invalidMembers.Select(x => x); } }

        public CommandFailedException(string message) : this(message, new string[0])
        {
        }

        public CommandFailedException(string message, IEnumerable<string> invalidMembers) : base(message)
        {
            this._invalidMembers = invalidMembers.ToList();
        }

        public CommandFailedException(string message, params string[] invalidMembers) : this(message, (IEnumerable<string>)invalidMembers)
        {
        }

        public CommandFailedException(string message, IEnumerable<Expression<Func<object>>> memberAccessors) : this(message, memberAccessors.Select(ExtractMemberName))
        {
        }

        public CommandFailedException(string message, params Expression<Func<object>>[] memberAccessors) : this(message, (IEnumerable<Expression<Func<object>>>)memberAccessors)
        {
        }

        [Obsolete("Fix nested access")]
        private static string ExtractMemberName(LambdaExpression expr)
        {
            var body = expr.Body;
            // Because our constructor takes a Func<object>, we might have a boxing conversion after the member access.
            while (body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.ConvertChecked)
                body = ((UnaryExpression)body).Operand;
            if (!(body is MemberExpression))
                throw new ArgumentException("Expression must be a member expression", "expr");
            return ((MemberExpression)body).Member.Name;
        }
    }
}