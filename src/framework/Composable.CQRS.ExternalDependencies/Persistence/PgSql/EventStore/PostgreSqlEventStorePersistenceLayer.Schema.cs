using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System.Transactions;
using C=Composable.Persistence.Common.EventStore.EventTable.Columns;

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


    CREATE TABLE IF NOT EXISTS {EventTable.Name}(
        {C.InsertionOrder} bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
        {C.AggregateId} {PgSqlGuidType} NOT NULL,  
        {C.UtcTimeStamp} timestamp NOT NULL,   
        {C.EventType} {PgSqlGuidType} NOT NULL,    
        {C.Event} TEXT NOT NULL,
        {C.EventId} {PgSqlGuidType} NOT NULL,
        {C.InsertedVersion} int NOT NULL,
        {C.SqlInsertTimeStamp} timestamp default CURRENT_TIMESTAMP,
        {C.TargetEvent} {PgSqlGuidType} null,
        {C.RefactoringType} smallint null,
        {C.ReadOrder} bigint null,
        {C.ReadOrderOrderOffset} bigint null,
        {C.EffectiveOrder} {EventTable.ReadOrderType} null,    
        {C.EffectiveVersion} int NULL,

        PRIMARY KEY ({C.AggregateId}, {C.InsertedVersion}),





        CONSTRAINT IX_{EventTable.Name}_Unique_{C.EventId} UNIQUE ( {C.EventId} ),
        CONSTRAINT IX_{EventTable.Name}_Unique_{C.InsertionOrder} UNIQUE ( {C.InsertionOrder} ),

        FOREIGN KEY ( {C.TargetEvent} ) 
            REFERENCES {EventTable.Name} ({C.EventId})
    );

    CREATE INDEX IX_{EventTable.Name}_{C.EffectiveOrder} ON {EventTable.Name} 
            ({C.EffectiveOrder} , {C.EffectiveVersion} );
            /*INCLUDE ({C.EventType}, {C.InsertionOrder})*/
"));

                _initialized = true;
            }
        });
    }
}
