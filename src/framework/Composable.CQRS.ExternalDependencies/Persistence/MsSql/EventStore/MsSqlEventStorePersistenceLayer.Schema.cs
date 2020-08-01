using Composable.Persistence.Common;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Event = Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.MsSql.EventStore
{
    partial class MsSqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbient(() =>
        {
            if(!_initialized)
            {
                _connectionManager.UseCommand(suppressTransactionWarning: true,
                                              command => command.ExecuteNonQuery($@"
IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{Event.TableName}')
BEGIN
    CREATE TABLE dbo.{Event.TableName}
    (
        {Event.InsertionOrder}       bigint IDENTITY(1,1)               NOT NULL,
        {Event.AggregateId}          uniqueidentifier                   NOT NULL,  
        {Event.UtcTimeStamp}         datetime2                          NOT NULL,   
        {Event.EventType}            uniqueidentifier                   NOT NULL,    
        {Event.Event}                nvarchar(max)                      NOT NULL,
        {Event.EventId}              uniqueidentifier                   NOT NULL,
        {Event.InsertedVersion}      int                                NOT NULL,
        {Event.SqlInsertTimeStamp}   datetime2 default SYSUTCDATETIME() NOT NULL,
        {Event.ReadOrder}            {Event.ReadOrderType}              NOT NULL,    
        {Event.EffectiveVersion}     int                                NOT NULL,
        {Event.TargetEvent}          uniqueidentifier                   NULL,
        {Event.RefactoringType}      tinyint                            NULL,

        CONSTRAINT PK_{Event.TableName} PRIMARY KEY CLUSTERED 
        (
            {Event.AggregateId} ASC,
            {Event.InsertedVersion} ASC
        )WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

        CONSTRAINT IX_{Event.TableName}_Unique_{Event.EventId}        UNIQUE ( {Event.EventId} ),
        CONSTRAINT IX_{Event.TableName}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),
        CONSTRAINT IX_{Event.TableName}_Unique_{Event.ReadOrder}      UNIQUE ( {Event.ReadOrder} ),

        CONSTRAINT FK_{Event.TableName}_{Event.TargetEvent} FOREIGN KEY ( {Event.TargetEvent} ) 
            REFERENCES {Event.TableName} ({Event.EventId}) 
    )

        CREATE NONCLUSTERED INDEX IX_{Event.TableName}_{Event.ReadOrder} ON dbo.{Event.TableName}
            ({Event.ReadOrder}, {Event.EffectiveVersion})
            INCLUDE ({Event.EventType}, {Event.InsertionOrder})
END 
"));

                _initialized = true;
            }
        });
    }
}
