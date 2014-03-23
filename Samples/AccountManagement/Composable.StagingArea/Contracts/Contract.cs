using System;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
    public static class Contract
    {
        ///<summary>
        ///<para>Start inspecting a multiple arguments and extract the name and value of the arguments from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the parameter name and ensures that it is always correct but runs a bit slower.</para>
        ///</summary>
        public static Inspected<TParameter> Arguments<TParameter>(params Expression<Func<TParameter>>[] arguments)
        {
            //Yes the loops are not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new InspectedValue<TParameter>[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                inspected[i] = new InspectedValue<TParameter>(
                    value: arguments[i].Compile().Invoke(),
                    name: ArgumentAccessorExpression.ExtractArgumentName(arguments[i]));
            }
            return new Inspected<TParameter>(inspected);
        }

        ///<summary>
        ///<para>Start inspecting a multiple arguments and extract the name and value of the arguments from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the parameter name and ensures that it is always correct but runs a bit slower.</para>
        ///</summary>
        public static Inspected<object> Arguments(params Expression<Func<object>>[] arguments)
        {
            return Arguments<object>(arguments);
        }

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return OptimizedContract.Return(returnValue, assert);
        }

        /// <summary>
        /// Returns a less SOLID and less convenient, but faster, interface for performing contract validation.
        /// </summary>
        public static OptimizedContract Optimized { get { return new OptimizedContract(); } }
    }
}

// ReSharper restore UnusedParameter.Global
