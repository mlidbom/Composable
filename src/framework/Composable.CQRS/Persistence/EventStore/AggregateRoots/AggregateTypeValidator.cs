using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.AggregateRoots
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class AllowPublicSettersAttribute : Attribute
    {
    }

    static class AggregateTypeValidator<TDomainClass, TEventClass, TEventInterface>
    {
        public static void Validate()
        {
            List<Type> typesToInspect = Seq.OfTypes<TDomainClass, TEventClass, TEventInterface>().ToList();

            typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TDomainClass)));
            typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TEventClass)));
            typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TEventInterface)));

            typesToInspect = typesToInspect.Distinct().ToList();

            var illegalTypes = typesToInspect.Where(HasIllegalPublicSettersOrFields).ToList();

            if (illegalTypes.Any())
            {
                var brokenTypeListMessage = illegalTypes.Select(illegal => illegal.FullName).Join(Environment.NewLine);
                var message = $"Types used by aggregate contains types that have public setters or public  fields. This is a bad domain design. List of problem types:{Environment.NewLine}{brokenTypeListMessage}{Environment.NewLine}{Environment.NewLine}";

                throw new Exception(message);
            }
        }

        static bool HasIllegalPublicSettersOrFields(Type type)
        {
            var publicFields = type.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(member => member.MemberType.HasFlag(MemberTypes.Field)).ToList();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var publicProperties = properties
                .Where(member => member?.SetMethod?.IsPublic == true)
                .ToList();

            var totalMutableProperties = publicFields.Concat(publicProperties).ToList();
            // ReSharper disable once AssignNullToNotNullAttribute
            totalMutableProperties = totalMutableProperties.Where(member => member.DeclaringType.GetCustomAttribute<AllowPublicSettersAttribute>() == null).ToList();

            return totalMutableProperties.Any();
        }


        static IReadOnlyList<Type> GetAllInheritingClassesOrInterfaces(Type type) => type.Assembly.GetTypes()
                                                                                       .Where(type.IsAssignableFrom)
                                                                                       .ToList();
    }
}