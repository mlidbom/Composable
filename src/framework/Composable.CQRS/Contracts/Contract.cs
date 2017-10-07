using System;
using JetBrains.Annotations;

namespace Composable.Contracts
{
    class Contract
    {
        internal static BaseAssertion Invariant => BaseAssertion.Invariant;
        internal static BaseAssertion Arguments => BaseAssertion.Arguments;

        public struct BaseAssertion
        {
            internal static BaseAssertion Invariant = new BaseAssertion(InspectionType.Invariant);
            internal static BaseAssertion Arguments = new BaseAssertion(InspectionType.Argument);

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
