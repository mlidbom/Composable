using System;
using Composable.Persistence.EventStore;

// ReSharper disable InconsistentNaming

namespace _01
{
    // tag::recruitment-birds-eye-view[]
    interface IUserActionEvent {}
    interface IJobSeekerActed : IUserActionEvent {} //<1>
    interface IRecruiterActed : IUserActionEvent {}

    interface IRecruitmentEvent : IAggregateEvent {} //<2>

    interface IRecruitmentRegistered : IRecruitmentEvent,        //<3>
                                       IRecruiterActed,          //<4>
                                       IAggregateCreatedEvent {} //<5>

    interface IApplicationEvent : IRecruitmentEvent {} //<6>
    interface IApplicationCreated : IApplicationEvent {}

    interface IJobSeekerApplied : IRecruitmentEvent,   // <7>
                                  IApplicationCreated, // <8>
                                  IJobSeekerActed {}   // <9>
    // end::recruitment-birds-eye-view[]
}

namespace _02
{
    // tag::02[]
    static partial class AccountEvent // <1>
    {
        interface RootEvent : IAggregateEvent {}                    //<2> <3>
        interface Registered : RootEvent, IAggregateCreatedEvent {} // <4>
    }
    // end::02[]
}

namespace _03
{
    // tag::03[]
    static partial class AccountEvent // <1>
    {
        interface RootEvent : IAggregateEvent {}                    //<2> <3>
        interface Registered : RootEvent, IAggregateCreatedEvent {} // <4>
    }
    // end::03[]
}
