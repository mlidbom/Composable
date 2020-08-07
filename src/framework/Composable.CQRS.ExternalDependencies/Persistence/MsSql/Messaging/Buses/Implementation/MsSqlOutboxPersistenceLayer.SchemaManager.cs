using System.Threading.Tasks;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Message = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using D = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.MsSql.Messaging.Buses.Implementation
{
    partial class MsSqlOutboxPersistenceLayer
    {
        static class SchemaManager
        {
            //Performance: Why is the MessageId not the primary key? Are we worried about performance loss because of fragmentation because of non-sequential Guids? Is there a (performant and truly reliable) sequential-guid-generator we could use? How does it not being the clustered index impact row vs page etc locking?
            public static async Task EnsureTablesExistAsync(IMsSqlConnectionPool connectionFactory)
            {
                await connectionFactory.ExecuteNonQueryAsync($@"
IF NOT EXISTS (select name from sys.tables where name = '{Message.TableName}')
BEGIN
    CREATE TABLE {Message.TableName}
    (
        {Message.GeneratedId}       bigint IDENTITY(1,1) NOT NULL,
        {Message.TypeIdGuidValue}   uniqueidentifier     NOT NULL,
        {Message.MessageId}         uniqueidentifier     NOT NULL,
        {Message.SerializedMessage} nvarchar(MAX)        NOT NULL,

        CONSTRAINT PK_{Message.TableName} PRIMARY KEY CLUSTERED ( [{Message.GeneratedId}] ASC ),

        CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
    )

    CREATE TABLE dbo.{D.TableName}
    (
        {D.MessageId}  uniqueidentifier NOT NULL,
        {D.EndpointId} uniqueidentifier NOT NULL,
        {D.IsReceived} bit              NOT NULL,

        CONSTRAINT PK_{D.TableName} PRIMARY KEY CLUSTERED( {D.MessageId}, {D.EndpointId})
            WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

        CONSTRAINT FK_{D.TableName}_{D.MessageId} FOREIGN KEY ( {D.MessageId} )  REFERENCES {Message.TableName} ({Message.MessageId})
    )
END
").NoMarshalling();
            }
        }
    }
}
