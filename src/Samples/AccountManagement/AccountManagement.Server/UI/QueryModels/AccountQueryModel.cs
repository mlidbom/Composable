using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Query.Models.Generators;

namespace AccountManagement.UI.QueryModels
{
    using Composable.Messaging.Events;

    partial class AccountQueryModel : ISingleAggregateQueryModel
    {
        public Password Password { get; private set; }
        public Email Email { get; private set; }
        public Guid Id { get; private set; }

        void ISingleAggregateQueryModel.SetId(Guid id) { Id = id; }
    }

    partial class AccountQueryModel
    {
        /// <summary>Ad hoc creates an <see cref="AccountQueryModel"/> by reading and applying the events from the event store reader</summary>
        internal class Generator : SingleAggregateQueryModelGenerator<Generator, AccountQueryModel, AccountEvent.Root, IEventStoreReader>
        {
            //Note the use of a custom interface. This lets us keep query model generators for different systems apart in the wiring.
            public Generator(IEventStoreReader session) : base(session)
            {
                RegisterHandlers()
                    .For<AccountEvent.PropertyUpdated.Email>(e => Model.Email = e.Email)
                    .For<AccountEvent.PropertyUpdated.Password>(e => Model.Password = e.Password);
            }


            public static void RegisterWith(IDependencyInjectionContainer container)
            {
                container.Register(Component.For<Generator>()
                                            .UsingFactoryMethod((IEventStoreReader session) => new Generator(session))
                                            .LifestyleScoped());
            }
        }
    }
}
