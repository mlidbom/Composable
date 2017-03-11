using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    static class InspectionTestHelper
    {
        internal static void BatchTestInspection<TException, TInspected>(
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

        static void InspectGoodValues<TInspected>(Action<Inspected<TInspected>> assert, TInspected goodValue)
        {
            Expression<Func<TInspected>> fetchInspected = () => goodValue;
            var inspectedName = ContractsExpression.ExtractName(fetchInspected);

            var inspected = Contract.Argument(() => goodValue);
            assert(inspected);

            inspected = Contract.Argument(() => goodValue);
            assert(inspected);

            inspected = Contract.Invariant(() => goodValue);
            assert(inspected);

            Return(goodValue, assert);
        }

        internal static void InspectBadValue<TException, TInspected>(Action<Inspected<TInspected>> assert, TInspected inspectedValue)
            where TException : ContractViolationException
        {
            Expression<Func<TInspected>> fetchInspected = () => inspectedValue;
            var inspectedName = ContractsExpression.ExtractName(fetchInspected);

            var inspected = Contract.Argument(() => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

            inspected = Contract.Argument(() => inspectedValue, () => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

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

            var exception = Assert.Throws<TException>(() => Return(inspectedValue, assert));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);
            exception.BadValue.Name.Should().Be(returnvalueName);

            exception = Assert.Throws<TException>(() => ReturnOptimized(inspectedValue, assert));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);
            exception.BadValue.Name.Should().Be(returnvalueName);
        }

        static void AssertThrows<TException, TInspected>(Inspected<TInspected> inspected,
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
            return Contract.Return(returnValue, assert);
        }
    }
}
