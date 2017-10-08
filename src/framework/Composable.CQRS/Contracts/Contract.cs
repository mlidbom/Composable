using System;
using JetBrains.Annotations;

namespace Composable.Contracts
{
    static class Contract
    {
        ///<summary>Assert conditions about current state of "this". Failures would mean that someone made a call that is illegal given state of "this".</summary>
        internal static BaseAssertion State { get; } = BaseAssertion.StateInstance;

        ///<summary>Assert something that must always be true for "this".</summary>
        internal static BaseAssertion Invariant { get; } = BaseAssertion.InvariantInstance;

        ///<summary>Assert conditions on arguments to current method.</summary>
        internal static BaseAssertion Argument { get; } = BaseAssertion.ArgumentsInstance;

        public struct BaseAssertion
        {
            internal static BaseAssertion InvariantInstance = new BaseAssertion(InspectionType.Invariant);
            internal static BaseAssertion ArgumentsInstance = new BaseAssertion(InspectionType.Argument);
            internal static BaseAssertion StateInstance = new BaseAssertion(InspectionType.State);

            readonly InspectionType _assertionType;
            BaseAssertion(InspectionType assertionType) => _assertionType = assertionType;

            [ContractAnnotation("c1:false => halt")] public ChainedAssertion Assert(bool c1) => RunAssertions(0, _assertionType, c1);
            [ContractAnnotation("c1:false => halt; c2:false => halt")] public ChainedAssertion Assert(bool c1, bool c2) => RunAssertions(0, _assertionType, c1, c2);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt")] public ChainedAssertion Assert(bool c1, bool c2, bool c3) => RunAssertions(0, _assertionType, c1, c2, c3);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt; c4:false => halt")] public ChainedAssertion Assert(bool c1, bool c2, bool c3, bool c4) => RunAssertions(0, _assertionType, c1, c2, c3, c4);
        }

        public struct ChainedAssertion
        {
            readonly InspectionType _inspectionType;
            readonly int _recursionDepth;
            internal ChainedAssertion(InspectionType inspectionType, int recursionDepth)
            {
                _inspectionType = inspectionType;
                _recursionDepth = recursionDepth;
            }

            [ContractAnnotation("c1:false => halt")] public ChainedAssertion And(bool c1) => RunAssertions(_recursionDepth, _inspectionType, c1);
            [ContractAnnotation("c1:false => halt; c2:false => halt")] public ChainedAssertion And(bool c1, bool c2) => RunAssertions(_recursionDepth, _inspectionType, c1, c2);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt")] public ChainedAssertion And(bool c1, bool c2, bool c3) => RunAssertions(_recursionDepth, _inspectionType, c1, c2, c3);
            [ContractAnnotation("c1:false => halt; c2:false => halt; c3:false => halt; c4:false => halt")] public ChainedAssertion And(bool c1, bool c2, bool c3, bool c4) => RunAssertions(_recursionDepth, _inspectionType, c1, c2, c3, c4);

        }

        static ChainedAssertion RunAssertions(int recursionLevel, InspectionType inspectionType, params bool[] conditions)
        {
            for (int condition = 0; condition < conditions.Length; condition++)
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
