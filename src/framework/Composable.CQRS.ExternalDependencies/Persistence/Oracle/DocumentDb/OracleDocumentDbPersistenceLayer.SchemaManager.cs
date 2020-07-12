using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System.Transactions;

namespace Composable.Persistence.Oracle.DocumentDb
{
    partial class OracleDocumentDbPersistenceLayer
    {
        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly IOracleConnectionProvider _connectionProvider;
            public SchemaManager(IOracleConnectionProvider connectionProvider) => _connectionProvider = connectionProvider;

            internal void EnsureInitialized()
            {
                lock(_lockObject)
                {
                    if(!_initialized)
                    {
                        TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                        {
                            _connectionProvider.ExecuteNonQuery(@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='STORE';
    if (existing_table_count <= 0) then
        EXECUTE IMMEDIATE '
            CREATE TABLE STORE (
                ID VARCHAR2(500) NOT NULL, 
                VALUETYPEID CHAR(38) NOT NULL,
                CREATED TIMESTAMP NOT NULL,
                UPDATED TIMESTAMP NOT NULL,
                VALUE NCLOB NOT NULL,
                
                CONSTRAINT STORE_PK PRIMARY KEY (ID, VALUETYPEID ) ENABLE)';
    end if;
end;
");
                        });
                    }

                    _initialized = true;
                }
            }
        }
    }
}
