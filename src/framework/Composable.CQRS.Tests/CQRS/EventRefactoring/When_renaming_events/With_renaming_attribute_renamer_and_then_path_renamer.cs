using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.When_renaming_events
{
    [TestFixture] public class With_renaming_attribute_renamer_and_then_path_renamer
    {
        const string OldPath = "Some.Old.Path.";

        RenamingEventNameMapper _nameMapper;

        [SetUp]
        public void SetupMappingsForEventsWithNoRenamingAttribute()
        {
            _nameMapper = new RenamingEventNameMapper(
                Seq.OfTypes<Event1, Event2>(),
                new RenameEventsBasedOnEventRenamedAttributes(),
                new EventPathRenamer(
                    oldPath: OldPath,
                    eventAtNewPath: typeof(Event1)
                    ));
        }

        [Test]
        public void Event_type_maps_to_event_name_with_path_replaced_by_path_replacer_and_name_replaced_by_attribute_name()
        {
            _nameMapper.GetName(typeof(Event1)).Should().Be(Event1.OldName);
            _nameMapper.GetName(typeof(Event2)).Should().Be(Event2.OldName);
        }

        [Test]
        public void Event_name_with_path_replaced_by_path_replacer_and_name_replaced_by_attribute_name_maps_to_event_type()
        {
            _nameMapper.GetType(Event1.OldName).Should().Be(typeof(Event1));
            _nameMapper.GetType(Event2.OldName).Should().Be(typeof(Event2));
        }

        [TypeId("7CDC3EB4-4778-4C0F-AC5C-7EB7A53019E3")][EventRenamedFrom(Name = "OldEvent1")] class Event1 : AggregateRootEvent
        {
            public const string OldName = OldPath + "OldEvent1";
        }

        [TypeId("0EB6D01B-D780-45B2-8AD9-C2DCC190A916")][EventRenamedFrom(Name = "OldEvent2")] class Event2 : AggregateRootEvent
        {
            public const string OldName = OldPath + "OldEvent2";
        }
    }
}