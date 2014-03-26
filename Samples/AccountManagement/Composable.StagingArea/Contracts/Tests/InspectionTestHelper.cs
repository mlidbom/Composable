using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    public static class InspectionTestHelper
    {
        public static void BatchTestInspection<TException, TInspected>(
            Action<Inspected<TInspected>> assert,
            IEnumerable<TInspected> badValues,
            IEnumerable<TInspected> goodValues
            )
            where TException : ContractViolationException
        {
            foreach(var badValue in badValues)
            {
                InspectBadValue<TException, TInspected>(assert, badValue);
            }

            foreach(var goodValue in goodValues)
            {
                InspectGoodValues(assert, goodValue);
            }
        }

        public static void InspectGoodValues<TInspected>(Action<Inspected<TInspected>> assert, TInspected goodValue)
        {
            Expression<Func<TInspected>> fetchInspected = () => goodValue;
            var inspectedName = ExpressionUtil.ExtractMemberName(fetchInspected);

            var inspected = Contract.Optimized.Argument(goodValue, inspectedName);
            assert(inspected);

            inspected = Contract.Optimized.Arguments(goodValue);
            assert(inspected);

            inspected = Contract.Arguments(() => goodValue);
            assert(inspected);

            inspected = Contract.Optimized.Invariant(goodValue);
            assert(inspected);

            inspected = Contract.Invariant(() => goodValue);
            assert(inspected);

            Return(goodValue, assert);
        }

        public static void InspectBadValue<TException, TInspected>(Action<Inspected<TInspected>> assert, TInspected inspectedValue)
            where TException : ContractViolationException
        {
            Expression<Func<TInspected>> fetchInspected = () => inspectedValue;
            var inspectedName = ExpressionUtil.ExtractMemberName(fetchInspected);

            var inspected = Contract.Optimized.Argument(inspectedValue, inspectedName);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

            inspected = Contract.Optimized.Arguments(inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: "");

            inspected = Contract.Optimized.Arguments(inspectedValue, inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: "");

            inspected = Contract.Arguments(() => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

            inspected = Contract.Arguments(() => inspectedValue, () => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

            inspected = Contract.Optimized.Invariant(inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Invariant,
                badValueName: "");

            inspected = Contract.Invariant(() => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Invariant,
                badValueName: inspectedName);

            inspected = Contract.Invariant(() => inspectedValue, () => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Invariant,
                badValueName: inspectedName);

            const string returnvalueName = "ReturnValue";
            inspected = Contract.ReturnValue(inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.ReturnValue,
                badValueName: returnvalueName);

            inspected = Contract.Optimized.ReturnValue(inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.ReturnValue,
                badValueName: returnvalueName);

            var exception = Assert.Throws<TException>(() => Return(inspectedValue, assert));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);            
            exception.BadValue.Name.Should().Be(returnvalueName);

            exception = Assert.Throws<TException>(() => ReturnOptimized(inspectedValue, assert));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);
            exception.BadValue.Name.Should().Be(returnvalueName);
        }

        private static void AssertThrows<TException, TInspected>(Inspected<TInspected> inspected,
            Action<Inspected<TInspected>> assert,
            InspectionType inspectionType,
            string badValueName)
            where TException : ContractViolationException
        {
            var exception = Assert.Throws<TException>(() => assert(inspected));

            exception.BadValue.Type.Should().Be(inspectionType);
            exception.BadValue.Name.Should().Be(badValueName);
        }


        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return Contract.Return(returnValue, assert);
        }

        public static TReturnValue ReturnOptimized<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            return Contract.Optimized.Return(returnValue, assert);
        }
    }
}
