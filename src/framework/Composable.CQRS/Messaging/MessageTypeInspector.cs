using System;
using System.Collections.Generic;
using Composable.System.Reflection;

namespace Composable.Messaging
{
    partial class MessageTypeInspector
    {
        static readonly MessageTypeDesignRule[] Rules = {
                                                            new MustBeIMessage(),
                                                            new CannotBeBothCommandAndEvent(),
                                                            new CannotBeBothCommandAndQuery(),
                                                            new CannotBeBothEventAndQuery(),
                                                            new CannotBeBothRemotableAndStrictlyLocal(),
                                                            new CannotForbidAndRequireTransactionalSender(),
                                                            new ConcreteQueryMustImplementGenericQueryInterface(),
                                                            new AtMostOnceCommandDefaultConstructorMustNotSetADeduplicationId()
                                                        };

        static readonly object Lock = new object();
        static HashSet<Type> _successfullyInspectedTypes = new HashSet<Type>();
        internal static void AssertValid(Type type)
        {
            if(_successfullyInspectedTypes.Contains(type)) return;

            lock(Lock)
            {
                if(_successfullyInspectedTypes.Contains(type)) return;

                foreach(var rule in Rules)
                {
                    rule.AssertFulfilledBy(type);
                }

                _successfullyInspectedTypes = new HashSet<Type>(_successfullyInspectedTypes) {type};
            }
        }

        abstract class MessageTypeDesignRule
        {
            internal abstract void AssertFulfilledBy(Type type);
        }

        abstract class SimpleMessageTypeDesignRule : MessageTypeDesignRule
        {
            protected abstract bool IsInvalid(Type type);
            protected abstract string CreateMessage(Type type);

            internal override void AssertFulfilledBy(Type type)
            {
                if(IsInvalid(type))
                {
                    throw new MessageTypeDesignViolationException(CreateMessage(type));
                }
            }
        }

        class MustBeIMessage : SimpleMessageTypeDesignRule
        {
            protected override bool IsInvalid(Type type) => !type.Implements<BusApi.IMessage>();
            protected override string CreateMessage(Type type) => $"{type.GetFullNameCompilable()} does not implement {typeof(BusApi.IMessage).GetFullNameCompilable()}";
        }

        class MutuallyExclusiveInterfaces<TInterface1, TInterface2> : SimpleMessageTypeDesignRule
        {
            protected override bool IsInvalid(Type type) => type.Implements<TInterface1>() && type.Implements<TInterface2>();
            protected override string CreateMessage(Type type) => $"{type.GetFullNameCompilable()} implements both {typeof(TInterface1).GetFullNameCompilable()} and {typeof(TInterface2).GetFullNameCompilable()}";
        }

        class CannotBeBothCommandAndEvent : MutuallyExclusiveInterfaces<BusApi.ICommand, BusApi.IEvent> {}

        class CannotBeBothCommandAndQuery : MutuallyExclusiveInterfaces<BusApi.ICommand, BusApi.IQuery> {}

        class CannotBeBothEventAndQuery : MutuallyExclusiveInterfaces<BusApi.IEvent, BusApi.IQuery> {}

        class CannotBeBothRemotableAndStrictlyLocal : MutuallyExclusiveInterfaces<BusApi.Remotable.IMessage, BusApi.StrictlyLocal.IMessage> {}

        class CannotForbidAndRequireTransactionalSender :  MutuallyExclusiveInterfaces<BusApi.IRequireTransactionalSender, BusApi.IForbidTransactionalRemoteSender> {}


        class ConcreteQueryMustImplementGenericQueryInterface : MessageTypeDesignRule
        {
            internal override void AssertFulfilledBy(Type type)
            {
                if(type.Implements<BusApi.IQuery>() && !type.IsAbstract && !type.Implements(typeof(BusApi.IQuery<>)))
                {
                    throw new MessageTypeDesignViolationException($"{type.GetFullNameCompilable()} implements only: {typeof(BusApi.IQuery).GetFullNameCompilable()}. Concrete classes must implement {typeof(BusApi.IQuery<>).GetFullNameCompilable()}");

                }
            }
        }

        class AtMostOnceCommandDefaultConstructorMustNotSetADeduplicationId : MessageTypeDesignRule
        {
            internal override void AssertFulfilledBy(Type type)
            {
                if(type.Implements<BusApi.Remotable.AtMostOnce.ICommand>())
                {
                    var instance = (BusApi.Remotable.AtMostOnce.ICommand)Constructor.CreateInstance(type);
                    if(instance.DeduplicationId != Guid.Empty)
                    {
                        throw new MessageTypeDesignViolationException($@"The default constructor of {type.GetFullNameCompilable()} sets {nameof(BusApi.Remotable.IAtMostOnceMessage)}.{nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)} to a value other than Guid.Empty.
Since {type.GetFullNameCompilable()} is an {typeof(BusApi.Remotable.AtMostOnce.ICommand).GetFullNameCompilable()} this is very likely to break the exactly once guarantee.
For instance: If you bind this command in a web UI and forget to bind the {nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)} then the infrastructure will be unable to realize that this is NOT the correct originally created {nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)}.
This in turn means that if your user clicks multiple times the command may well be both sent and handled multiple times. Thus breaking the exactly once guarantee. The same thing if a Single Page Application receives an HTTP timeout and retries the command. 
And another example: If you make the setter private many serialization technologies will not be able to maintain the value of the property. But since you used this constructor the property will have a value. A new one each time the instance is deserialized. Again breaking the at most once guarantee.
");
                    }
                }
            }
        }
    }
}
