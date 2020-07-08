using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System.Transactions;

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
                            _connectionProvider.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS store (
  Id VARCHAR(500) NOT NULL,
  ValueTypeId CHAR(38) NOT NULL,
  Created DATETIME NOT NULL,
  Updated DATETIME NOT NULL,
  Value MEDIUMTEXT NOT NULL,
  PRIMARY KEY (Id, ValueTypeId))
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
