using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.MsSql.SystemExtensions;
using Message =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.MsSql.Messaging.Buses.Implementation
{
    partial class MsSqlInboxPersistenceLayer
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IMsSqlConnectionProvider connectionFactory)
            {
                //Performance: Why is the MessageId not the primary key? Are we worried about performance loss because of fragmentation because of non-sequential Guids? Is there a (performant and truly reliable) sequential-guid-generator we could use? How does it not being the clustered index impact row vs page etc locking?
                await  connectionFactory.ExecuteNonQueryAsync($@"
IF NOT EXISTS(select name from sys.tables where name = '{Message.TableName}')
BEGIN
    CREATE TABLE {Message.TableName}
    (
        {Message.GeneratedId}         bigint IDENTITY(1,1) NOT NULL,
        {Message.TypeId}              uniqueidentifier     NOT NULL,
        {Message.MessageId}           uniqueidentifier     NOT NULL,
        {Message.Status}              smallint             NOT NULL,
        {Message.Body}                nvarchar(MAX)        NOT NULL,
        {Message.ExceptionCount}      int                  NOT NULL  DEFAULT 0,
        {Message.ExceptionType}       nvarchar(500)        NULL,
        {Message.ExceptionStackTrace} nvarchar(MAX)        NULL,
        {Message.ExceptionMessage}    nvarchar(MAX)        NULL,


        CONSTRAINT PK_{Message.TableName} PRIMARY KEY CLUSTERED ( [{Message.GeneratedId}] ASC ),

        CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
    )
END
");
            }
        }
    }
}
