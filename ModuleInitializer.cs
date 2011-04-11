#region usings

using System;
using System.Linq;
using System.Reflection;

#endregion

internal class ModuleInitializer
{
    public static void Run()
    {
        AppDomain.CurrentDomain.AssemblyResolve +=
            (sender, args) =>
            {
                Console.WriteLine("Doing manual assembly loading in: {0}", typeof(ModuleInitializer).AssemblyQualifiedName);
                Console.WriteLine("Looking for: {0}", args.Name);

                var resourceName = new AssemblyName(args.Name).Name + ".dll";

                var resourcePath =
                    Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(name => name.EndsWith(resourceName)).
                        SingleOrDefault();
                if (resourcePath == null)
                {
                    Console.WriteLine("Could not find embedded assembly: {0}", resourceName);
                    return null;
                }

                Console.WriteLine("Loading assembly from resource: {0}", resourcePath);
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
                {
                    var assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
    }
}