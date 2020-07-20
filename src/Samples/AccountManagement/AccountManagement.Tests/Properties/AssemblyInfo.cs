using Composable.DependencyInjection;
using NCrunch.Framework;

//urgent: include at least InMemory as testing PersistenceLayerProvider [assembly:NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.InMemory))]
[assembly:DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.Memory), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.PgSql), nameof(PersistenceLayer.Orcl), nameof(PersistenceLayer.DB2))]
