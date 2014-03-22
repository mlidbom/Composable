using System;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
    public static class Contract
    {
        ///<summary>
        ///<para>Start inspecting a single argument and extract the name and value of the argument from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the parameter name and ensures that it is always correct but runs a bit slower.</para>
        /// <para>This version is recommended unless unless performance is paramount.</para>
        ///</summary>
        public static Inspected<TParameter> Argument<TParameter>(Expression<Func<TParameter>> argument)
        {
            return new Inspected<TParameter>(argument.Compile().Invoke(), ExtractMemberName(argument));
        }

        ///<summary>
        ///<para>Start inspecting a multiple arguments and extract the name and value of the arguments from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the parameter name and ensures that it is always correct but runs a bit slower.</para>
        /// <para>This version is recommended unless unless performance is paramount.</para>
        ///</summary>
        public static Inspected<TParameter> Arguments<TParameter>(params Expression<Func<TParameter>>[] arguments)
        {
            return new Inspected<TParameter>(
                arguments.Select(argument => new InspectedValue<TParameter>(
                   value: argument.Compile().Invoke(),
                   name: ExtractMemberName(argument))).ToArray()
                );
        }

        ///<summary>
        ///<para>Start inspecting a multiple arguments and extract the name and value of the arguments from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the parameter name and ensures that it is always correct but runs a bit slower.</para>
        /// <para>This version is recommended unless unless performance is paramount.</para>
        ///</summary>
        public static Inspected<object> Arguments(params Expression<Func<object>>[] arguments)
        {
            return new Inspected<object>(
                arguments.Select(argument => new InspectedValue<object>(
                   value: argument.Compile().Invoke(),
                   name: ExtractMemberName(argument))).ToArray()
                );
        }

        /// <summary>
        /// Returns a less SOLID and less convenient, but faster, interface for performing contract validation.
        /// </summary>
        public static OptimizedContract Optimized
        {
            get { return new OptimizedContract(); }
        }

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return OptimizedContract.Return(returnValue, assert);
        }

        private static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
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
