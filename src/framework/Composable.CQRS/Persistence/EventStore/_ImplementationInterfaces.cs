using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using Composable.Contracts;
using Composable.System.Linq;
using Microsoft.Extensions.Options;

namespace Composable.Persistence.EventStore
{
    interface IAggregateTypeValidator
    {
        void AssertIsValid<TAggregate>();
    }

    interface IEventStorePersistenceLayer
    {
        void SetupSchemaIfDatabaseUnInitialized();

        IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<EventDataRow> StreamEvents(int batchSize);
        IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder();
        void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events);
        void DeleteAggregate(Guid aggregateId);
        void UpdateEffectiveVersions(IReadOnlyList<ManualVersionSpecification> versions);

        struct ReadOrder : IComparable<ReadOrder>, IEquatable<ReadOrder>
        {
            public override string ToString() => $"{Order}.{OffSet}";

            public static readonly ReadOrder Zero = new ReadOrder(0, 0);


            public ReadOrder(long order, long offSet)
            {
                if(order < 0) throw new ArgumentException("Must be >= 0");
                if(offSet < 0) throw new ArgumentException("Must be >= 0");
                Order = order;
                OffSet = offSet;
            }

            public long Order { get; }
            public long OffSet { get; }

            public SqlDecimal ToSqlDecimal() => SqlDecimal.ConvertToPrecScale(SqlDecimal.Parse(ToString()), 38, 19);

            public static ReadOrder Parse(string value)
            {
                var parts = value.Split(".");
                Assert.Argument.Assert(parts.Length == 2);
                var order = parts[0];
                var offset = parts[1];
                if(order[0] == '-') throw new ArgumentException("We do not use negative numbers");
                if(offset[0] == '-') throw new ArgumentException("We do not use negative numbers");

                //Urgent: Restore assert
                //if(offset.Length != 19) throw new ArgumentException($"Got number with {offset.Length} decimal numbers. It must be exactly 19", nameof(value));

                return new ReadOrder(long.Parse(order, CultureInfo.InvariantCulture), long.Parse(offset, CultureInfo.InvariantCulture));
            }

            public static ReadOrder FromSqlDecimal(SqlDecimal value) => Parse(value.ToString());

            public static ReadOrder[] CreateOrdersForEventsBetween(int numberOfEvents, ReadOrder rangeStart, ReadOrder rangeEnd)
            {
                if(rangeEnd.Order - rangeStart.Order > 1)  throw new ArgumentException("We should only ever insert between two adjacent events.");

                long rangeSize;
                if(rangeEnd.Order > rangeStart.Order)
                {
                    rangeSize = long.MaxValue - rangeStart.OffSet;
                } else
                {
                    rangeSize = rangeEnd.OffSet - rangeStart.OffSet;
                }

                var increment = rangeSize / (numberOfEvents + 10);
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

            public bool Equals(ReadOrder other) => Order == other.Order && OffSet == other.OffSet;
            public override bool Equals(object? obj) => obj is ReadOrder other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Order, OffSet);

            public static bool operator <(ReadOrder left, ReadOrder right) => left.CompareTo(right) < 0;
            public static bool operator >(ReadOrder left, ReadOrder right) => left.CompareTo(right) > 0;
        }

        class EventNeighborhood
        {
            public ReadOrder EffectiveReadOrder { get; }
            public ReadOrder PreviousEventReadOrder { get; }
            public ReadOrder NextEventReadOrder { get; }

            public SqlDecimal EffectiveReadOrderOld => EffectiveReadOrder.ToSqlDecimal();
            public SqlDecimal PreviousEventReadOrderOld => PreviousEventReadOrder.ToSqlDecimal();
            public SqlDecimal NextEventReadOrderOld => NextEventReadOrder.ToSqlDecimal();


            public EventNeighborhood(ReadOrder effectiveReadOrder, ReadOrder? previousEventReadOrder, ReadOrder? nextEventReadOrder)
            {
                EffectiveReadOrder = effectiveReadOrder;
                NextEventReadOrder = nextEventReadOrder ?? new ReadOrder(EffectiveReadOrder.Order + 1, 0);
                PreviousEventReadOrder = previousEventReadOrder ?? ReadOrder.Zero;
            }

            [Obsolete]
            public EventNeighborhood(SqlDecimal effectiveReadOrder, SqlDecimal previousEventReadOrder, SqlDecimal nextEventReadOrder)
            {
                EffectiveReadOrder = ReadOrder.FromSqlDecimal(effectiveReadOrder);
                NextEventReadOrder = nextEventReadOrder.IsNull ? new ReadOrder(EffectiveReadOrder.Order + 1, 0) : ReadOrder.FromSqlDecimal(nextEventReadOrder);
                PreviousEventReadOrder = previousEventReadOrder.IsNull ? new ReadOrder(0, 0) : ReadOrder.FromSqlDecimal(previousEventReadOrder);
            }
        }
        EventNeighborhood LoadEventNeighborHood(Guid eventId);

        class ManualVersionSpecification
        {
            public ManualVersionSpecification(Guid eventId, int version)
            {
                EventId = eventId;
                EffectiveVersion = version;
            }

            public Guid EventId { get; }
            public int EffectiveVersion { get; }
        }
    }

    class CreationEventRow
    {
        public CreationEventRow(Guid aggregateId, Guid typeId)
        {
            AggregateId = aggregateId;
            TypeId = typeId;
        }
        public Guid AggregateId { get; }
        public Guid TypeId { get; }
    }

    class EventDataRow
    {
        public EventDataRow(IAggregateEvent @event, AggregateEventRefactoringInformation refactoringInformation, Guid eventType, string eventAsJson)
        {
            EventJson = eventAsJson;
            EventType = eventType;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            RefactoringInformation = refactoringInformation;
        }

        public EventDataRow(EventInsertionSpecification specification, Guid typeId, string eventAsJson)
        {
            var @event = specification.Event;
            EventJson = eventAsJson;
            EventType = typeId;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            RefactoringInformation = new AggregateEventRefactoringInformation()
                                     {
                                         InsertedVersion = specification.InsertedVersion,
                                         EffectiveVersion = specification.ManualVersion
                                     };
        }

        public EventDataRow(Guid eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, AggregateEventRefactoringInformation refactoringInformation)
        {
            EventType = eventType;
            EventJson = eventJson;
            EventId = eventId;
            AggregateVersion = aggregateVersion;
            AggregateId = aggregateId;
            UtcTimeStamp = utcTimeStamp;

            RefactoringInformation = refactoringInformation;
        }

        public Guid EventType { get; private set; }
        public string EventJson { get; private set; }
        public Guid EventId { get; private set; }
        public int AggregateVersion { get; private set; }

        public Guid AggregateId { get; private set; }
        public DateTime UtcTimeStamp { get; private set; }

        internal AggregateEventRefactoringInformation RefactoringInformation { get; private set; }

        public override string ToString() => $"{nameof(RefactoringInformation.InsertedVersion)}{RefactoringInformation.InsertedVersion},{nameof(RefactoringInformation.EffectiveVersion)}{RefactoringInformation.EffectiveVersion}, {nameof(RefactoringInformation.EffectiveOrder)}{RefactoringInformation.EffectiveOrder}";
    }

    //Urgent: Refactor into enum Replace,InsertBefore,InsertAfter + RefactoredEventId + make all properties non-nullable instead make the whole instance on the event nullable + move data that is on all events elsewhere + split that elsewhere between read and write so that effective order is not nullable when reading and not present when writing. 
    class AggregateEventRefactoringInformation
    {
        internal SqlDecimal? EffectiveOrder { get; set; }
        internal int InsertedVersion { get; set; }

        //urgent: See if this cannot be non-nullable.
        internal int? EffectiveVersion { get; set; }
        internal Guid? Replaces { get; set; }
        internal Guid? InsertBefore { get; set; }
        internal Guid? InsertAfter { get; set; }
    }

    class EventInsertionSpecification
    {
        public EventInsertionSpecification(IAggregateEvent @event) : this(@event, @event.AggregateVersion, null) {}

        public EventInsertionSpecification(IAggregateEvent @event, int insertedVersion, int? manualVersion)
        {
            Event = @event;
            InsertedVersion = insertedVersion;
            ManualVersion = manualVersion;
        }

        internal IAggregateEvent Event { get; }
        internal int InsertedVersion { get; }
        internal int? ManualVersion { get; }
    }
}
