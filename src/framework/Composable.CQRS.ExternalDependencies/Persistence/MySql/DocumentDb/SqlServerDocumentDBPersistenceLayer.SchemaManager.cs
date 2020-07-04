using Composable.Persistence.MySql.SystemExtensions;
using Composable.System.Transactions;

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
                            _connectionProvider.ExecuteNonQuery(@"
IF NOT EXISTS(select name from sys.tables where name = 'Store')
BEGIN 
    CREATE TABLE [dbo].[Store](
	    [Id] [nvarchar](500) NOT NULL,
	    [ValueTypeId] uniqueidentifier NOT NULL,
        [Created] [datetime2] NOT NULL,
        [Updated] [datetime2] NOT NULL,
	    [Value] [nvarchar](max) NOT NULL,
     CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
    (
	    [Id] ASC,
	    [ValueTypeId] ASC
    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
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
