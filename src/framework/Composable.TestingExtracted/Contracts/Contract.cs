using System;
using System.Linq.Expressions;

// ReSharper disable UnusedParameter.Global

namespace Composable.Testing.Contracts
{
 /// <summary>
    /// Ensures that a class's contract is followed.
    /// <para>Inspects arguments, members and return values and throws different <see cref="ContractViolationException"/>s if the inspection fails.</para>
    /// <para><see cref="Argument{TParameter}"/> inspects method arguments. Call at the very beginning of methods.</para>
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
        public static IInspected<TParameter> Argument<TParameter>(params Expression<Func<TParameter>>[] arguments) => CreateInspected(arguments, InspectionType.Argument);

        static IInspected<TParameter> CreateInspected<TParameter>(Expression<Func<TParameter>>[] arguments, InspectionType inspectionType)
        { //Yes the loop is not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new IInspectedValue<TParameter>[arguments.Length];
            for(var i = 0; i < arguments.Length; i++)
            {
                inspected[i] = new InspectedValue<TParameter>(
                    value: ContractsExpression.ExtractValue(arguments[i]),
                    type: inspectionType,
                    name: ContractsExpression.ExtractName(arguments[i]));
            }
            return new Inspected<TParameter>(inspected);
        }

        internal static readonly IContractAssertion Assert = new ContractAssertionImplementation(InspectionType.Assertion);

        class ContractAssertionImplementation : IContractAssertion
        {
            public ContractAssertionImplementation(InspectionType inspectionType) => InspectionType = inspectionType;
            public InspectionType InspectionType { get; }
        }
    }

    interface IContractAssertion
    {
        InspectionType InspectionType { get; }
    }
}

// ReSharper restore UnusedParameter.Global
