using Composable.Persistence.MsSql.SystemExtensions;
using Composable.System.Transactions;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.MsSql.DocumentDb
{
    partial class MsSqlDocumentDbPersistenceLayer
    {
        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly IMsSqlConnectionProvider _connectionProvider;
            public SchemaManager(IMsSqlConnectionProvider connectionProvider) => _connectionProvider = connectionProvider;

            internal void EnsureInitialized()
            {
                lock(_lockObject)
                {
                    if(!_initialized)
                    {
                        TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                        {
                            _connectionProvider.ExecuteNonQuery($@"
IF NOT EXISTS(select name from sys.tables where name = 'Store')
BEGIN 
    CREATE TABLE dbo.{Document.TableName}
    (
        {Document.Id}          nvarchar(500)    NOT NULL,
        {Document.ValueTypeId} uniqueidentifier NOT NULL,
        {Document.Created}     datetime2        NOT NULL,
        {Document.Updated}     datetime2        NOT NULL,
        {Document.Value}       nvarchar(max)    NOT NULL,
           
        CONSTRAINT PK_{Document.TableName} PRIMARY KEY CLUSTERED 
           ({Document.Id} ASC, {Document.ValueTypeId} ASC)
           WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF)
    )

END
");
                        });
                    }

                    _initialized = true;
                }
            }
        }
    }
}
