using System;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
 /// <summary>
    /// Ensures that a class's contract is followed. 
    /// <para>Inspects arguments, members and return values and throws different <see cref="ContractViolationException"/> if the inspection fails.</para>
    /// <para><see cref="Arguments{TParameter}"/> inspects method arguments. Call at the very beginning of methods.</para>
    /// <para><see cref="ReturnValue{TReturnValue}"/> and <see cref="Return{TReturnValue}"/> inspects the return value from a method. Call at the very end of a method.</para>
    /// <para><see cref="Invariant"/> inspects class members(Fields and Properties). Call within a shared method called something like AssertInvariantsAreMet.</para>
    /// <para>.</para>
    /// <para>The returned type of all these methods: <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
    /// <code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }
    /// </code>
    /// </summary>
    public static class Contract
    {
        ///<summary>
        ///<para>Start inspecting one or more arguments for contract compliance.</para> 
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///</summary>
        public static Inspected<TParameter> Arguments<TParameter>(params Expression<Func<TParameter>>[] arguments)
        {
            return CreateInspected(arguments, InspectionType.Argument);
        }

        ///<summary>
        ///<para>Start inspecting one or more arguments for contract compliance.</para> 
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static Inspected<object> Arguments(params Expression<Func<object>>[] arguments)
        {
            return CreateInspected(arguments, InspectionType.Argument);
        }

        ///<summary>
        ///<para>Start inspecting one or more members for contract compliance.</para>
        /// <para>An invariant is something that must always be true for an object. Like email and password never being missing for an account.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static Inspected<TParameter> Invariant<TParameter>(params Expression<Func<TParameter>>[] members)
        {
            return CreateInspected(members, InspectionType.Invariant);
                //For now it just delegates to arguments since they do the same thing. Eventually we will want different exceptions(At least messages) for argument vs invariant verifications.
        }

        ///<summary>
        ///<para>Start inspecting one or more members for contract compliance.</para>
        /// <para>An invariant is something that must always be true for an object. Like email and password never being missing for an account.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static Inspected<object> Invariant(params Expression<Func<object>>[] arguments)
        {
            return CreateInspected(arguments, InspectionType.Invariant);
        }

        ///<summary>Start inspecting a return value
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code> 
        ///</summary>
        public static Inspected<TReturnValue> ReturnValue<TReturnValue>(TReturnValue returnValue)
        {
            return Optimized.ReturnValue(returnValue);
        }

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return Optimized.Return(returnValue, assert);
        }

        /// <summary>Returns a less SOLID and less convenient, but faster, interface for performing contract validation.</summary>
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
