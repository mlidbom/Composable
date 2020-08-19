using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

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
                                                            new AtMostOnceCommandDefaultConstructorMustNotSetADeduplicationId(),
                                                            new WrapperEventInterfaceMustBeGenericAndDeclareTypeParameterAsAsOutParameter()
                                                        };

        static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
        static HashSet<Type> _successfullyInspectedTypes = new HashSet<Type>();


        internal static void AssertValidForSubscription(Type type)
        {
            if(!type.Is<MessageTypes.IEvent>()) throw new Exception($"You can only subscribe to subtypes of {typeof(MessageTypes.IEvent).GetFullNameCompilable()}");
            if(!type.IsInterface) throw new Exception($"{type.GetFullNameCompilable()} is not an interface. You can only subscribe to event interfaces because as soon as you subscribe to classes you loose the guarantees of semantic routing since classes do not support multiple inheritance.");;
            AssertValid(type);
        }

        internal static void AssertValid(Type type)
        {
            if(_successfullyInspectedTypes.Contains(type)) return;

            Monitor.Update(() =>
            {
                if(_successfullyInspectedTypes.Contains(type)) return;

                foreach(var rule in Rules)
                {
                    rule.AssertFulfilledBy(type);
                }

                _successfullyInspectedTypes = new HashSet<Type>(_successfullyInspectedTypes) {type};
            });
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
            protected override bool IsInvalid(Type type) => !type.Implements<MessageTypes.IMessage>();
            protected override string CreateMessage(Type type) => $"{type.GetFullNameCompilable()} does not implement {typeof(MessageTypes.IMessage).GetFullNameCompilable()}";
        }

        class MutuallyExclusiveInterfaces<TInterface1, TInterface2> : SimpleMessageTypeDesignRule
        {
            protected override bool IsInvalid(Type type) => typeof(TInterface1).IsAssignableFrom(type) && typeof(TInterface2).IsAssignableFrom(type);
            protected override string CreateMessage(Type type) => $"{type.GetFullNameCompilable()} implements both {typeof(TInterface1).GetFullNameCompilable()} and {typeof(TInterface2).GetFullNameCompilable()}";
        }

        class CannotBeBothCommandAndEvent : MutuallyExclusiveInterfaces<MessageTypes.ICommand, MessageTypes.IEvent> {}

        class CannotBeBothCommandAndQuery : MutuallyExclusiveInterfaces<MessageTypes.ICommand, MessageTypes.IQuery<object>> {}

        class CannotBeBothEventAndQuery : MutuallyExclusiveInterfaces<MessageTypes.IEvent, MessageTypes.IQuery<object>> {}

        class CannotBeBothRemotableAndStrictlyLocal : MutuallyExclusiveInterfaces<MessageTypes.IRemotableMessage, MessageTypes.IStrictlyLocalMessage> {}

        class CannotForbidAndRequireTransactionalSender :  MutuallyExclusiveInterfaces<MessageTypes.IMustBeSentTransactionally, MessageTypes.ICannotBeSentRemotelyFromWithinTransaction> {}


        class WrapperEventInterfaceMustBeGenericAndDeclareTypeParameterAsAsOutParameter : SimpleMessageTypeDesignRule
        {
            string _message = "";
            protected override bool IsInvalid(Type type)
            {
                if(type.Is<MessageTypes.IWrapperEvent<MessageTypes.IEvent>>())
                {
                    var allInterfaces = type.GetInterfaces().ToList();
                    if(type.IsInterface) allInterfaces.Add(type);

                    var wrapperInterfacesImplemented = allInterfaces.Where(@interface => @interface.Is<MessageTypes.IWrapperEvent<MessageTypes.IEvent>>()).ToArray();
                    var nonGeneric = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.IsGenericType);
                    if(nonGeneric != null)
                    {
                        _message = $"{nonGeneric.GetFullNameCompilable()} implements {typeof(MessageTypes.IWrapperEvent<>).GetFullNameCompilable()} but is not generic. This means that routing based on the covariance of the wrapping type is impossible and thus semantic routing breaks down.";
                        return true;
                    }

                    var typeParameterIsNotOut = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.GetGenericTypeDefinition().GetGenericArguments()[0].GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant));
                    if(typeParameterIsNotOut != null)
                    {
                        _message = $"{typeParameterIsNotOut.GetFullNameCompilable()} implements {typeof(MessageTypes.IWrapperEvent<>).GetFullNameCompilable()} but does not declare the type parameter as covariant(out). If the type parameter is not covariant routing to derived types does not work because they are not assignable to the base interface type";
                        return true;
                    }
                }

                return false;
            }

            protected override string CreateMessage(Type type) => _message;
        }

        class AtMostOnceCommandDefaultConstructorMustNotSetADeduplicationId : MessageTypeDesignRule
        {
            internal override void AssertFulfilledBy(Type type)
            {
                if(type.Implements<MessageTypes.IAtMostOnceHypermediaCommand>())
                {
                    if(Constructor.HasDefaultConstructor(type))
                    {
                        var instance = (MessageTypes.IAtMostOnceHypermediaCommand)Constructor.CreateInstance(type);
                        if(instance.MessageId != Guid.Empty)
                        {
                            throw new MessageTypeDesignViolationException($@"The default constructor of {type.GetFullNameCompilable()} sets {nameof(MessageTypes.IAtMostOnceMessage)}.{nameof(MessageTypes.IAtMostOnceMessage.MessageId)} to a value other than Guid.Empty.
Since {type.GetFullNameCompilable()} is an {typeof(MessageTypes.IAtMostOnceHypermediaCommand).GetFullNameCompilable()} this is very likely to break the exactly once guarantee.
For instance: If you bind this command in a web UI and forget to bind the {nameof(MessageTypes.IAtMostOnceMessage.MessageId)} then the infrastructure will be unable to realize that this is NOT the correct originally created {nameof(MessageTypes.IAtMostOnceMessage.MessageId)}.
This in turn means that if your user clicks multiple times the command may well be both sent and handled multiple times. Thus breaking the exactly once guarantee. The same thing if a Single Page Application receives an HTTP timeout and retries the command. 
And another example: If you make the setter private many serialization technologies will not be able to maintain the value of the property. But since you used this constructor the property will have a value. A new one each time the instance is deserialized. Again breaking the at most once guarantee.
");
                        }
                    }
                }
            }
        }
    }
}
