#region usings

using System;
using System.Linq;
using System.Reflection;

#endregion

internal class ModuleInitializer
{
    private static bool _initialized;

    public static void Run()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        if (!_initialized)
        {
            _initialized = true;

            var embeddedAssemblies = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(name => name.EndsWith(".dll")).ToList();
            foreach (var embeddedAssembly in embeddedAssemblies)
            {
                var assemblyData = ReadMatchingResourceByteArray(embeddedAssembly);
                var pdbData = ReadMatchingResourceByteArray(embeddedAssembly.Replace(".dll", ".pdb"));

                if (!IsLoaded(assemblyData))
                {
                    Assembly.Load(assemblyData, pdbData);
                }
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.FullName == args.Name);
    }

    private static bool IsLoaded(byte[] assemblyData)
    {
        try
        {
            var name = Assembly.ReflectionOnlyLoad(assemblyData).FullName;
            return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName == name);
        }
        catch (System.IO.FileLoadException)
        {
            //Believe it or not this is what is thrown if the assembly is already loaded in the ReflectionOnly context..
            //We will simply assume that this means another assembly using this code has already loaded this assembly. 
            //The risk that we are mistaken should be extremely low.
            return true;
        }
    }

    private static byte[] ReadMatchingResourceByteArray(string resourceName)
    {
        var resourcePath = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(name => name.EndsWith(resourceName)).SingleOrDefault();
        if (resourcePath == null)
        {
            return null;
        }
        using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
        {
            var resourceData = new Byte[resourceStream.Length];
            resourceStream.Read(resourceData, 0, resourceData.Length);
            return resourceData;
        }
    }
}