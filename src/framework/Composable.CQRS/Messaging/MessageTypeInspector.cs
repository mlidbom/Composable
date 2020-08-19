using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Messaging
{
    partial class MessageTypeInspector
    {
        static readonly MessageTypeDesignRule[] MessageTypeDesignRules =
        {
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

        static IReadonlySetCEx<Type> _successfullyInspectedSubscribableTypes = new HashSetCEx<Type>();
        internal static void AssertValidForSubscription(Type type)
        {
            if(_successfullyInspectedSubscribableTypes.Contains(type)) return;

            Monitor.Update(() =>
            {
                if(!type.Is<IEvent>()) throw new Exception($"You can only subscribe to subtypes of {typeof(IEvent).GetFullNameCompilable()}");
                if(!type.IsInterface) throw new Exception($"{type.GetFullNameCompilable()} is not an interface. You can only subscribe to event interfaces because as soon as you subscribe to classes you loose the guarantees of semantic routing since classes do not support multiple inheritance.");
                AssertTypeIsValidInternal(type);
                ThreadSafe.AddToCopyAndReplace(ref _successfullyInspectedSubscribableTypes, type);
            });
        }

        static IReadonlySetCEx<Type> _successfullyInspectedTypes = new HashSetCEx<Type>();
        internal static void AssertValid(Type type)
        {
            if(_successfullyInspectedTypes.Contains(type)) return;

            Monitor.Update(() =>
            {
                if(_successfullyInspectedTypes.Contains(type)) return;

                AssertTypeIsValidInternal(type);

                ThreadSafe.AddToCopyAndReplace(ref _successfullyInspectedTypes, type);
            });
        }

        static void AssertTypeIsValidInternal(Type type) =>
            MessageTypeDesignRules.ForEach(rule => rule.AssertFulfilledBy(type));

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
            protected override bool IsInvalid(Type type) => !type.Implements<IMessage>();
            protected override string CreateMessage(Type type) => $"{type.GetFullNameCompilable()} does not implement {typeof(IMessage).GetFullNameCompilable()}";
        }

        class MutuallyExclusiveInterfaces<TInterface1, TInterface2> : SimpleMessageTypeDesignRule
        {
            protected override bool IsInvalid(Type type) => typeof(TInterface1).IsAssignableFrom(type) && typeof(TInterface2).IsAssignableFrom(type);
            protected override string CreateMessage(Type type) => $"{type.GetFullNameCompilable()} implements both {typeof(TInterface1).GetFullNameCompilable()} and {typeof(TInterface2).GetFullNameCompilable()}";
        }

        class CannotBeBothCommandAndEvent : MutuallyExclusiveInterfaces<ICommand, IEvent> {}

        class CannotBeBothCommandAndQuery : MutuallyExclusiveInterfaces<ICommand, IQuery<object>> {}

        class CannotBeBothEventAndQuery : MutuallyExclusiveInterfaces<IEvent, IQuery<object>> {}

        class CannotBeBothRemotableAndStrictlyLocal : MutuallyExclusiveInterfaces<IRemotableMessage, IStrictlyLocalMessage> {}

        class CannotForbidAndRequireTransactionalSender : MutuallyExclusiveInterfaces<IMustBeSentTransactionally, ICannotBeSentRemotelyFromWithinTransaction> {}

        class WrapperEventInterfaceMustBeGenericAndDeclareTypeParameterAsAsOutParameter : MessageTypeDesignRule
        {
            internal override void AssertFulfilledBy(Type type)
            {
                if(type.Is<IWrapperEvent<IEvent>>())
                {
                    var allInterfaces = type.GetInterfaces().ToList();
                    if(type.IsInterface) allInterfaces.Add(type);

                    var wrapperInterfacesImplemented = allInterfaces.Where(@interface => @interface.Is<IWrapperEvent<IEvent>>()).ToArray();
                    var nonGeneric = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.IsGenericType);
                    if(nonGeneric != null) throw new MessageTypeDesignViolationException($"{nonGeneric.GetFullNameCompilable()} implements {typeof(IWrapperEvent<>).GetFullNameCompilable()} but is not generic. This means that routing based on the covariance of the wrapping type is impossible and thus semantic routing breaks down.");

                    var typeParameterIsNotOut = wrapperInterfacesImplemented.FirstOrDefault(@interface => !@interface.GetGenericTypeDefinition().GetGenericArguments()[0].GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant));
                    if(typeParameterIsNotOut != null) throw new MessageTypeDesignViolationException($"{typeParameterIsNotOut.GetFullNameCompilable()} implements {typeof(IWrapperEvent<>).GetFullNameCompilable()} but does not declare the type parameter as covariant(out). If the type parameter is not covariant routing to derived types does not work because they are not assignable to the base interface type");
                }
            }
        }

        class AtMostOnceCommandDefaultConstructorMustNotSetADeduplicationId : MessageTypeDesignRule
        {
            internal override void AssertFulfilledBy(Type type)
            {
                if(type.Implements<IAtMostOnceHypermediaCommand>())
                {
                    if(Constructor.HasDefaultConstructor(type))
                    {
                        var instance = (IAtMostOnceHypermediaCommand)Constructor.CreateInstance(type);
                        if(instance.MessageId != Guid.Empty)
                        {
                            throw new MessageTypeDesignViolationException($@"The default constructor of {type.GetFullNameCompilable()} sets {nameof(IAtMostOnceMessage)}.{nameof(IAtMostOnceMessage.MessageId)} to a value other than Guid.Empty.
Since {type.GetFullNameCompilable()} is an {typeof(IAtMostOnceHypermediaCommand).GetFullNameCompilable()} this is very likely to break the exactly once guarantee.
For instance: If you bind this command in a web UI and forget to bind the {nameof(IAtMostOnceMessage.MessageId)} then the infrastructure will be unable to realize that this is NOT the correct originally created {nameof(IAtMostOnceMessage.MessageId)}.
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
