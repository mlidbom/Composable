using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
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
        {C.TargetEvent} {MySqlGuidType} null,
        {C.RefactoringType} tinyint null,
        {C.ReadOrder} bigint null,
        {C.ReadOrderOrderOffset} bigint null,
        {C.EffectiveOrder} {EventTable.ReadOrderType} null,    
        {C.EffectiveVersion} int NULL,

        PRIMARY KEY ({C.AggregateId}, {C.InsertedVersion}),





        UNIQUE INDEX IX_{EventTable.Name}_Unique_{C.EventId} ( {C.EventId} ASC ),
        UNIQUE INDEX IX_{EventTable.Name}_Unique_{C.InsertionOrder} ( {C.InsertionOrder} ASC ),

        FOREIGN KEY ( {C.TargetEvent} ) 
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
