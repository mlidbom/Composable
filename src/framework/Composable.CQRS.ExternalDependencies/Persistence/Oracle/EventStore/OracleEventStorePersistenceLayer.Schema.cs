using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.SystemCE.TransactionsCE;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.Oracle.EventStore
{
    partial class OracleEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        const string OracleGuidType = "CHAR(36)";
        bool _initialized;

        public void SetupSchemaIfDatabaseUnInitialized() => TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
        {
            if(!_initialized)
            {
                _connectionManager.UseCommand(command => command.SetCommandText($@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='{Event.TableName.ToUpperInvariant()}';
    if (existing_table_count = 0) then
        EXECUTE IMMEDIATE '

            CREATE TABLE {Event.TableName}
            (
                {Event.InsertionOrder}       NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL ,
                {Event.AggregateId}          {OracleGuidType}                        NOT NULL,  
                {Event.UtcTimeStamp}         TIMESTAMP(7)                            NOT NULL,   
                {Event.EventType}            {OracleGuidType}                        NOT NULL,    
                {Event.Event}                NCLOB                                   NOT NULL,
                {Event.EventId}              {OracleGuidType}                        NOT NULL,
                {Event.InsertedVersion}      NUMBER(10)                              NOT NULL,
                {Event.SqlInsertTimeStamp}   TIMESTAMP(9) default CURRENT_TIMESTAMP  NOT NULL,
                {Event.ReadOrder}            {Event.ReadOrderType}                   NOT NULL,
                {Event.EffectiveVersion}     NUMBER(10)                              NOT NULL,
                {Event.TargetEvent}          {OracleGuidType}                        NULL,
                {Event.RefactoringType}      NUMBER(3)                               NULL,                

                CONSTRAINT {Event.TableName}_PK PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),

                CONSTRAINT {Event.TableName}_Unique_{Event.EventId}        UNIQUE ( {Event.EventId} ),
                CONSTRAINT {Event.TableName}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),
                CONSTRAINT {Event.TableName}_Unique_{Event.ReadOrder}      UNIQUE ( {Event.ReadOrder} )
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {Event.TableName} ADD CONSTRAINT FK_{Event.TargetEvent} 
                FOREIGN KEY ( {Event.TargetEvent} ) REFERENCES {Event.TableName} ({Event.EventId})';
        
        EXECUTE IMMEDIATE '
            CREATE INDEX IX_{Event.TableName}_{Event.ReadOrder} ON {Event.TableName} 
                ({Event.ReadOrder} ASC, {Event.EffectiveVersion} ASC)';


        EXECUTE IMMEDIATE '
            CREATE TABLE {Lock.TableName}
            (
                {Lock.AggregateId} {OracleGuidType} NOT NULL,

                CONSTRAINT PK_AggregateLock PRIMARY KEY ({Lock.AggregateId})
            )';

    end if;
end;
")
                                                                .ExecuteNonQuery());

                _initialized = true;
            }
        });
    }
}
