using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events.With_renaming_attribute
{
    [TestFixture]
    public class By_name
    {
        RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                eventTypes: Seq.OfTypes<Event1, Event2>(),
                renamers: new RenameEventsBasedOnEventRenamedAttributes());
        }

        [Test]
        public void Event_type_maps_to_event_name_with_name_replaced_but_path_retained()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldFullName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldFullName);
        }

        [Test]
        public void Event_name_with_name_replaced_but_path_retained_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldFullName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldFullName).Should().Be(typeof(Event2));
        }
    }

    [EventRenamedFrom(Name = OldName)] [TypeId("A06FF121-825D-452F-AB39-C9F18BBAECBC")]class Event1 : DomainEvent
    {
        const string OldName = "Even1OldName";
        public static readonly string OldFullName = $"{typeof(Event1).Namespace}.{OldName}";
    }

    [EventRenamedFrom(Name = OldName)] [TypeId("53068F60-B4FF-4BA4-9663-2B23A5ED2F92")]class Event2 : DomainEvent
    {
        const string OldName = "Event2OldName";
        public static readonly string OldFullName = $"{typeof(Event1).Namespace}.{OldName}";
    }
}
