using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System.Transactions;
using C=Composable.Persistence.Common.EventStore.EventTable.Columns;

namespace Composable.Persistence.MySql.EventStore
{
    partial class MySqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        const string MySqlGuidType = "CHAR(36)";
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                //Urgent: Figure out the syntax for the commented out parts.
                _connectionManager.UseCommand(command=> command.ExecuteNonQuery($@"

    CREATE TABLE IF NOT EXISTS {EventTable.Name}(
        {C.InsertionOrder} bigint NOT NULL AUTO_INCREMENT,
        {C.AggregateId} {MySqlGuidType} NOT NULL,  
        {C.UtcTimeStamp} datetime(6) NOT NULL,   
        {C.EventType} {MySqlGuidType} NOT NULL,    
        {C.Event} MEDIUMTEXT NOT NULL,
        {C.EventId} {MySqlGuidType} NOT NULL,
        {C.InsertedVersion} int NOT NULL,
        {C.SqlInsertTimeStamp} datetime(6) default CURRENT_TIMESTAMP,
        {C.InsertAfter} {MySqlGuidType} null,
        {C.InsertBefore} {MySqlGuidType} null,
        {C.Replaces} {MySqlGuidType} null,
        {C.ReadOrder} bigint null,
        {C.ReadOrderOrderOffset} bigint null,
        {C.EffectiveOrder} {EventTable.ReadOrderType} null,    
        {C.EffectiveVersion} int NULL,

        PRIMARY KEY ({C.AggregateId}, {C.InsertedVersion}),





        UNIQUE INDEX IX_{EventTable.Name}_Unique_{C.EventId} ( {C.EventId} ASC ),
        UNIQUE INDEX IX_{EventTable.Name}_Unique_{C.InsertionOrder} ( {C.InsertionOrder} ASC ),
/*
        CONSTRAINT CK_{EventTable.Name}_Only_one_reordering_column_allowed_for_use
        CHECK 
        (
            ({C.InsertAfter} is null and {C.InsertBefore} is null)
            or
            ({C.InsertAfter} is null and {C.Replaces} is null)
            or
            ({C.InsertBefore} is null and {C.Replaces} is null) 
        ),
*/
        FOREIGN KEY ( {C.Replaces} ) 
            REFERENCES {EventTable.Name} ({C.EventId}),

        FOREIGN KEY ( {C.InsertBefore} )
            REFERENCES {EventTable.Name} ({C.EventId}),

        FOREIGN KEY ( {C.InsertAfter} ) 
            REFERENCES {EventTable.Name} ({C.EventId}),


        INDEX IX_{EventTable.Name}_{C.EffectiveOrder} 
            ({C.EffectiveOrder} ASC, {C.EffectiveVersion} ASC)
            /*INCLUDE ({C.EventType}, {C.InsertionOrder})*/

    )
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4;
"));

                _initialized = true;
            }
        });
    }
}
