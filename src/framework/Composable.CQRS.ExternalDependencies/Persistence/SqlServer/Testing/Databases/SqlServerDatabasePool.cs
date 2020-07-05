namespace Composable.Persistence.SqlServer.Testing.Databases
{
    class SqlServerDatabasePool : DatabasePool
    {
        protected override string ConnectionStringConfigurationParameterName => "COMPOSABLE_SQL_SERVER_DATABASE_POOL_MASTER_CONNECTIONSTRING";
    }
}
