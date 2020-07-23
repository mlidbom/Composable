using Composable.Persistence.MySql.SystemExtensions;
using Composable.SystemCE.Transactions;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.MySql.DocumentDb
{
    partial class MySqlDocumentDbPersistenceLayer
    {
        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly IMySqlConnectionProvider _connectionProvider;
            public SchemaManager(IMySqlConnectionProvider connectionProvider) => _connectionProvider = connectionProvider;

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
  {Document.Created}     DATETIME     NOT NULL,
  {Document.Updated}     DATETIME     NOT NULL,
  {Document.Value}       MEDIUMTEXT   NOT NULL,

  PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4;

");
                        });
                    }

                    _initialized = true;
                }
            }
        }
    }
}
