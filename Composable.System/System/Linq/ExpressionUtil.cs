#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

#endregion

namespace Composable.System.Linq
{
    ///<summary>Extracts member names from expressions</summary>
    public static class ExpressionUtil
    {
        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
        {
            Contract.Requires(func != null);
            return ExtractMemberName((LambdaExpression)func);
        }


        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TValue>(Expression<Func<TParam, TValue>> func)
        {
            Contract.Requires(func != null);
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TParam2, TValue>(Expression<Func<TParam, TParam2, TValue>> func)
        {
            Contract.Requires(func != null);
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied lambda expression returns.</summary>
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


        public static string ExtractMemberPath<TValue>(Expression<Func<TValue>> func)
        {
            Contract.Requires(func != null);
            return ExtractMemberPath((LambdaExpression)func);
        }

        public static string ExtractMemberPath(LambdaExpression lambda)
        {
            Contract.Requires(lambda != null);
            var body = lambda.Body;
            MemberExpression memberExpression;

            if (body is UnaryExpression)
            {
                memberExpression = (MemberExpression)((UnaryExpression)body).Operand;
            }
            else
            {
                memberExpression = (MemberExpression)body;
            }

            return $"{memberExpression.Member.DeclaringType.FullName}.{memberExpression.Member.Name}" ;
        }
    }
}