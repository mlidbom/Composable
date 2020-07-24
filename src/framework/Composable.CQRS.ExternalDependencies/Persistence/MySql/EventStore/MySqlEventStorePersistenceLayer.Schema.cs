using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

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


    CREATE TABLE IF NOT EXISTS {Event.TableName}
    (
        {Event.InsertionOrder}       bigint                     NOT NULL  AUTO_INCREMENT,
        {Event.AggregateId}          {MySqlGuidType}            NOT NULL,  
        {Event.UtcTimeStamp}         datetime(6) NOT            NULL,   
        {Event.EventType}            {MySqlGuidType}            NOT NULL,    
        {Event.Event}                MEDIUMTEXT                 NOT NULL,
        {Event.EventId}              {MySqlGuidType}            NOT NULL,
        {Event.InsertedVersion}      int                        NOT NULL,
        {Event.SqlInsertTimeStamp}   datetime(6)                NOT NULL  default CURRENT_TIMESTAMP,
        {Event.ReadOrder}            {Event.ReadOrderType}      NOT NULL,    
        {Event.EffectiveVersion}     int                        NOT NULL,
        {Event.TargetEvent}          {MySqlGuidType}            NULL,
        {Event.RefactoringType}      tinyint                    NULL,

        PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),





        UNIQUE INDEX IX_{Event.TableName}_Unique_{Event.EventId}        ( {Event.EventId} ASC ),
        UNIQUE INDEX IX_{Event.TableName}_Unique_{Event.InsertionOrder} ( {Event.InsertionOrder} ASC ),
        UNIQUE INDEX IX_{Event.TableName}_Unique_{Event.ReadOrder}      ( {Event.ReadOrder} ASC ),

        FOREIGN KEY ( {Event.TargetEvent} ) 
            REFERENCES {Event.TableName} ({Event.EventId}),


        INDEX IX_{Event.TableName}_{Event.ReadOrder} 
            ({Event.ReadOrder} ASC, {Event.EffectiveVersion} ASC)
            /*INCLUDE ({Event.EventType}, {Event.InsertionOrder})*/

    )
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4;
"));

                _initialized = true;
            }
        });
    }
}
