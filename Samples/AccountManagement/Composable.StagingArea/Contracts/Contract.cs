using System;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
    public static class Contract
    {
        ///<summary>
        ///<para>Start inspecting one or more arguments and extract the name and value of the arguments from a lambda expression</para> 
        ///<para>Using an expression removes the need for an extra string to specify the parameter name and ensures that  the name is always correct.</para>
        ///</summary>
        public static Inspected<TParameter> Arguments<TParameter>(params Expression<Func<TParameter>>[] arguments)
        {
            return CreateInspected(arguments, InspectionType.Argument);
        }

        ///<summary>
        ///<para>Start inspecting one or more arguments and extract the name and value of the arguments from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the parameter name and ensures that  the name is always correct.</para>
        ///</summary>
        public static Inspected<object> Arguments(params Expression<Func<object>>[] arguments)
        {
            return CreateInspected(arguments, InspectionType.Argument);
        }

        ///<summary>
        ///<para>Start inspecting one or more members and extract the name and value of the member from a lambda expression</para> 
        ///<para>Using an expression removes the need for an extra string to specify the member name and ensures that  the name is always correct.</para>
        ///</summary>
        public static Inspected<TParameter> Invariant<TParameter>(params Expression<Func<TParameter>>[] members)
        {
            return CreateInspected(members, InspectionType.Invariant);
                //For now it just delegates to arguments since they do the same thing. Eventually we will want different exceptions(At least messages) for argument vs invariant verifications.
        }

        ///<summary>
        ///<para>Start inspecting one or more members and extract the name and value of the members from a lambda expression</para> 
        /// <para>Using an expression removes the need for an extra string to specify the member name and ensures that  the name is always correct.</para>
        ///</summary>
        public static Inspected<object> Invariant(params Expression<Func<object>>[] arguments)
        {
            return CreateInspected(arguments, InspectionType.Invariant);
        }

        ///<summary>Start inspecting a return value</summary>
        public static Inspected<TReturnValue> ReturnValue<TReturnValue>(TReturnValue returnValue)
        {
            return Optimized.ReturnValue(returnValue);
        }

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return Optimized.Return(returnValue, assert);
        }

        /// <summary>
        /// Returns a less SOLID and less convenient, but faster, interface for performing contract validation.
        /// </summary>
        public static OptimizedContract Optimized { get { return new OptimizedContract(); } }


        private static Inspected<TParameter> CreateInspected<TParameter>(Expression<Func<TParameter>>[] arguments, InspectionType inspectionType)
        { //Yes the loop is not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new InspectedValue<TParameter>[arguments.Length];
            for(var i = 0; i < arguments.Length; i++)
            {
                inspected[i] = new InspectedValue<TParameter>(
                    value: arguments[i].Compile().Invoke(),
                    type: inspectionType,
                    name: ArgumentAccessorExpression.ExtractArgumentName(arguments[i]));
            }
            return new Inspected<TParameter>(inspected);
        }
    }
}

// ReSharper restore UnusedParameter.Global
