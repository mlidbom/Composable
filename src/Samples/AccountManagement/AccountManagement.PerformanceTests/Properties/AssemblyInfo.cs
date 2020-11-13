using System.Reflection;
using NCrunch.Framework;
using NUnit.Framework;

[assembly: AssemblyVersion("1.0.0.0")]

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.None)]
#endif

//Nothing in this project should run in parallel
[assembly:Serial, NUnit.Framework.Category("Performance")]