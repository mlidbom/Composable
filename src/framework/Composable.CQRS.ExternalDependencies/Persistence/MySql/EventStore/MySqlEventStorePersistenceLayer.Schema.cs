using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System.Transactions;
using C=Composable.Persistence.Common.EventStore.EventTable.Columns;

namespace Composable.Persistence.MySql.EventStore
{
    partial class MySqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                //Urgent: Figure out the syntax for the commented out parts.
                _connectionManager.UseCommand(command=> command.ExecuteNonQuery($@"

    CREATE TABLE IF NOT EXISTS {EventTable.Name}(
        {C.InsertionOrder} bigint NOT NULL AUTO_INCREMENT,
        {C.AggregateId} CHAR(36) NOT NULL,  
        {C.UtcTimeStamp} datetime NOT NULL,   
        {C.EventType} CHAR(36) NOT NULL,    
        {C.Event} MEDIUMTEXT NOT NULL,
        {C.EventId} CHAR(36) NOT NULL,
        {C.InsertedVersion} int NOT NULL,
        {C.SqlInsertTimeStamp} datetime default CURRENT_TIMESTAMP,
        {C.InsertAfter} CHAR(36) null,
        {C.InsertBefore} CHAR(36) null,
        {C.Replaces} CHAR(36) null,
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

        CONSTRAINT FK_{EventTable.Name}_{C.Replaces} FOREIGN KEY ( {C.Replaces} ) 
            REFERENCES {EventTable.Name} ({C.EventId}),

        CONSTRAINT FK_{EventTable.Name}_{C.InsertBefore} FOREIGN KEY ( {C.InsertBefore} )
            REFERENCES {EventTable.Name} ({C.EventId}),

        CONSTRAINT FK_{EventTable.Name}_{C.InsertAfter} FOREIGN KEY ( {C.InsertAfter} ) 
            REFERENCES {EventTable.Name} ({C.EventId}) 
*/

        INDEX IX_{EventTable.Name}_{C.EffectiveOrder} 
            ({C.EffectiveOrder} ASC, {C.EffectiveVersion} ASC)
            /*INCLUDE ({C.EventType}, {C.InsertionOrder})*/

    )
"));

                _initialized = true;
            }
        });

        static readonly string blah = @"
CREATE TABLE `test`.`events` (
  `InsertionOrder` BIGINT NOT NULL AUTO_INCREMENT,
  `AggregateId` CHAR(36) NOT NULL,
  `UtcTimeStamp` DATETIME NOT NULL,
  `EventType` CHAR(36) NOT NULL,
  `Event` MEDIUMTEXT NOT NULL,
  `EventId` CHAR(36) NOT NULL,
  `InsertedVersion` INT NOT NULL,
  `SqlInsertTimeStamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `InsertAfter` CHAR(36) NULL,
  `InsertBefore` CHAR(36) NULL,
  `Replaces` CHAR(36) NULL,
  `ReadOrder` BIGINT NULL,
  `ReadOrderOrderOffset` BIGINT NULL,
  `EffectiveOrder` DECIMAL(38,19) NULL,
  `EffectiveVersion` INT NULL,
  UNIQUE INDEX `InsertionOrder_UNIQUE` (`InsertionOrder` ASC) INVISIBLE,
  PRIMARY KEY (`AggregateId`, `InsertedVersion`),
  UNIQUE INDEX `EventId_UNIQUE` (`EventId` ASC) VISIBLE,
  INDEX `IX_EVENTS_EFFECTIVEORDER` (`EffectiveOrder` ASC, `EffectiveVersion` ASC) VISIBLE);
";
    }
}
