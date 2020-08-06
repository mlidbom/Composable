using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.PgSql.DocumentDb
{
    partial class PgSqlDocumentDbPersistenceLayer
    {
        class SchemaManager
        {
            readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
            bool _initialized = false;
            readonly IPgSqlConnectionPool _connectionPool;
            public SchemaManager(IPgSqlConnectionPool connectionPool) => _connectionPool = connectionPool;

            internal void EnsureInitialized() => _monitor.Update(() =>
            {
                if(!_initialized)
                {
                    TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                    {
                        _connectionPool.PrepareAndExecuteNonQuery($@"
CREATE TABLE IF NOT EXISTS {Document.TableName} 
(
    {Document.Id}          VARCHAR(500) NOT NULL,
    {Document.ValueTypeId} CHAR(38)     NOT NULL,
    {Document.Created}     TIMESTAMP    NOT NULL,
    {Document.Updated}     TIMESTAMP    NOT NULL,
    {Document.Value}       TEXT         NOT NULL,

    PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
)

");
                    });
                }

                _initialized = true;
            });
        }
    }
}
