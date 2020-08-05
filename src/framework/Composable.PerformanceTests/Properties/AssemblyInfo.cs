using NCrunch.Framework;
using NUnit.Framework;

#if !NCRUNCH
[assembly: Parallelizable(ParallelScope.None)]
#endif

//Nothing in this project should run in parallel
[assembly:Serial, NUnit.Framework.Category("Performance")]