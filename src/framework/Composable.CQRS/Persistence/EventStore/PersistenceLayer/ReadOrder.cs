using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    readonly struct ReadOrder : IComparable<ReadOrder>, IEquatable<ReadOrder>
    {
        readonly BigInteger _order;
        readonly BigInteger _offSet;

        public override string ToString() => $"{_order}.{_offSet:D19}";

        public static readonly ReadOrder Zero = new ReadOrder(0, 0);

        internal static ReadOrder FromLong(long readOrder) => new ReadOrder(readOrder, 0);

        static readonly BigInteger MaxOffset = BigInteger.Parse("1".PadRight(20, '0'), CultureInfo.InvariantCulture);

        ReadOrder(BigInteger order, BigInteger offSet)
        {
            if(order < 0) throw new ArgumentException($"{nameof(order)} Must be >= 0");
            if(offSet < 0) throw new ArgumentException($"{nameof(offSet)} Must be >= 0");

            _order = order;
            _offSet = offSet;
        }

        public SqlDecimal ToSqlDecimal() => ToCorrectPrecisionAndScale(SqlDecimal.Parse(ToString()));

        public ReadOrder NextIntegerOrder => new ReadOrder(_order + 1, 0);

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

            return new ReadOrder(BigInteger.Parse(order, CultureInfo.InvariantCulture), BigInteger.Parse(offset.PadRight(19, '0'), CultureInfo.InvariantCulture));
        }

        public static ReadOrder FromSqlDecimal(SqlDecimal value) => Parse(value.ToString());

        public static ReadOrder[] CreateOrdersForEventsBetween(int numberOfEvents, ReadOrder rangeStart, ReadOrder rangeEnd)
        {
            if(rangeEnd._order - rangeStart._order > 1)  throw new ArgumentException("We should only ever insert between two adjacent events.");

            BigInteger rangeSize;
            if(rangeEnd._order > rangeStart._order)
            {
                rangeSize = (MaxOffset + rangeEnd._offSet) - rangeStart._offSet; //We are allowed to overflow onto the next Order value
            } else
            {
                rangeSize = rangeEnd._offSet - rangeStart._offSet;
            }

            var increment = rangeSize / (numberOfEvents + 1);
            if(increment < 1)
                throw new ArgumentException("Range too small to fit events.");

            var offSetsFromStartRange = 1.Through(numberOfEvents).Select(index => rangeStart._offSet + index * increment).ToArray();
            var result = offSetsFromStartRange.Select(offset =>
            {
                if(offset < MaxOffset) //We are still between the range start and the next integer Order value
                {
                    return new ReadOrder(rangeStart._order, offset);
                } else //Offset has overflowed to the next Order value
                {
                    var order = rangeStart._order + 1;
                    offset = offset - MaxOffset;
                    return new ReadOrder(order, offset);
                }
            }).ToArray();

            Assert.Result.Assert(result.All(order => order > rangeStart)); //We are staying within the specified range
            Assert.Result.Assert(result.All(order => order < rangeEnd)); //We are staying within the specified range
            Assert.Result.Assert(result.Distinct().Count() == numberOfEvents); //Each ReadOrder is unique

            return result;
        }

        static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, 38, 19);

        public bool Equals(ReadOrder other) => _order.Equals(other._order) && _offSet.Equals(other._offSet);
        public override bool Equals(object? obj) => obj is ReadOrder other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_order, _offSet);
        public static bool operator ==(ReadOrder left, ReadOrder right) => left.Equals(right);
        public static bool operator !=(ReadOrder left, ReadOrder right) => !left.Equals(right);

        public int CompareTo(ReadOrder other)
        {
            var orderComparison = _order.CompareTo(other._order);
            if(orderComparison != 0) return orderComparison;
            return _offSet.CompareTo(other._offSet);
        }

        public static bool operator <(ReadOrder left, ReadOrder right) => left.CompareTo(right) < 0;
        public static bool operator >(ReadOrder left, ReadOrder right) => left.CompareTo(right) > 0;
    }
}
