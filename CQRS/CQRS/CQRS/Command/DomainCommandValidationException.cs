using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Composable.System;

namespace Composable.CQRS.Command
{
    /// <summary>
    /// This exception should be thrown when the domain is unable to do what it has been asked to 
    /// and should contain information as to which member(s) of the command caused the issue if such can be determined.
    /// </summary>
    public class DomainCommandValidationException : Exception
    {
        public DomainCommandValidationException(string message) : this(message, new string[0]) {}

        public DomainCommandValidationException(string message, IEnumerable<string> invalidMembers) : base(message)
        {
            InvalidMembers = invalidMembers.ToList();
        }

        public DomainCommandValidationException(string message, params string[] invalidMembers) : this(message, (IEnumerable<string>)invalidMembers) {}

        public DomainCommandValidationException(string message, IEnumerable<Expression<Func<object>>> memberAccessors)
            : this(message, (IEnumerable<string>)memberAccessors.Select(ExtractMemberName)) {}

        public DomainCommandValidationException(string message, params Expression<Func<object>>[] memberAccessors) : this(message, (IEnumerable<Expression<Func<object>>>)memberAccessors) {}

        public IEnumerable<string> InvalidMembers { get; private set; }

        private static string ExtractMemberName(LambdaExpression expr)
        {
            var body = expr.Body;
            var parts = new List<string>();
            for(;;)
            {
                switch(body.NodeType)
                {
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        body = ((UnaryExpression)body).Operand;
                        break;
                    case ExpressionType.MemberAccess:
                        var member = ((MemberExpression)body).Member;
                        if(!member.DeclaringType.IsDefined(typeof(CompilerGeneratedAttribute), true))
                            // Don't add access to compiler-generated classes to our path because an expression such as () => command.Member.SubMember will contain an access to the "command" member of an anonymous type.
                        {
                            parts.Add(member.Name);
                        }
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