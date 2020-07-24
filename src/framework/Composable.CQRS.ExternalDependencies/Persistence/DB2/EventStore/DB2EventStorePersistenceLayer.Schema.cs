using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.DB2.EventStore
{
    partial class DB2EventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                _connectionManager.UseCommand(command => command.SetCommandText($@"
begin
  declare continue handler for sqlstate '42710' begin end; --Ignore error if table exists
        EXECUTE IMMEDIATE '

            CREATE TABLE {Event.TableName}
            (
                {Event.InsertionOrder}        BIGINT GENERATED ALWAYS AS IDENTITY     NOT NULL ,
                {Event.AggregateId}           {DB2GuidType}                           NOT NULL,  
                {Event.UtcTimeStamp}          TIMESTAMP                               NOT NULL,   
                {Event.EventType}             {DB2GuidType}                           NOT NULL,    
                {Event.Event}                 CLOB                                    NOT NULL,
                {Event.EventId}               {DB2GuidType}                           NOT NULL,
                {Event.InsertedVersion}       INTEGER                                 NOT NULL,
                {Event.SqlInsertTimeStamp}    TIMESTAMP    default CURRENT_TIMESTAMP  NOT NULL,
                {Event.ReadOrderIntegerPart}  DECIMAL(19)                             NOT NULL,
                {Event.ReadOrderFractionPart} DECIMAL(19)                             NOT NULL,
                {Event.EffectiveVersion}      INTEGER                                 NOT NULL,
                {Event.TargetEvent}           {DB2GuidType}                           NULL,
                {Event.RefactoringType}       SMALLINT                                NULL,                

                PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),

                CONSTRAINT {Event.TableName}_Unique_{Event.EventId}        UNIQUE ( {Event.EventId} ),
                CONSTRAINT {Event.TableName}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),
                CONSTRAINT {Event.TableName}_Unique_{Event.ReadOrder}      UNIQUE ( {Event.ReadOrderIntegerPart}, {Event.ReadOrderFractionPart} )
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {Event.TableName} ADD CONSTRAINT FK_{Event.TargetEvent} 
                FOREIGN KEY ( {Event.TargetEvent} ) REFERENCES {Event.TableName} ({Event.EventId})';
        
        EXECUTE IMMEDIATE '
            CREATE INDEX IX_{Event.TableName}_{Event.ReadOrder} ON {Event.TableName} 
                ({Event.ReadOrderIntegerPart} ASC, {Event.ReadOrderFractionPart} ASC, {Event.EffectiveVersion} ASC)';


        EXECUTE IMMEDIATE '
            CREATE TABLE {Lock.TableName}
            (
                {Lock.AggregateId} {DB2GuidType} NOT NULL,

                CONSTRAINT PK_AggregateLock PRIMARY KEY ({Lock.AggregateId})
            )';

end
")
                                                                .ExecuteNonQuery());

                _initialized = true;
            }
        });
    }
}
