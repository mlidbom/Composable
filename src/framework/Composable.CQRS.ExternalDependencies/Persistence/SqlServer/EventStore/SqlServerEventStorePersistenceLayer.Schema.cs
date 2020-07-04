using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System.Transactions;
using C=Composable.Persistence.SqlServer.EventStore.EventTable.Columns;

namespace Composable.Persistence.SqlServer.EventStore
{
    partial class SqlServerEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                using var connection = _connectionManager.OpenConnection();

                connection.ExecuteNonQuery($@"
IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{EventTable.Name}')
BEGIN
    CREATE TABLE dbo.{EventTable.Name}(
        {C.InsertionOrder} bigint IDENTITY(1,1) NOT NULL,
        {C.AggregateId} uniqueidentifier NOT NULL,  
        {C.UtcTimeStamp} datetime2 NOT NULL,   
        {C.EventType} uniqueidentifier NOT NULL,    
        {C.Event} nvarchar(max) NOT NULL,
        {C.EventId} uniqueidentifier NOT NULL,
        {C.InsertedVersion} int NOT NULL,
        {C.SqlInsertTimeStamp} datetime2 default SYSUTCDATETIME(),
        {C.InsertAfter} uniqueidentifier null,
        {C.InsertBefore} uniqueidentifier null,
        {C.Replaces} uniqueidentifier null,
        {C.ReadOrder} bigint null,
        {C.ReadOrderOrderOffset} bigint null,
        {C.EffectiveOrder} {EventTable.ReadOrderType} null,    
        {C.EffectiveVersion} int NULL,

        CONSTRAINT PK_{EventTable.Name} PRIMARY KEY CLUSTERED 
        (
            {C.AggregateId} ASC,
            {C.InsertedVersion} ASC
        )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

        CONSTRAINT IX_{EventTable.Name}_Unique_{C.EventId} UNIQUE
        (
            {C.EventId}
        ),

        CONSTRAINT IX_{EventTable.Name}_Unique_{C.InsertionOrder} UNIQUE
        (
            {C.InsertionOrder}
        ),

        CONSTRAINT CK_{EventTable.Name}_Only_one_reordering_column_allowed_for_use
        CHECK 
        (
            ({C.InsertAfter} is null and {C.InsertBefore} is null)
            or
            ({C.InsertAfter} is null and {C.Replaces} is null)
            or
            ({C.InsertBefore} is null and {C.Replaces} is null) 
        ),

        CONSTRAINT FK_{EventTable.Name}_{C.Replaces} FOREIGN KEY ( {C.Replaces} ) 
            REFERENCES {EventTable.Name} ({C.EventId}),

        CONSTRAINT FK_{EventTable.Name}_{C.InsertBefore} FOREIGN KEY ( {C.InsertBefore} )
            REFERENCES {EventTable.Name} ({C.EventId}),

        CONSTRAINT FK_{EventTable.Name}_{C.InsertAfter} FOREIGN KEY ( {C.InsertAfter} ) 
            REFERENCES {EventTable.Name} ({C.EventId}) 
    )

        CREATE NONCLUSTERED INDEX IX_{EventTable.Name}_{C.EffectiveOrder} ON dbo.{EventTable.Name}
            ({C.EffectiveOrder}, {C.EffectiveVersion})
            INCLUDE ({C.EventType}, {C.InsertionOrder})

        CREATE NONCLUSTERED INDEX IX_{EventTable.Name}_{C.Replaces}	ON dbo.{EventTable.Name}
            ({C.Replaces})
            INCLUDE ({C.EventId})

        CREATE NONCLUSTERED INDEX IX_{EventTable.Name}_{C.InsertAfter}	ON dbo.{EventTable.Name}
            ({C.InsertAfter})
            INCLUDE ({C.EventId})

        CREATE NONCLUSTERED INDEX IX_{EventTable.Name}_{C.InsertBefore}	ON dbo.{EventTable.Name} 
            ({C.InsertBefore})
            INCLUDE ({C.EventId})

        CREATE NONCLUSTERED INDEX IX_{EventTable.Name}_{C.EffectiveVersion}	ON dbo.{EventTable.Name} 
            ({C.EffectiveVersion})
END 
");

                _initialized = true;
            }
        });
    }
}
