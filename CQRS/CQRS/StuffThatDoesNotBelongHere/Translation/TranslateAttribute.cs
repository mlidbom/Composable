using System;

namespace Composable.StuffThatDoesNotBelongHere.Translation
{
    /// <summary>
    /// Put this attribute on a property of type string to make the <see cref="TranslationInterceptor"/> automatically translate the entity when it is loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TranslateAttribute : Attribute
    {
        /// <summary>
        /// Key to use for the translation. If null, use the property name.
        /// </summary>
        public string Key { get; private set; }

        public TranslateAttribute()
        {
        }

        public TranslateAttribute(string key)
        {
            this.Key = key;
        }
    }
}
