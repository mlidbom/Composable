using System;
using System.Data.SqlTypes;
using Composable.Persistence.EventStore;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.IEventStorePersistenceLayer.ReadOrder;

namespace Composable.Tests.Persistence.EventStore
{
    [TestFixture] public class ReadOrderTests
    {
        [Test] public void Parse_followed_by_ToString_always_results_in_identical_string()
        {
            var maxValue = $"{long.MaxValue}.{long.MaxValue}";

            ReadOrder.Parse(maxValue).ToString().Should().Be(maxValue);
            ReadOrder.Parse(CreateString(1, 1)).ToString().Should().Be(CreateString(1, 1));
        }

        [Test] public void Parse_throws_on_negative_numbers()
        {
            Assert.Throws<ArgumentException>(() => ReadOrder.Parse(CreateString(0, -1)))
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
            void TestValue(ReadOrder value)
            {
                var stringValue = value.ToString();
                var sql = value.ToSqlDecimal();
                var order = value;
                ReadOrder.FromSqlDecimal(sql).Should().Be(value);

                sql.ToString().Should().Be(stringValue);
                order.ToString().Should().Be(stringValue);

                order.ToSqlDecimal().Should().Be(sql);

                ReadOrder.FromSqlDecimal(sql).ToString().Should().Be(stringValue);
            }

            TestValue(Create(1, 2));
        }

        [Test] public void InsertionIntervals()
        {
            ReadOrder.CreateOrdersForEventsBetween(2, Create(1, 0), Create(2, 0))
                                     .ForEach(@this => Console.WriteLine(@this));


             ReadOrder.CreateOrdersForEventsBetween(2, Create(1, 10), Create(1, 3000))
                .ForEach(@this => Console.WriteLine(@this));
        }

        static ReadOrder Create(long order, long offset) => ReadOrder.Parse($"{order}.{offset:D19}");
        static string CreateString(int order, int value) => $"{order}.{DecimalPlaces(value)}";
        static string DecimalPlaces(int number) => new string(number.ToString()[0], 19);
    }
}
