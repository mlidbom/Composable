using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

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
        ///</summary>
        public static IInspected<TParameter> Argument<TParameter>(params (TParameter Value, string Name)[] arguments) => new Inspected<TParameter>(InspectionType.Argument, arguments);


        public static IInspected<object> Argument(object? p1, [InvokerParameterName] string n1) =>
            new Inspected<object>(InspectionType.Argument, (p1, n1));

        public static IInspected<object> Argument(object? p1, [InvokerParameterName] string n1, object? p2, [InvokerParameterName] string n2) =>
            new Inspected<object>(InspectionType.Argument, (p1!, n1), (p2!, n2));

        public static IInspected<object> Argument(object? p1, [InvokerParameterName] string n1, object? p2, [InvokerParameterName] string n2, object? p3, [InvokerParameterName] string n3) =>
            new Inspected<object>(InspectionType.Argument, (p1!, n1), (p2!, n2), (p3!, n3));

        public static IInspected<TInspected> Argument<TInspected>(TInspected p1, [InvokerParameterName] string n1) =>
            new Inspected<TInspected>(InspectionType.Argument, (p1, n1));

        public static IInspected<TInspected> Argument<TInspected>(TInspected p1, [InvokerParameterName] string n1, TInspected p2, [InvokerParameterName] string n2) =>
            new Inspected<TInspected>(InspectionType.Argument, (p1!, n1), (p2!, n2));

        public static IInspected<TInspected> Argument<TInspected>(TInspected p1, [InvokerParameterName] string n1, TInspected p2, [InvokerParameterName] string n2, TInspected p3, [InvokerParameterName] string n3) =>
            new Inspected<TInspected>(InspectionType.Argument, (p1!, n1), (p2!, n2), (p3!, n3));


        public static IInspected<object> Invariant(object? p1, [InvokerParameterName] string n1) =>
            new Inspected<object>(InspectionType.Invariant, (p1, n1));

        public static IInspected<object> Invariant(object? p1, [InvokerParameterName] string n1, object? p2, [InvokerParameterName] string n2) =>
            new Inspected<object>(InspectionType.Invariant, (p1!, n1), (p2!, n2));

        public static IInspected<object> Invariant(object? p1, [InvokerParameterName] string n1, object? p2, [InvokerParameterName] string n2, object? p3, [InvokerParameterName] string n3) =>
            new Inspected<object>(InspectionType.Invariant, (p1!, n1), (p2!, n2), (p3!, n3));

        public static IInspected<TInspected> Invariant<TInspected>(TInspected p1, [InvokerParameterName] string n1) =>
            new Inspected<TInspected>(InspectionType.Invariant, (p1, n1));

        public static IInspected<TInspected> Invariant<TInspected>(TInspected p1, [InvokerParameterName] string n1, TInspected p2, [InvokerParameterName] string n2) =>
            new Inspected<TInspected>(InspectionType.Invariant, (p1!, n1), (p2!, n2));

        public static IInspected<TInspected> Invariant<TInspected>(TInspected p1, [InvokerParameterName] string n1, TInspected p2, [InvokerParameterName] string n2, TInspected p3, [InvokerParameterName] string n3) =>
            new Inspected<TInspected>(InspectionType.Invariant, (p1!, n1), (p2!, n2), (p3!, n3));


        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1) =>
            ArgumentNotNull((p1, n1));
        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2) =>
            ArgumentNotNull((p1, n1), (p2, n2));
        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3) =>
            ArgumentNotNull((p1, n1), (p2, n2), (p3, n3));
        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4) =>
            ArgumentNotNull((p1, n1), (p2, n2), (p3, n3),(p4, n4));
        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4, [NotNull]object? p5, [InvokerParameterName] string n5) =>
            ArgumentNotNull((p1, n1), (p2, n2), (p3, n3),(p4, n4),(p5, n5));
        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4, [NotNull]object? p5, [InvokerParameterName] string n5, [NotNull]object? p6, [InvokerParameterName] string n6) =>
            ArgumentNotNull((p1, n1), (p2, n2), (p3, n3),(p4, n4),(p5, n5),(p6, n6));
        public static void ArgumentNotNull([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4, [NotNull]object? p5, [InvokerParameterName] string n5, [NotNull]object? p6, [InvokerParameterName] string n6, [NotNull]object? p7, [InvokerParameterName] string n7) =>
            ArgumentNotNull((p1, n1), (p2, n2), (p3, n3),(p4, n4),(p5, n5),(p6, n6), (p7, n7));

        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1) =>
            ArgumentNotNullOrDefault((p1, n1));
        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2) =>
            ArgumentNotNullOrDefault((p1, n1), (p2, n2));
        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3) =>
            ArgumentNotNullOrDefault((p1, n1), (p2, n2), (p3, n3));
        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4) =>
            ArgumentNotNullOrDefault((p1, n1), (p2, n2), (p3, n3),(p4, n4));
        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4, [NotNull]object? p5, [InvokerParameterName] string n5) =>
            ArgumentNotNullOrDefault((p1, n1), (p2, n2), (p3, n3),(p4, n4),(p5, n5));
        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4, [NotNull]object? p5, [InvokerParameterName] string n5, [NotNull]object? p6, [InvokerParameterName] string n6) =>
            ArgumentNotNullOrDefault((p1, n1), (p2, n2), (p3, n3),(p4, n4),(p5, n5),(p6, n6));
        public static void ArgumentNotNullOrDefault([NotNull]object? p1, [InvokerParameterName] string n1, [NotNull]object? p2, [InvokerParameterName] string n2, [NotNull]object? p3, [InvokerParameterName] string n3, [NotNull]object? p4, [InvokerParameterName] string n4, [NotNull]object? p5, [InvokerParameterName] string n5, [NotNull]object? p6, [InvokerParameterName] string n6, [NotNull]object? p7, [InvokerParameterName] string n7) =>
            ArgumentNotNullOrDefault((p1, n1), (p2, n2), (p3, n3),(p4, n4),(p5, n5),(p6, n6), (p7, n7));

        static void ArgumentNotNull(params (object? Argument, string Name)[] arguments)
        {
            for(int i = 0; i < arguments.Length; i++)
            {
                if(arguments[i].Argument is null)
                {
                    throw new ArgumentNullException(arguments[0].Name);
                }
            }
        }

        static void ArgumentNotNullOrDefault(params (object? Value, string Name)[] arguments)
        {
            for(int i = 0; i < arguments.Length; i++)
            {
                if(NullOrDefaultTester<object>.IsNullOrDefault(arguments[i].Value))
                {
                    throw new ArgumentNullException(arguments[0].Name);
                }
            }
        }


        ///<summary>
        ///<para>Start inspecting one or more arguments for contract compliance.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///</summary>
        public static IInspected<TParameter> Argument<TParameter>(params Expression<Func<TParameter>>[] arguments) => CreateInspected(arguments, InspectionType.Argument);

        ///<summary>
        ///<para>Start inspecting one or more arguments for contract compliance.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static IInspected<object> Argument(params Expression<Func<object>>[] arguments) => CreateInspected(arguments, InspectionType.Argument);

        ///<summary>
        ///<para>Start inspecting one or more members for contract compliance.</para>
        /// <para>An invariant is something that must always be true for an object. Like email and password never being missing for an account.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        internal static IInspected<TParameter> Invariant<TParameter>(params Expression<Func<TParameter>>[] members) => CreateInspected(members, InspectionType.Invariant);

        ///<summary>
        ///<para>Start inspecting one or more members for contract compliance.</para>
        /// <para>An invariant is something that must always be true for an object. Like email and password never being missing for an account.</para>
        ///<para>Using an expression removes the need for an extra string to specify the name and ensures that  the name is always correct in exceptions.</para>
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        public static IInspected<object> Invariant(params Expression<Func<object>>[] arguments) => CreateInspected(arguments, InspectionType.Invariant);

        ///<summary>Start inspecting a return value
        ///<para>The returned type : <see cref="Inspected{TValue}"/> can be easily extended with extension methods to support generic inspections.</para>
        ///<code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
        ///</summary>
        internal static IInspected<TReturnValue> ReturnValue<TReturnValue>(TReturnValue returnValue) => new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(InspectionType.ReturnValue, returnValue, "ReturnValue"));

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<IInspected<TReturnValue>> assert)
        {
            assert(ReturnValue(returnValue));
            return returnValue;
        }

        static IInspected<TParameter> CreateInspected<TParameter>(Expression<Func<TParameter>>[] arguments, InspectionType inspectionType)
        { //Yes the loop is not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new IInspectedValue<TParameter>[arguments.Length];
            for(var i = 0; i < arguments.Length; i++)
            {
                inspected[i] = new InspectedValue<TParameter>(type: inspectionType,
                    value: ContractsExpression.ExtractValue(arguments[i]),
                    name: ContractsExpression.ExtractName(arguments[i]));
            }
            return new Inspected<TParameter>(inspected);
        }

        internal static readonly IContractAssertion Assert = new ContractAssertionImplementation(InspectionType.Assertion);
        internal static readonly IContractAssertion Arguments = new ContractAssertionImplementation(InspectionType.Argument);

        internal static void AssertThat(params bool[] conditions)
        {
            for(var condition = 0; condition < conditions.Length; condition++)
            {
                if(!conditions[condition])
                {
                    throw new ContractAssertThatException(condition);
                }
            }
        }

        class ContractAssertionImplementation : IContractAssertion
        {
            public ContractAssertionImplementation(InspectionType inspectionType) => InspectionType = inspectionType;
            public InspectionType InspectionType { get; }
        }
    }

    public class ContractAssertThatException : Exception
    {
        public ContractAssertThatException(int condition):base($"Condition: {condition} was false")
        {}
    }

    interface IContractAssertion
    {
        InspectionType InspectionType { get; }
    }
}

// ReSharper restore UnusedParameter.Global
