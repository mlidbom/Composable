using JetBrains.Annotations;

namespace Composable.Testing.System
{
    ///<summary>Contains extensions on <see cref="string"/></summary>
    static class StringExtensions
    {
        ///<summary>returns true if me is null, empty or only whitespace</summary>
        [ContractAnnotation("null => true")]
        internal static bool IsNullOrWhiteSpace(this string me) => string.IsNullOrWhiteSpace(me);
    }
}