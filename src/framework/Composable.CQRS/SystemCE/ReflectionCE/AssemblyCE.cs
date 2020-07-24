using System.Linq;
using System.Reflection;

namespace Composable.SystemCE.ReflectionCE
{
    static class AssemblyCE
    {
        internal static bool IsKnownThirdPartyLibrary(this Assembly @this)
        {
            var name = @this.GetName().Name ?? "";

            if(ExactNamesForKnownThirdPartyLibraryAssemblies.Contains(name)) return true;

            if(StartPatternsForKnownThirdPartyLibraryAssemblies.Any(me => name.StartsWithInvariant(me))) return true;

            return false;
        }

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
