using Composable.Persistence.MsSql.SystemExtensions;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.MsSql.DocumentDb
{
    partial class MsSqlDocumentDbPersistenceLayer
    {
        class SchemaManager
        {
            readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
            bool _initialized = false;
            readonly IMsSqlConnectionPool _connectionPool;
            public SchemaManager(IMsSqlConnectionPool connectionPool) => _connectionPool = connectionPool;

            internal void EnsureInitialized() => _monitor.Update(() =>
            {
                if(!_initialized)
                {
                    TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                    {
                        _connectionPool.ExecuteNonQuery($@"
IF NOT EXISTS(select name from sys.tables where name = '{Document.TableName}')
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
            });
        }
    }
}
