using Composable.Persistence.DB2.SystemExtensions;
using Composable.System.Transactions;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Composable.Persistence.DB2.DocumentDb
{
    partial class DB2DocumentDbPersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";

        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly IDB2ConnectionProvider _connectionProvider;
            public SchemaManager(IDB2ConnectionProvider connectionProvider) => _connectionProvider = connectionProvider;

            internal void EnsureInitialized()
            {
                lock(_lockObject)
                {
                    if(!_initialized)
                    {
                        TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                        {
                            //Urgent: Move to using common schema strings class like in event store and bus persistence layers.
                            _connectionProvider.ExecuteNonQuery($@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='STORE';
    if (existing_table_count = 0) then
        EXECUTE IMMEDIATE '
        
            CREATE TABLE {Document.TableName} 
            (
                {Document.Id}           VARCHAR2(500)    NOT NULL, 
                {Document.ValueTypeId}  {DB2GuidType} NOT NULL,
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
