using System.Linq;
using System.Reflection;

namespace Composable.System.Reflection
{
    static class AssemblyExtensions
    {
        internal static bool IsKnownThirdPartyLibrary(this Assembly @this)
        {
            var name = @this.GetName().Name;

            if(ExactNamesForKnownThirdPartyLibraryAssemblies.Contains(name)) return true;

            if(StartPatternsForKnownThirdPartyLibraryAssemblies.Any(me => name.StartsWith(me))) return true;

            return false;
        }

        internal static bool ContainsComposableMessageTypes(this Assembly @this) =>
            !@this.IsKnownThirdPartyLibrary() &&
            @this.GetCustomAttributesData().Any(attributeData => attributeData.AttributeType == typeof(ContainsComposableTypeIdsAttribute));

        static readonly string[] StartPatternsForKnownThirdPartyLibraryAssemblies = {"System.", "Castle.", "Microsoft.", "nunit.", "nCrunch.", "xunit."};
        static readonly string[] ExactNamesForKnownThirdPartyLibraryAssemblies =
        {
            "System",
            "mscorlib",
            "netstandard",
            "EasyHook",
            "FluentAssertions",
            "SimpleInjector",
            "NetMQ",
            "AsyncIO",
            "Newtonsoft.Json"
        };
    }
}
