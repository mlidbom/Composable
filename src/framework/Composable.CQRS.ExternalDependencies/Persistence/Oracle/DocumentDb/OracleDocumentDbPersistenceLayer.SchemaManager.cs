using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System.Transactions;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.Oracle.DocumentDb
{
    partial class OracleDocumentDbPersistenceLayer
    {
        const string OracleGuidType = "CHAR(36)";

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
                            _connectionProvider.ExecuteNonQuery($@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='{Document.TableName.ToUpperInvariant()}';
    if (existing_table_count = 0) then
        EXECUTE IMMEDIATE '
        
            CREATE TABLE {Document.TableName} 
            (
                {Document.Id}           VARCHAR2(500)    NOT NULL, 
                {Document.ValueTypeId}  {OracleGuidType} NOT NULL,
                {Document.Created}      TIMESTAMP        NOT NULL,
                {Document.Updated}      TIMESTAMP        NOT NULL,
                {Document.Value}        NCLOB            NOT NULL,
                
                CONSTRAINT PK_{Document.TableName} PRIMARY KEY ({Document.Id}, {Document.ValueTypeId}) ENABLE
            )
        ';

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
