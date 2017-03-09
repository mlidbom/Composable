#region usings

using System;

using System.Linq.Expressions;
using Composable.Contracts;

#endregion

namespace Composable.System.Linq
{
    ///<summary>Extracts member names from expressions</summary>
    public static class ExpressionUtil
    {
        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberName((LambdaExpression)func);
        }


        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TValue>(Expression<Func<TParam, TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TParam2, TValue>(Expression<Func<TParam, TParam2, TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied lambda expression returns.</summary>
        public static string ExtractMemberName(LambdaExpression lambda)
        {
            Contract.Argument(() => lambda).NotNull();
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
            Contract.Argument(() => func).NotNull();
            return ExtractMemberPath((LambdaExpression)func);
        }

        public static string ExtractMemberPath(LambdaExpression lambda)
        {
            Contract.Argument(() => lambda).NotNull();
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