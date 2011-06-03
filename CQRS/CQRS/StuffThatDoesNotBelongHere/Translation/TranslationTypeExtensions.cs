using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Composable.StuffThatDoesNotBelongHere.Translation
{
    public static class TranslationTypeExtensions
    {
        public static bool HasTranslatableProperties(this Type me)
        {
            return me.GetTranslatedProperties().Any();
        }


        private static readonly ConcurrentDictionary<Type, IEnumerable<TranslatedProperty>> TranslatedPropertiesCache = new ConcurrentDictionary<Type, IEnumerable<TranslatedProperty>>();



        /// <summary>
        /// Returns the translated properties for a type, The key in the dictionary is the name of the property, and the value is the key that should be used for the translation.
        /// </summary>
        public static IEnumerable<TranslatedProperty> GetTranslatedProperties(this Type me)
        {
            IEnumerable<TranslatedProperty> result;
            if (TranslatedPropertiesCache.TryGetValue(me, out result))
                return result;

            result = me.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).ToList()
                                 .Select(p => new { p, attr = p.GetCustomAttributes(typeof(TranslateAttribute), true).Cast<TranslateAttribute>().SingleOrDefault() })
                                 .Where( p => p.attr != null)
                                 .Select(x => new TranslatedProperty(x.attr, x.p))
                                 .ToList();


            TranslatedPropertiesCache.TryAdd(me, result);

            return result;
        }

    }

    public class TranslatedProperty
    {
        private readonly PropertyInfo _propertyInfo;

        public TranslatedProperty(TranslateAttribute attr, PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
            if(propertyInfo.PropertyType != typeof(string))
            {
                throw new InvalidOperationException("The property " + propertyInfo.DeclaringType.FullName + "." + propertyInfo.Name + " can not be translated because it does not have type string.");
            }
            Name = _propertyInfo.Name;
        }

        public string Name { get; set; }
        public void SetValue(object instance, string value)
        {
            _propertyInfo.GetSetMethod(true).Invoke(instance, new[]{ value});
        }
    }
}