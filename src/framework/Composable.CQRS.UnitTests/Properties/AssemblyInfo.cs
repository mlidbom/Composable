
using NUnit.Framework;

#if !NCRUNCH
    [assembly: Parallelizable(ParallelScope.Fixtures)]
#endif