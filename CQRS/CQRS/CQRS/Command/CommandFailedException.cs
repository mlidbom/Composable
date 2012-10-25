using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Composable.System;

namespace Composable.CQRS.Command
{
    public class CommandFailedException : Exception
    {
        public IEnumerable<string> InvalidMembers { get; private set; }

        public CommandFailedException(string message) : this(message, new string[0])
        {
        }

        public CommandFailedException(string message, IEnumerable<string> invalidMembers) : base(message)
        {
            this.InvalidMembers = invalidMembers.ToList();
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

        private static string ExtractMemberName(LambdaExpression expr)
        {
            var body = expr.Body;
            var parts = new List<string>();
            for (;;)
            {
                switch (body.NodeType)
                {
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        body = ((UnaryExpression)body).Operand;
                        break;
                    case ExpressionType.MemberAccess:
                        var member = ((MemberExpression)body).Member;
                        if (!member.DeclaringType.IsDefined(typeof(CompilerGeneratedAttribute), true))  // Don't add access to compiler-generated classes to our path because an expression such as () => command.Member.SubMember will contain an access to the "command" member of an anonymous type.
                            parts.Add(member.Name);
                        body = ((MemberExpression)body).Expression;
                        break;
                    case ExpressionType.Constant:
                    case ExpressionType.Parameter:
                        goto breakOuter;
                    default:
                        throw new ArgumentException("Expression must be a member expression, eg () => command.InvalidMember", "expr");
                }
            }
breakOuter:
            return ((IEnumerable<string>)parts).Reverse().Join(".");
        }
    }
}