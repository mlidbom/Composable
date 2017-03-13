using System;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
 /// <summary>
    /// Ensures that a class's contract is followed.
    /// <para>Inspects arguments, members and return values and throws different <see cref="ContractViolationException"/>s if the inspection fails.</para>
    /// <para><see cref="Argument{TParameter}"/> inspects method arguments. Call at the very beginning of methods.</para>
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
        public static Inspected<TParameter> Argument<TParameter>(params Expression<Func<TParameter>>[] arguments) => CreateInspected(arguments, InspectionType.Argument);

        ///<summary>
        ///<para>Start inspecting one or more arguments for contract compliance.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static Inspected<object> Argument(params Expression<Func<object>>[] arguments) => CreateInspected(arguments, InspectionType.Argument);

        ///<summary>
        ///<para>Start inspecting one or more members for contract compliance.</para>
        /// <para>An invariant is something that must always be true for an object. Like email and password never being missing for an account.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        internal static Inspected<TParameter> Invariant<TParameter>(params Expression<Func<TParameter>>[] members) => CreateInspected(members, InspectionType.Invariant);

        ///<summary>
        ///<para>Start inspecting one or more members for contract compliance.</para>
        /// <para>An invariant is something that must always be true for an object. Like email and password never being missing for an account.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static Inspected<object> Invariant(params Expression<Func<object>>[] arguments) => CreateInspected(arguments, InspectionType.Invariant);

        ///<summary>Start inspecting a return value
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        internal static Inspected<TReturnValue> ReturnValue<TReturnValue>(TReturnValue returnValue) => new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(returnValue, InspectionType.ReturnValue, "ReturnValue"));

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            assert(ReturnValue(returnValue));
            return returnValue;
        }

        static Inspected<TParameter> CreateInspected<TParameter>(Expression<Func<TParameter>>[] arguments, InspectionType inspectionType)
        { //Yes the loop is not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new InspectedValue<TParameter>[arguments.Length];
            for(var i = 0; i < arguments.Length; i++)
            {
                inspected[i] = new InspectedValue<TParameter>(
                    value: ContractsExpression.ExtractValue(arguments[i]),
                    type: inspectionType,
                    name: ContractsExpression.ExtractName(arguments[i]));
            }
            return new Inspected<TParameter>(inspected);
        }

        internal static void Assert(bool assertion)
        {
            if(!assertion)
            {
                throw new AssertionException();
            }
        }

        internal static void Assert(bool assertion, string failureMessage)
        {
            if (!assertion)
            {
                throw new AssertionException(failureMessage);
            }
        }

        //internal static AssertClass Assert => AssertClass.Instance;


    }

    internal class ContractAssertion
    {
        readonly ContractAssertion Instance = new ContractAssertion();
    }
}

// ReSharper restore UnusedParameter.Global
