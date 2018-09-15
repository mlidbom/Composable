﻿using System;
using System.Collections.Generic;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

namespace AccountManagement.UI.QueryModels
{
    class AccountQueryModel : SelfGeneratingQueryModel<AccountQueryModel, AccountEvent.Root>, IAccountResourceData
    {
        public Password Password { get; private set; }
        public Email Email { get; private set; }

        AccountQueryModel(IEnumerable<AccountEvent.Root> events)
        {
            RegisterEventAppliers()
               .For<AccountEvent.PropertyUpdated.Email>(@event => Email = @event.Email)
               .For<AccountEvent.PropertyUpdated.Password>(@event => Password = @event.Password)
               .IgnoreUnhandled<AccountEvent.LoginAttempted>();

            LoadFromHistory(events);
        }

        // ReSharper disable MemberCanBeMadeStatic.Global fluent composable APIs and statics do not mix
        internal class Api
        {
            internal Query Queries => new Query();
            internal class Query
            {
                public BusApi.StrictlyLocal.Queries.EntityLink<AccountQueryModel> Get(Guid id) => new BusApi.StrictlyLocal.Queries.EntityLink<AccountQueryModel>(id);
            }

            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => Get(registrar);

            static void Get(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (BusApi.StrictlyLocal.Queries.EntityLink<AccountQueryModel> query, ILocalApiNavigatorSession bus) =>
                    new AccountQueryModel(bus.Execute(new EventStoreApi().Queries.GetHistory<AccountEvent.Root>(query.EntityId))));
        }
    }
}
