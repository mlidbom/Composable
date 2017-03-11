using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Composable.CQRS")]
[assembly: AssemblyProduct("Composable.CQRS")]

[assembly: InternalsVisibleTo("Composable.StagingArea")]
[assembly: InternalsVisibleTo("Composable.HyperBus")]
[assembly: InternalsVisibleTo("NetMqProcess01")]
[assembly: InternalsVisibleTo("NetMqProcess02")]
[assembly: InternalsVisibleTo("NSpec.NUnit")]
[assembly: InternalsVisibleTo("Composable.CQRS")]
[assembly: InternalsVisibleTo("Composable.Core.Tests")]
[assembly: InternalsVisibleTo("Composable.CQRS.Specs")]
[assembly: InternalsVisibleTo("Composable.CQRS.Tests")]
[assembly: InternalsVisibleTo("Composable.CQRS.Testing")]