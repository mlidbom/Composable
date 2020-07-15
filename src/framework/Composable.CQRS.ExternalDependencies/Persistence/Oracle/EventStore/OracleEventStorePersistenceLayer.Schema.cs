using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System.Transactions;
using Event=Composable.Persistence.Common.EventStore.EventTable.Columns;

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
                //Urgent: EffectiveOrder should have a unique constraint for all persistence providers.
                _connectionManager.UseCommand(command => command.SetCommandText($@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='{EventTable.Name.ToUpper()}';
    if (existing_table_count = 0) then
        EXECUTE IMMEDIATE '

            CREATE TABLE {EventTable.Name}
            (
                {Event.InsertionOrder}       NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL ,
                {Event.AggregateId}          {OracleGuidType}                        NOT NULL,  
                {Event.UtcTimeStamp}         TIMESTAMP(7)                            NOT NULL,   
                {Event.EventType}            {OracleGuidType}                        NOT NULL,    
                {Event.Event}                NCLOB                                   NOT NULL,
                {Event.EventId}              {OracleGuidType}                        NOT NULL,
                {Event.InsertedVersion}      NUMBER(10)                              NOT NULL,
                {Event.SqlInsertTimeStamp}   TIMESTAMP(9) default CURRENT_TIMESTAMP  NOT NULL,
                {Event.TargetEvent}          {OracleGuidType}                        NULL,
                {Event.RefactoringType}      NUMBER(3)                               NULL,
                {Event.ReadOrder}            NUMBER(19)                              NULL,
                {Event.ReadOrderOrderOffset} NUMBER(19)                              NULL,
                {Event.EffectiveOrder}       {EventTable.ReadOrderType}              NULL,    
                {Event.EffectiveVersion}     NUMBER(10)                              NULL,

                CONSTRAINT {EventTable.Name}_PK PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),

                CONSTRAINT {EventTable.Name}_Unique_{Event.EventId} UNIQUE ( {Event.EventId} ),
                CONSTRAINT {EventTable.Name}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} )
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {EventTable.Name} ADD CONSTRAINT FK_{Event.TargetEvent} 
                FOREIGN KEY ( {Event.TargetEvent} ) REFERENCES {EventTable.Name} ({Event.EventId})';
        
        EXECUTE IMMEDIATE '
            CREATE INDEX IX_{EventTable.Name}_{Event.EffectiveOrder} ON {EventTable.Name} 
                ({Event.EffectiveOrder} ASC, {Event.EffectiveVersion} ASC)';

    end if;
end;
")
                                                                .ExecuteNonQuery());

                _initialized = true;
            }
        });
    }
}
