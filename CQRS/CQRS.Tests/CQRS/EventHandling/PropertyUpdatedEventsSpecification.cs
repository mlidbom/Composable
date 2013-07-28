using System;
using System.Collections.Generic;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.NewtonSoft;
using Composable.System.Linq;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventHandling
{
    public static class CVPropertyUpdated
    {
        public interface IEmail : PropertyUpdatedEventsSpecification.ICVEvent
        {
            string Email { get; set; }
        }

        public interface IPassword : PropertyUpdatedEventsSpecification.ICVEvent
        {
            string Password { get; set; }
        }

        public interface ISkills : PropertyUpdatedEventsSpecification.ICVEvent
        {
            List<string> AddedSkills { get; set; }
            List<string> RemovedSkills { get; set; }
        }
    }

    public class PropertyUpdatedEventsSpecification : NSpec.NUnit.nspec
    {
        public interface ICVEvent : IAggregateRootEvent {}

        //Property updated events. There should be one per property or collection.
        //For collections they should handle collections. Not Items.

        public interface ICVUpdatedByRecruiter : ICVEvent
        {
            Guid RecruiterId { get; set; }
        }

        public interface ICVSkillsEditedByRecruiter : ICVUpdatedByRecruiter, CVPropertyUpdated.ISkills { }
        
        public interface ICVCreated : ICVEvent, IAggregateRootCreatedEvent {}

        public interface ICVRegistered : ICVCreated, CVPropertyUpdated.IEmail, CVPropertyUpdated.IPassword {}

        public interface ICVSkillsEditedByCandidate : CVPropertyUpdated.ISkills {}        

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

        public class UserQueryModel : ValueObject<UserQueryModel>, IHasPersistentIdentity<Guid>
        {
            public Guid Id { get; private set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public HashSet<string> Skills { get; set; }
        }

        public class UserQueryModelUpdater : SingleAggregateQueryModelUpdater<UserQueryModelUpdater, UserQueryModel, ICVEvent, IDocumentDbSession>
        {
            public UserQueryModelUpdater(IDocumentDbSession session) : base(session)
            {
                RegisterHandlers()
                    .For<CVPropertyUpdated.IEmail>(e => Model.Email = e.Email)
                    .For<CVPropertyUpdated.IPassword>(e => Model.Password = e.Password)
                    .For<CVPropertyUpdated.ISkills>(e =>
                                                   {
                                                       Model.Skills.RemoveWhere(skill => e.RemovedSkills.Contains(skill));
                                                       Model.Skills.AddRange(e.AddedSkills);
                                                   });
            }
        }

        [Test]
        public void TestSerilizationOfCollectionPropertyUpdatedEvents()
        {
            var skillsEdited = new CVSkillsEdited()
                                          {
                                              AddedSkills = new List<string>() { "AddedSkill1", "AddedSkill2", "AddedSkill3" },
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
