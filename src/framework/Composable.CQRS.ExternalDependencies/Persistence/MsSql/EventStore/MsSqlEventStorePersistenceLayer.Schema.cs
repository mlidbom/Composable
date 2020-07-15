﻿using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.System.Transactions;
using Event=Composable.Persistence.Common.EventStore.EventTable.Columns;

namespace Composable.Persistence.MsSql.EventStore
{
    partial class MsSqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {

        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                _connectionManager.UseCommand(command=> command.ExecuteNonQuery($@"
IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{EventTable.Name}')
BEGIN
    CREATE TABLE dbo.{EventTable.Name}(
        {Event.InsertionOrder} bigint IDENTITY(1,1) NOT NULL,
        {Event.AggregateId} uniqueidentifier NOT NULL,  
        {Event.UtcTimeStamp} datetime2 NOT NULL,   
        {Event.EventType} uniqueidentifier NOT NULL,    
        {Event.Event} nvarchar(max) NOT NULL,
        {Event.EventId} uniqueidentifier NOT NULL,
        {Event.InsertedVersion} int NOT NULL,
        {Event.SqlInsertTimeStamp} datetime2 default SYSUTCDATETIME(),
        {Event.TargetEvent} uniqueidentifier null,
        {Event.RefactoringType} tinyint null,
        {Event.ReadOrder} bigint null,
        {Event.ReadOrderOrderOffset} bigint null,
        {Event.EffectiveOrder} {EventTable.ReadOrderType} null,    
        {Event.EffectiveVersion} int NULL,

        CONSTRAINT PK_{EventTable.Name} PRIMARY KEY CLUSTERED 
        (
            {Event.AggregateId} ASC,
            {Event.InsertedVersion} ASC
        )WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

        CONSTRAINT IX_{EventTable.Name}_Unique_{Event.EventId} UNIQUE ( {Event.EventId} ),
        CONSTRAINT IX_{EventTable.Name}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),

        CONSTRAINT FK_{EventTable.Name}_{Event.TargetEvent} FOREIGN KEY ( {Event.TargetEvent} ) 
            REFERENCES {EventTable.Name} ({Event.EventId}) 
    )

        CREATE NONCLUSTERED INDEX IX_{EventTable.Name}_{Event.EffectiveOrder} ON dbo.{EventTable.Name}
            ({Event.EffectiveOrder}, {Event.EffectiveVersion})
            INCLUDE ({Event.EventType}, {Event.InsertionOrder})
END 
"));

                _initialized = true;
            }
        });
    }
}
