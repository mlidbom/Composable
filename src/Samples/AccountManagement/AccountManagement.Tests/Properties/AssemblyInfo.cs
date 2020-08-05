using System.Runtime.CompilerServices;
using Composable.Testing;
using NUnit.Framework;


[assembly: InternalsVisibleTo("AccountManagement.PerformanceTests")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelismCE]
#endif
