using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Composable.Persistence.EventStore;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ReflectionCE.EmitCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public static partial class MessageTypes
    {
        public static class WrapperEvent
        {
            static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();

            static class WrapperConstructorCache<TWrapperEvent, TWrappedEvent>
                where TWrapperEvent : IWrapperEvent<TWrappedEvent>
                where TWrappedEvent : IEvent
            {
                static readonly Func<IEvent, IWrapperEvent<IEvent>> UntypedConstructor = Monitor.Update(() => CreateConstructorFor(typeof(TWrappedEvent)));

                internal static readonly Func<TWrappedEvent, IWrapperEvent<TWrappedEvent>> Constructor = @event => (IWrapperEvent<TWrappedEvent>)UntypedConstructor(@event);
            }

            public static TWrapperEvent WrapEvent<TWrapperEvent, TWrappedEvent>(TWrappedEvent theEvent)
                where TWrapperEvent : IWrapperEvent<TWrappedEvent>
                where TWrappedEvent : IEvent =>
                (TWrapperEvent)WrapperConstructorCache<TWrapperEvent, TWrappedEvent>.Constructor(theEvent);

            public static IWrapperEvent<TWrappedEvent> WrapEvent<TWrappedEvent>(TWrappedEvent theEvent) where TWrappedEvent : IEvent =>
                WrapperConstructorCache<IWrapperEvent<TWrappedEvent>, TWrappedEvent>.Constructor(theEvent);

            static IReadOnlyDictionary<Type, Func<IEvent, IWrapperEvent<IEvent>>> _wrapperConstructors = new Dictionary<Type, Func<IEvent, IWrapperEvent<IEvent>>>();
            public static IWrapperEvent<IEvent> WrapEvent(IEvent theEvent) =>
                WrapperConstructorFor(theEvent.GetType()).Invoke(theEvent);

            public static Func<IEvent, IWrapperEvent<IEvent>> WrapperConstructorFor(Type wrappedEventType)
            {
                if(_wrapperConstructors.TryGetValue(wrappedEventType, out var constructor))
                {
                    return constructor;
                }

                Monitor.Update(() =>
                {
                    constructor = CreateConstructorFor(wrappedEventType);
                    ThreadSafe.AddToCopyAndReplace(ref _wrapperConstructors, wrappedEventType, constructor);
                });

                return constructor!;
            }

            static Func<IEvent, IWrapperEvent<IEvent>> CreateConstructorFor(Type wrappedEventType)
            {
                var openWrapperEventType = typeof(IWrapperEvent<>);
                var closedWrapperEventType = openWrapperEventType.MakeGenericType(wrappedEventType);

                var openWrapperImplementationType = CreateGenericWrapperEventType(openWrapperEventType);
                var closedWrapperImplementationType = openWrapperImplementationType.MakeGenericType(wrappedEventType);

                var constructorArgumentTypes = new[] {wrappedEventType};
                var creatorFunctionArgumentTypes = new[] {typeof(IEvent)};

                var constructor = closedWrapperImplementationType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
                if(constructor == null)
                {
                    throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {closedWrapperEventType.GetFullNameCompilable()}({DescribeParameterList(constructorArgumentTypes)})");
                }

                var constructorCallMethod = new DynamicMethod(name: $"Generated_constructor_for_{closedWrapperEventType.Name}", returnType: closedWrapperEventType, parameterTypes: creatorFunctionArgumentTypes, owner: closedWrapperImplementationType);
                var ilGenerator = constructorCallMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Castclass, wrappedEventType);
                ilGenerator.Emit(OpCodes.Newobj, constructor);
                ilGenerator.Emit(OpCodes.Ret);

                return (Func<IEvent, IWrapperEvent<IEvent>>)constructorCallMethod.CreateDelegate(typeof(Func<IEvent, IWrapperEvent<IEvent>>));
            }

            static string DescribeParameterList(IEnumerable<Type> parameterTypes) { return parameterTypes.Select(parameterType => parameterType.FullNameNotNull()).Join(", "); }

            static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();
            static Type CreateGenericWrapperEventType(Type wrapperEventType)
            {
                if(_createdWrapperTypes.TryGetValue(wrapperEventType, out var cachedWrapperImplementation))
                {
                    return cachedWrapperImplementation;
                }

                if(!wrapperEventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperEventType)}");

                if(wrapperEventType != typeof(IWrapperEvent<>)
                && wrapperEventType.GetInterfaces().All(iface => iface != typeof(IWrapperEvent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
                    throw new ArgumentException($"Must implement {typeof(IWrapperEvent<>).FullName}", $"{nameof(wrapperEventType)}");

                var wrappedEventType = wrapperEventType.GetGenericArguments()[0];

                var requiredEventInterface = wrappedEventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(IEvent).IsAssignableFrom(constraint));

                var genericWrapperEventType = AssemblyBuilderCE.Module.Update(module =>
                {
                    TypeBuilder wrapperEventBuilder = module.DefineType(
                        name: $"{wrapperEventType}_ilgen_impl",
                        attr: TypeAttributes.Public,
                        parent: null,
                        interfaces: new[] {wrapperEventType});

                    GenericTypeParameterBuilder wrappedEventTypeParameter = wrapperEventBuilder.DefineGenericParameters("TWrappedEvent")[0];

                    wrappedEventTypeParameter.SetInterfaceConstraints(requiredEventInterface);

                    var (wrappedEventField, _) = wrapperEventBuilder.ImplementProperty(nameof(IWrapperEvent<IAggregateEvent>.Event), wrappedEventTypeParameter);

                    wrapperEventBuilder.ImplementConstructor(wrappedEventField);

                    return wrapperEventBuilder.CreateType().NotNull();
                });

                ThreadSafe.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperEventType, genericWrapperEventType);

                return genericWrapperEventType;
            }
        }

        public class WrapperEvent<TEventInterface> : IWrapperEvent<TEventInterface>
            where TEventInterface : IEvent
        {
            public WrapperEvent(TEventInterface @event) => Event = @event;
            public TEventInterface Event { get; }
        }
    }
}
