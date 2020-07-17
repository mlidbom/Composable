using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NotNull = global::System.Diagnostics.CodeAnalysis.NotNullAttribute;
#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Composable.Contracts
{
    public static class Assert
    {
        ///<summary>Assert conditions about current state of "this". Failures would mean that someone made a call that is illegal given state of "this".</summary>
        public static BaseAssertion State { get; } = BaseAssertion.StateInstance;

        ///<summary>Assert something that must always be true for "this".</summary>
        public static BaseAssertion Invariant { get; } = BaseAssertion.InvariantInstance;

        ///<summary>Assert conditions on arguments to current method.</summary>
        public static BaseAssertion Argument { get; } = BaseAssertion.ArgumentsInstance;

        ///<summary>Assert conditions on the result of makeing a method call.</summary>
        public static BaseAssertion Result { get; } = BaseAssertion.ResultInstance;



        public readonly struct BaseAssertion
        {
            internal static BaseAssertion InvariantInstance = new BaseAssertion(InspectionType.Invariant);
            internal static BaseAssertion ArgumentsInstance = new BaseAssertion(InspectionType.Argument);
            internal static BaseAssertion StateInstance = new BaseAssertion(InspectionType.State);
            internal static BaseAssertion ResultInstance = new BaseAssertion(InspectionType.Result);

            readonly InspectionType _inspectionType;
            BaseAssertion(InspectionType inspectionType) => _inspectionType = inspectionType;

            [ContractAnnotation("c1:false => halt")] public ChainedAssertion Assert([DoesNotReturnIf(false)]bool c1) => RunAssertions(0, _inspectionType, c1);
            [ContractAnnotation("c1:false => halt; c2:false => halt")] public ChainedAssertion Assert([DoesNotReturnIf(false)]bool c1, [DoesNotReturnIf(false)]bool c2) => RunAssertions(0, _inspectionType, c1, c2);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt")] public ChainedAssertion Assert([DoesNotReturnIf(false)]bool c1, [DoesNotReturnIf(false)]bool c2, [DoesNotReturnIf(false)]bool c3) => RunAssertions(0, _inspectionType, c1, c2, c3);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt; c4:false => halt")] public ChainedAssertion Assert([DoesNotReturnIf(false)]bool c1, [DoesNotReturnIf(false)]bool c2, [DoesNotReturnIf(false)]bool c3, [DoesNotReturnIf(false)]bool c4) => RunAssertions(0, _inspectionType, c1, c2, c3, c4);


            [return: NotNull] [ContractAnnotation("obj:null => halt")]
            public TValue NotNull<TValue>([NotNull][AllowNull]TValue obj) => obj switch
            {
                // ReSharper disable once PatternAlwaysOfType
                TValue instance => instance,
                null => throw new AssertionException(_inspectionType, 0)
            };

#pragma warning disable CS8777 //Reviewed OK: We do know for sure that these parameters have been verified to not be null when method exits.
            [return: NotNull] [ContractAnnotation("obj:null => halt")]
            public TValue NotNullOrDefault<TValue>([AllowNull]TValue obj)
            {
                if(NullOrDefaultTester<TValue>.IsNullOrDefault(obj))
                {
                    throw new AssertionException(_inspectionType, 0);
                }

                return obj!;
            }

            [ContractAnnotation("c1:null => halt; c2:null => halt")] public ChainedAssertion NotNull([NotNull][AllowNull]object c1, [NotNull][AllowNull]object c2) => RunNotNull(0, _inspectionType, c1, c2);
            [ContractAnnotation("c1:null => halt; c2:null => halt; c3:null => halt")] public ChainedAssertion NotNull([NotNull][AllowNull]object c1, [NotNull][AllowNull]object c2, [NotNull][AllowNull]object c3) => RunNotNull(0, _inspectionType, c1, c2, c3);
            [ContractAnnotation("c1:null => halt; c2:null => halt; c3:null => halt; c4:null => halt")] public ChainedAssertion NotNull([NotNull][AllowNull]object c1, [NotNull][AllowNull]object c2, [NotNull][AllowNull]object c3, [NotNull][AllowNull]object c4) => RunNotNull(0, _inspectionType, c1, c2, c3, c4);
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
        }

        public readonly struct ChainedAssertion
        {
            readonly InspectionType _inspectionType;
            readonly int _recursionDepth;
            internal ChainedAssertion(InspectionType inspectionType, int recursionDepth)
            {
                _inspectionType = inspectionType;
                _recursionDepth = recursionDepth;
            }

            [ContractAnnotation("c1:false => halt")] public ChainedAssertion And([DoesNotReturnIf(false)]bool c1) => RunAssertions(_recursionDepth, _inspectionType, c1);
            [ContractAnnotation("c1:false => halt; c2:false => halt")] public ChainedAssertion And([DoesNotReturnIf(false)]bool c1, [DoesNotReturnIf(false)]bool c2) => RunAssertions(_recursionDepth, _inspectionType, c1, c2);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt")] public ChainedAssertion And([DoesNotReturnIf(false)]bool c1, [DoesNotReturnIf(false)]bool c2, [DoesNotReturnIf(false)]bool c3) => RunAssertions(_recursionDepth, _inspectionType, c1, c2, c3);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt; c4:false => halt")] public ChainedAssertion And([DoesNotReturnIf(false)]bool c1, [DoesNotReturnIf(false)]bool c2, [DoesNotReturnIf(false)]bool c3, [DoesNotReturnIf(false)]bool c4) => RunAssertions(_recursionDepth, _inspectionType, c1, c2, c3, c4);

            [ContractAnnotation("c1:null => halt")] public ChainedAssertion NotNull(object c1) => RunNotNull(0, _inspectionType, c1);
            [ContractAnnotation("c1:null => halt; c2:null => halt")] public ChainedAssertion NotNull(object c1, object c2) => RunNotNull(0, _inspectionType, c1, c2);
            [ContractAnnotation("c1:null => halt; c2:null => halt; c3:null => halt")] public ChainedAssertion NotNull(object c1, object c2, object c3) => RunNotNull(0, _inspectionType, c1, c2, c3);
            [ContractAnnotation("c1:null => halt; c2:null => halt; c3:null => halt; c4:null => halt")] public ChainedAssertion NotNull(object c1, object c2, object c3, object c4) => RunNotNull(0, _inspectionType, c1, c2, c3, c4);
        }

        static ChainedAssertion RunNotNull(int recursionLevel, InspectionType inspectionType, params object?[] instances)
        {
            for (var index = 0; index < instances.Length; index++)
            {
                if (instances[index] == null)
                {
                    throw new AssertionException(inspectionType, index);
                }
            }
            return new ChainedAssertion(inspectionType, recursionLevel + 1);
        }

        static ChainedAssertion RunAssertions(int recursionLevel, InspectionType inspectionType, params bool[] conditions)
        {
            for (var condition = 0; condition < conditions.Length; condition++)
            {
                if (!conditions[condition])
                {
                    throw new AssertionException(inspectionType, condition);
                }
            }
            return new ChainedAssertion(inspectionType, recursionLevel + 1);
        }

        class AssertionException : Exception
        {
            public AssertionException(InspectionType inspectionType, int index) : base($"{inspectionType}: {index}") { }
        }
    }
}
