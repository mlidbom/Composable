using System;
using System.Linq.Expressions;
using Composable.Contracts;

namespace Composable.SystemCE.LinqCE
{
    ///<summary>Extracts member names from expressions</summary>
    static class ExpressionUtil
    {
        public static string ExtractMethodName(Expression<Action> func)
        {
            Contract.ArgumentNotNull(func, nameof(func));
            return ((MethodCallExpression)func.Body).Method.Name;
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMethodName<T>(Expression<Func<T>> func)
        {
            Contract.ArgumentNotNull(func, nameof(func));
            return ((MethodCallExpression)func.Body).Method.Name;
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
        {
            Contract.ArgumentNotNull(func, nameof(func));
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TValue>(Expression<Func<TParam, TValue>> func)
        {
            Contract.ArgumentNotNull(func, nameof(func));
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TParam2, TValue>(Expression<Func<TParam, TParam2, TValue>> func)
        {
            Contract.ArgumentNotNull(func, nameof(func));
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied lambda expression returns.</summary>
        static string ExtractMemberName(LambdaExpression lambda)
        {
            Contract.ArgumentNotNull(lambda, nameof(lambda));

            var memberExpression = lambda.Body is UnaryExpression unaryExpression
                                       ? (MemberExpression)unaryExpression.Operand
                                       : (MemberExpression)lambda.Body;

            return memberExpression.Member.Name;
        }

        public static string ExtractMemberPath<TValue>(Expression<Func<TValue>> func)
        {
            Contract.ArgumentNotNull(func, nameof(func));
            return ExtractMemberPath((LambdaExpression)func);
        }

        static string ExtractMemberPath(LambdaExpression lambda)
        {
            Contract.ArgumentNotNull(lambda, nameof(lambda));
            var memberExpression = lambda.Body is UnaryExpression unaryExpression
                                       ? (MemberExpression)unaryExpression.Operand
                                       : (MemberExpression)lambda.Body;

            return $"{memberExpression.Member.DeclaringType!.FullName}.{memberExpression.Member.Name}";
        }
    }
}
