using System;
using System.Collections.Concurrent;
using Composable.System.Data.SqlClient;

namespace Composable.Persistence.DocumentDb.SqlServer
{
    partial class SqlServerDocumentDb
    {
        class SchemaManager
        {
            readonly object _lockObject = new object();
            bool _initialized = false;
            readonly ISqlConnection _connectionManager;
            public SchemaManager(ISqlConnection connectionManager) => _connectionManager = connectionManager;

            internal void EnsureInitialized()
            {
                lock(_lockObject)
                {
                    if(!_initialized)
                    {
                        using(var connection = _connectionManager.OpenConnection())
                        {
                            var valueTypeExists = (int)connection.ExecuteScalar("select count(*) from sys.tables where name = 'ValueType'");
                            if(valueTypeExists == 0)
                            {
                                connection.ExecuteNonQuery(@"
CREATE TABLE [dbo].[ValueType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ValueType] [varchar](500) NOT NULL,
 CONSTRAINT [PK_ValueType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
");
                            }

                            var storeExists = (int)connection.ExecuteScalar("select count(*) from sys.tables where name = 'Store'");
                            if(storeExists == 0)
                            {
                                connection.ExecuteNonQuery(
                                    @"
CREATE TABLE [dbo].[Store](
	[Id] [nvarchar](500) NOT NULL,
	[ValueTypeId] [int] NOT NULL,
    [Created] [datetime2] NOT NULL,
    [Updated] [datetime2] NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[ValueTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_ValueType_Store] FOREIGN KEY([ValueTypeId])
REFERENCES [dbo].[ValueType] ([Id])

ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_ValueType_Store]

");
                            }
                        }
                    }
                }
            }
        }
    }
}
