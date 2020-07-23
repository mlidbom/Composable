using Composable.Persistence.DB2.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Document = Composable.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;
// ReSharper disable StringLiteralTypo

namespace Composable.Persistence.DB2.DocumentDb
{
    partial class DB2DocumentDbPersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";

        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly IComposableDB2ConnectionProvider _connectionProvider;
            public SchemaManager(IComposableDB2ConnectionProvider connectionProvider) => _connectionProvider = connectionProvider;

            internal void EnsureInitialized()
            {
                lock(_lockObject)
                {
                    if(!_initialized)
                    {
                        TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
                        {
                            _connectionProvider.UseCommand(cmd => cmd.SetCommandText($@"
begin
    declare continue handler for sqlstate '42710' begin end; --Ignore error if table exists
        
    EXECUTE IMMEDIATE '

        CREATE TABLE {Document.TableName} 
        (
            {Document.Id}           VARCHAR(500)  NOT NULL, 
            {Document.ValueTypeId}  {DB2GuidType} NOT NULL,
            {Document.Created}      TIMESTAMP     NOT NULL,
            {Document.Updated}      TIMESTAMP     NOT NULL,
            {Document.Value}        CLOB          NOT NULL,
            
            PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
        );
    ';
end;
")
                                                                     .ExecuteNonQuery());
                        });
                    }

                    _initialized = true;
                }
            }
        }
    }
}
