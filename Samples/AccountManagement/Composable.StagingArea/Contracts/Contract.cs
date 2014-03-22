using System;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
    public static class Contract
    {
        public static Inspected<TParameter> Argument<TParameter>(Expression<Func<TParameter>> argument)
        {
            return new Inspected<TParameter>(argument.Compile().Invoke(), ExtractMemberName(argument));
        }

        public static Inspected<TParameter> Arguments<TParameter>(params Expression<Func<TParameter>>[] arguments)
        {
            return new Inspected<TParameter>(
                arguments.Select(argument => new InspectedValue<TParameter>(
                   value: argument.Compile().Invoke(),
                   name: ExtractMemberName(argument))).ToArray()
                );
        }
 
        public static Inspected<TParameter> Argument<TParameter>(TParameter argument, string name = "")
        {
            return new Inspected<TParameter>(argument, name);
        }

        public static Inspected<object> Arguments(params object[] @params)
        {
            return new Inspected<object>(@params.Select(param => new InspectedValue<object>(param)).ToArray());
        }

        public static Inspected<TParameter> Arguments<TParameter>(params TParameter[] @params)
        {
            return new Inspected<TParameter>(@params.Select(param => new InspectedValue<TParameter>(param)).ToArray());
        }

        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            assert(new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(returnValue, "ReturnValue")));
            return returnValue;
        }

        public static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
        {
            return ExtractMemberName((LambdaExpression)func);
        }

        private static string ExtractMemberName(LambdaExpression lambda)
        {
            var body = lambda.Body as MemberExpression;
            if (body != null)
            {
                return body.Member.Name;
            }
            throw new Exception("The lambda passed must be exactly of this form: () => parameterName");
        }
    }
}

// ReSharper restore UnusedParameter.Global
