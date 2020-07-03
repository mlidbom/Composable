using System;
using System.Data.SqlTypes;
using Composable.Persistence.EventStore;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;
using ReadOrder = Composable.Persistence.EventStore.IEventStorePersistenceLayer.ReadOrder;

namespace Composable.Tests.Persistence.EventStore
{
    [TestFixture] public class ReadOrderTests
    {
        [Test] public void Parse_followed_by_ToString_always_results_in_identical_string()
        {
            var maxValue = $"{long.MaxValue}.{long.MaxValue}";

            ReadOrder.Parse(maxValue).ToString().Should().Be(maxValue);
            ReadOrder.Parse(Create(1, 1)).ToString().Should().Be(Create(1, 1));
        }

        [Test] public void Parse_throws_on_negative_numbers()
        {
            Assert.Throws<ArgumentException>(() => ReadOrder.Parse(Create(0, -1)))
                  .Message.Should().Contain("negative");
        }

        [Test] public void Parse_requires_exactly_19_decimal_point_numbers()
        {
            1.Through(18).ForEach(
                num => Assert.Throws<ArgumentException>(
                    () => ReadOrder.Parse($"1.{new string('1', num)}")).Message.Should().Contain("decimal numbers"));

            ReadOrder.Parse($"1.{new string('1', 19)}");

            20.Through(40).ForEach(
                num => Assert.Throws<ArgumentException>(
                    () => ReadOrder.Parse($"1.{new string('1', num)}")).Message.Should().Contain("decimal numbers"));
        }

        [Test] public void Parse_sets_order_and_offset_to_parsed_string_values()
        {
            var maxValue = $"{long.MaxValue}.{long.MaxValue}";
            var parsed = ReadOrder.Parse(maxValue);
            parsed.Order.Should().Be(long.MaxValue);
            parsed.OffSet.Should().Be(long.MaxValue);


        }

        [Test] public void RoundTripping_SqlDecimal_results_in_same_value()
        {
            void TestValue(string value)
            {
                var sql = SqlDecimal.Parse(value);
                var order = ReadOrder.Parse(value);

                sql.ToString().Should().Be(value);
                order.ToString().Should().Be(value);

                order.ToSqlDecimal().Should().Be(sql);

                ReadOrder.FromSqlDecimal(sql).ToString().Should().Be(value);
            }

            TestValue(Create(1, 2));
        }

        static string Create(int order, int value) => $"{order}.{DecimalPlaces(value)}";
        static string DecimalPlaces(int number) => new string(number.ToString()[0], 19);
    }
}
