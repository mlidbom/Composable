using System.Reflection;
using System.Runtime.CompilerServices;
using Composable.Testing;
using NUnit.Framework;


[assembly: AssemblyVersion("1.0.0.0")]
[assembly: InternalsVisibleTo("AccountManagement.PerformanceTests")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
