using System;
using System.Collections.Generic;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Hypermedia;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

namespace AccountManagement.UI.QueryModels
{
    class AccountQueryModel : SelfGeneratingQueryModel<AccountQueryModel, AccountEvent.Root>, IAccountResourceData
    {
        public Password Password { get; private set; } = null!; //Nullable status guaranteed by AssertInvariantsAreMet
        public Email Email { get; private set; } = null!; //Nullable status guaranteed by AssertInvariantsAreMet

        AccountQueryModel(IEnumerable<AccountEvent.Root> events)
        {
            RegisterEventAppliers()
               .For<AccountEvent.PropertyUpdated.Email>(@event => Email = @event.Email)
               .For<AccountEvent.PropertyUpdated.Password>(@event => Password = @event.Password)
               .IgnoreUnhandled<AccountEvent.LoginAttempted>();

            LoadFromHistory(events);
        }

        protected override void AssertInvariantsAreMet() => Contract.Invariant(() => Email, () => Password, () => Id).NotNullOrDefault();

        // ReSharper disable MemberCanBeMadeStatic.Global fluent composable APIs and statics do not mix
        internal class Api
        {
            internal Query Queries => new Query();
            internal class Query
            {
                public MessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> Get(Guid id) => new MessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel>(id);
            }

            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => Get(registrar);

            static void Get(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (MessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> query, ILocalHypermediaNavigator navigator) =>
                    new AccountQueryModel(navigator.Execute(new EventStoreApi().Queries.GetHistory<AccountEvent.Root>(query.EntityId))));
        }
    }
}
