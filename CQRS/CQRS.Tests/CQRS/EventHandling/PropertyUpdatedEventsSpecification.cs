using System;
using System.Collections.Generic;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.NewtonSoft;
using Composable.System.Linq;
using CQRS.Tests.CQRS.EventHandling.CVManagement;
using CQRS.Tests.CQRS.EventHandling.CVManagement.GlobalEvents;
using CQRS.Tests.CQRS.EventHandling.CVManagement.InternalEvents.InternalImplementations;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventHandling
{
    namespace CVManagement
    {
        namespace GlobalEvents
        {
            #region Generic events intended to be inherited. None of these should ever be raised. Only inheritors should be raised.
            
            //Every single CV event should inherit this one. Directly or indirectly.
            public interface ICVEvent : IAggregateRootEvent {}

            //Note how this event inherits IAggregateRootCreatedEvent. This allows SingleAggregateQueryModelUpdater and others to automatically know that it is a creation event.
            //Any event that signifies that a CV has been created should inherit this event
            //That way SingleAggregateQueryModelUpdater will not try to read the model from the DB and it can automatically instantiate the model with the correct aggregate root id.
            public interface ICVCreated : ICVEvent, IAggregateRootCreatedEvent {}

            //Note how this event inherits IAggregateRootDeletedEvent. This allows SingleAggregateQueryModelUpdater and others to automatically know that it is a deletion event.
            //That way SingleAggregateQueryModelUpdater will know that it can go right ahead and delete the model.
            public interface ICVDeleted : ICVEvent, IAggregateRootDeletedEvent {}

            //Should be inherited by any event that is triggered by the candidate editing his/her CV
            public interface ICVUpdatedByOwner : ICVEvent {}

            //Should be inherited by any event that is triggered by a recruiter acting on the CV
            public interface ICVUpdatedByRecruiter : ICVEvent
            {
                Guid RecruiterId { get; set; }
            }

            //Property updated events. There should be one per property or collection.            
            //All other events MUST inherit each PropertyUpdated event that applies. They should NOT add properties that are part of the aggregate in any way but by inheriting
            //a PropertyUpdated event.
            namespace PropertyUpdated
            {
                public interface ICVEmailPropertyUpdated : ICVEvent
                {
                    string Email { get; set; }
                }

                public interface ICVPasswordPropertyUpdated : ICVEvent
                {
                    string Password { get; set; }
                }

                //For collections PropertyUpdated events should handle List<T>. Not Items. 
                //They should have both Added* and Removed* properties.
                public interface ICVSkillsPropertyUpdated : ICVEvent
                {
                    List<string> AddedSkills { get; set; }
                    List<string> RemovedSkills { get; set; }
                }
            }

            #endregion

            public interface ICVRegistered : ICVCreated, PropertyUpdated.ICVEmailPropertyUpdated, PropertyUpdated.ICVPasswordPropertyUpdated {}
            public interface ICVSkillsEditedByCandidate : PropertyUpdated.ICVSkillsPropertyUpdated, ICVUpdatedByOwner {}
            public interface ICVSkillsEditedByRecruiter : PropertyUpdated.ICVSkillsPropertyUpdated, ICVUpdatedByRecruiter {}         
        }

        public class UserQueryModel : ValueObject<UserQueryModel>, IHasPersistentIdentity<Guid>
        {
            public Guid Id { get; private set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public HashSet<string> Skills { get; set; }
        }

        public class UserQueryModelUpdater : SingleAggregateQueryModelUpdater<UserQueryModelUpdater, UserQueryModel, ICVEvent, IDocumentDbSession>
        {
            public UserQueryModelUpdater(IDocumentDbSession session)
                : base(session)
            {
                RegisterHandlers()
                    .For<CVManagement.GlobalEvents.PropertyUpdated.ICVEmailPropertyUpdated>(e => Model.Email = e.Email)
                    .For<CVManagement.GlobalEvents.PropertyUpdated.ICVPasswordPropertyUpdated>(e => Model.Password = e.Password)
                    .For<CVManagement.GlobalEvents.PropertyUpdated.ICVSkillsPropertyUpdated>(e =>
                    {
                        Model.Skills.RemoveRange(e.RemovedSkills);
                        Model.Skills.AddRange(e.AddedSkills);
                    });
            }
        }

        namespace InternalEvents
        {
            namespace InternalImplementations
            {
                public class CVRegisteredEvent : AggregateRootEvent, ICVRegistered
                {
                    public string Email { get; set; }
                    public string Password { get; set; }
                }

                public class CVSkillsEdited : AggregateRootEvent, ICVSkillsEditedByCandidate
                {
                    public List<string> AddedSkills { get; set; }
                    public List<string> RemovedSkills { get; set; }
                }
            }
        }
    }

    public class PropertyUpdatedEventsSpecification : NSpec.NUnit.nspec
    {
        [Test]
        public void TestSerilizationOfCollectionPropertyUpdatedEvents()
        {
            var skillsEdited = new CVSkillsEdited()
                               {
                                   AddedSkills = new List<string>() {"AddedSkill1", "AddedSkill2", "AddedSkill3"},
                                   RemovedSkills = new List<string>() {}
                               };

            Console.WriteLine(JsonConvert.SerializeObject(skillsEdited, JsonSettings.JsonSerializerSettings));
        }

        public void starting_from_empty()
        {
            UserQueryModel userQueryModel = null;
            UserQueryModelUpdater userQueryModelUpdater = null;
            Mock<IDocumentDbSession> documentDbSessionMock = null;
            Guid cvId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            before = () =>
                     {
                         userQueryModel = null;
                         documentDbSessionMock = new Mock<IDocumentDbSession>(MockBehavior.Strict);
                         userQueryModelUpdater = new UserQueryModelUpdater(documentDbSessionMock.Object);
                     };

            context["after receiving UserRegisteredAvent"] =
                () =>
                {
                    CVRegisteredEvent registeredEvent = null;
                    before = () =>
                             {
                                 registeredEvent = new CVRegisteredEvent()
                                                   {
                                                       Email = "Eail",
                                                       Password = "Password",
                                                   };
                                 documentDbSessionMock.Setup(session => session.Save(It.IsAny<UserQueryModel>())).Callback<UserQueryModel>(saved => userQueryModel = saved);
                                 userQueryModelUpdater.Handle(registeredEvent);
                             };
                    it["does not crash :)"] = () => Assert.True(true);
                    it["_resultingModel.Id is template.id"] = () => userQueryModel.Id.Should().Be(registeredEvent.AggregateRootId);
                    it["_resultingModel.Email is template.Email"] = () => userQueryModel.Email.Should().Be(registeredEvent.Email);
                    it["_resultingModel.Password is template.Password"] = () => userQueryModel.Password.Should().Be(registeredEvent.Password);
                };
        }
    }
}
