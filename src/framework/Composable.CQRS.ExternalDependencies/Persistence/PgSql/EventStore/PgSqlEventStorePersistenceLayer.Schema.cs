using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

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


    CREATE TABLE IF NOT EXISTS {Event.TableName}
    (
        {Event.InsertionOrder}          bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
        {Event.AggregateId}             {PgSqlGuidType}                     NOT NULL,  
        {Event.UtcTimeStamp}            timestamp                           NOT NULL,   
        {Event.EventType}               {PgSqlGuidType}                     NOT NULL,    
        {Event.Event}                   TEXT                                NOT NULL,
        {Event.EventId}                 {PgSqlGuidType}                     NOT NULL,
        {Event.InsertedVersion}         int                                 NOT NULL,
        {Event.SqlInsertTimeStamp}      timestamp                           NOT NULL  default CURRENT_TIMESTAMP,
        {Event.ReadOrder}               {Event.ReadOrderType}               NOT NULL,    
        {Event.EffectiveVersion}        int                                 NOT NULL,
        {Event.TargetEvent}             {PgSqlGuidType}                     NULL,
        {Event.RefactoringType}         smallint                            NULL,

        PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),





        CONSTRAINT IX_{Event.TableName}_Unique_{Event.EventId}        UNIQUE ( {Event.EventId} ),
        CONSTRAINT IX_{Event.TableName}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),
        CONSTRAINT IX_{Event.TableName}_Unique_{Event.ReadOrder}      UNIQUE ( {Event.ReadOrder} ),

        FOREIGN KEY ( {Event.TargetEvent} ) 
            REFERENCES {Event.TableName} ({Event.EventId})
    );

    CREATE INDEX IF NOT EXISTS IX_{Event.TableName}_{Event.ReadOrder} ON {Event.TableName} 
            ({Event.ReadOrder} , {Event.EffectiveVersion} );
            /*INCLUDE ({Event.EventType}, {Event.InsertionOrder})*/


    CREATE TABLE IF NOT EXISTS {Lock.TableName}
    (
        {Lock.AggregateId} {PgSqlGuidType} NOT NULL,
        PRIMARY KEY ( {Lock.AggregateId} )
    );

"));

                _initialized = true;
            }
        });
    }
}
