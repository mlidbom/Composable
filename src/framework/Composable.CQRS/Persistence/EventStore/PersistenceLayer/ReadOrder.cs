using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using Composable.Contracts;
using Composable.SystemCE.Linq;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    readonly struct ReadOrder : IComparable<ReadOrder>
    {
        public long Order { get; }

        //Urgent: Figure out what do do about this type. We will probably be using Decimal(38,19) in all the persistence layers to save the readorder, so should this be long?
        public long OffSet { get; }

        public override string ToString() => $"{Order}.{OffSet:D19}";

        public static readonly ReadOrder Zero = new ReadOrder(0, 0);

        internal ReadOrder(long order, long offSet)
        {
            if(order < 0) throw new ArgumentException($"{nameof(order)} Must be >= 0");
            if(offSet < 0) throw new ArgumentException($"{nameof(offSet)} Must be >= 0");

            Order = order;
            OffSet = offSet;
        }

        public SqlDecimal ToSqlDecimal() => SqlDecimal.ConvertToPrecScale(SqlDecimal.Parse(ToString()), 38, 19);

        public static ReadOrder Parse(string value, bool bypassScaleTest = false)
        {
            var parts = value.Split(".");
            Assert.Argument.Assert(parts.Length == 2);
            var order = parts[0];
            var offset = parts[1];
            if(order[0] == '-') throw new ArgumentException("We do not use negative numbers");
            if(offset[0] == '-') throw new ArgumentException("We do not use negative numbers");

            if(!bypassScaleTest)
            {
                if(offset.Length != 19) throw new ArgumentException($"Got number with {offset.Length} decimal numbers. It must be exactly 19", nameof(value));
            }

            return new ReadOrder(Int64.Parse(order, CultureInfo.InvariantCulture), Int64.Parse(offset, CultureInfo.InvariantCulture));
        }

        public static ReadOrder FromSqlDecimal(SqlDecimal value) => Parse(value.ToString());

        public static ReadOrder[] CreateOrdersForEventsBetween(int numberOfEvents, ReadOrder rangeStart, ReadOrder rangeEnd)
        {
            if(rangeEnd.Order - rangeStart.Order > 1)  throw new ArgumentException("We should only ever insert between two adjacent events.");

            long rangeSize;
            if(rangeEnd.Order > rangeStart.Order)
            {
                rangeSize = Int64.MaxValue - rangeStart.OffSet;
            } else
            {
                rangeSize = rangeEnd.OffSet - rangeStart.OffSet;
            }

            var increment = rangeSize / (numberOfEvents + 1);
            if(increment < 1)
                throw new InvalidOperationException("Unable to fit events");

            var result = 1.Through(numberOfEvents).Select(index => new ReadOrder(rangeStart.Order, rangeStart.OffSet + index * increment)).ToArray();

            Assert.Result.Assert(result[0] > rangeStart);
            Assert.Result.Assert(result[^1] < rangeEnd);
            return result;
        }

        public int CompareTo(ReadOrder other)
        {
            var orderComparison = Order.CompareTo(other.Order);
            if(orderComparison != 0) return orderComparison;
            return OffSet.CompareTo(other.OffSet);
        }

        public static bool operator <(ReadOrder left, ReadOrder right) => left.CompareTo(right) < 0;
        public static bool operator >(ReadOrder left, ReadOrder right) => left.CompareTo(right) > 0;
    }
}
