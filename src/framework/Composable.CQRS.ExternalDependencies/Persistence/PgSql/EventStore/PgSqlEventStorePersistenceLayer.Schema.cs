using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System.Transactions;
using Event=Composable.Persistence.Common.EventStore.EventTable.Columns;

namespace Composable.Persistence.PgSql.EventStore
{
    partial class PgSqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        const string PgSqlGuidType = "CHAR(36)";
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                _connectionManager.UseCommand(command=> command.ExecuteNonQuery($@"


    CREATE TABLE IF NOT EXISTS {EventTable.Name}
    (
        {Event.InsertionOrder}          bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
        {Event.AggregateId}             {PgSqlGuidType}                     NOT NULL,  
        {Event.UtcTimeStamp}            timestamp                           NOT NULL,   
        {Event.EventType}               {PgSqlGuidType}                     NOT NULL,    
        {Event.Event}                   TEXT                                NOT NULL,
        {Event.EventId}                 {PgSqlGuidType}                     NOT NULL,
        {Event.InsertedVersion}         int                                 NOT NULL,
        {Event.SqlInsertTimeStamp}      timestamp                           NOT NULL  default CURRENT_TIMESTAMP,
        {Event.ReadOrder}               {EventTable.ReadOrderType}          NOT NULL,    
        {Event.EffectiveVersion}        int                                 NOT NULL,
        {Event.TargetEvent}             {PgSqlGuidType}                     NULL,
        {Event.RefactoringType}         smallint                            NULL,

        PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),





        CONSTRAINT IX_{EventTable.Name}_Unique_{Event.EventId}        UNIQUE ( {Event.EventId} ),
        CONSTRAINT IX_{EventTable.Name}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),
        CONSTRAINT IX_{EventTable.Name}_Unique_{Event.ReadOrder}      UNIQUE ( {Event.ReadOrder} ),

        FOREIGN KEY ( {Event.TargetEvent} ) 
            REFERENCES {EventTable.Name} ({Event.EventId})
    );

    CREATE INDEX IF NOT EXISTS IX_{EventTable.Name}_{Event.ReadOrder} ON {EventTable.Name} 
            ({Event.ReadOrder} , {Event.EffectiveVersion} );
            /*INCLUDE ({Event.EventType}, {Event.InsertionOrder})*/


    CREATE TABLE IF NOT EXISTS AggregateLock
    (
        {Event.AggregateId} {PgSqlGuidType} NOT NULL,
        PRIMARY KEY ( {Event.AggregateId} )
    );

"));

                _initialized = true;
            }
        });
    }
}
