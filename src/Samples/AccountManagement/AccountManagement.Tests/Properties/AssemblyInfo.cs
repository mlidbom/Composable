using Composable.DependencyInjection;


//urgent: include at least InMemory as testing PersistenceLayerProvider [assembly:NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.SqlServer), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.InMemory))]
[assembly:NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.SqlServer))]
