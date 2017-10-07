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
            Action<IInspected<TInspected>> assert,
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

        static void InspectGoodValues<TInspected>(Action<IInspected<TInspected>> assert, TInspected goodValue)
        {
            var inspected = OldContract.Argument(() => goodValue);
            assert(inspected);

            inspected = OldContract.Argument(() => goodValue);
            assert(inspected);

            inspected = OldContract.Invariant(() => goodValue);
            assert(inspected);

            Return(goodValue, assert);
        }

        internal static void InspectBadValue<TException, TInspected>(Action<IInspected<TInspected>> assert, TInspected inspectedValue)
            where TException : ContractViolationException
        {
            Expression<Func<TInspected>> fetchInspected = () => inspectedValue;
            var inspectedName = ContractsExpression.ExtractName(fetchInspected);

            var inspected = OldContract.Argument(() => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

            inspected = OldContract.Argument(() => inspectedValue, () => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Argument,
                badValueName: inspectedName);

            inspected = OldContract.Invariant(() => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Invariant,
                badValueName: inspectedName);

            inspected = OldContract.Invariant(() => inspectedValue, () => inspectedValue);
            AssertThrows<TException, TInspected>(
                inspected: inspected,
                assert: assert,
                inspectionType: InspectionType.Invariant,
                badValueName: inspectedName);

            const string returnvalueName = "ReturnValue";
            inspected = OldContract.ReturnValue(inspectedValue);
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

        static void AssertThrows<TException, TInspected>(IInspected<TInspected> inspected,
            Action<IInspected<TInspected>> assert,
            InspectionType inspectionType,
            string badValueName)
            where TException : ContractViolationException
        {
            var exception = Assert.Throws<TException>(() => assert(inspected));

            exception.BadValue.Type.Should().Be(inspectionType);
            exception.BadValue.Name.Should().Be(badValueName);
        }

        static void Return<TReturnValue>(TReturnValue returnValue, Action<IInspected<TReturnValue>> assert)
        {
            OldContract.Return(returnValue, assert);
        }

        static void ReturnOptimized<TReturnValue>(TReturnValue returnValue, Action<IInspected<TReturnValue>> assert)
        {
            OldContract.Return(returnValue, assert);
        }
    }
}
