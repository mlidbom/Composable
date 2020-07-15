using Composable.DependencyInjection;


//urgent: include at least InMemory as testing PersistenceLayerProvider [assembly:NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.InMemory))]
[assembly:NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.InMemory), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.PgSql), nameof(PersistenceLayer.Orcl))]
