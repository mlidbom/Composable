using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.PgSql.DocumentDb
{
    partial class PgSqlDocumentDbPersistenceLayer
    {
        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly INpgsqlConnectionProvider _connectionProvider;
            public SchemaManager(INpgsqlConnectionProvider connectionProvider) => _connectionProvider = connectionProvider;

            internal void EnsureInitialized()
            {
                lock(_lockObject)
                {
                    if(!_initialized)
                    {
                        TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                        {
                            _connectionProvider.ExecuteNonQuery($@"
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
                }
            }
        }
    }
}
