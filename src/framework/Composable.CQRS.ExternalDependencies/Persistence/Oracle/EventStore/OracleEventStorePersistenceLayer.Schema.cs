using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System.Transactions;
using C=Composable.Persistence.Common.EventStore.EventTable.Columns;

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
    if (existing_table_count <= 0) then
        EXECUTE IMMEDIATE '
            CREATE TABLE {EventTable.Name}(
                {C.InsertionOrder} NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL,
                {C.AggregateId} {OracleGuidType} NOT NULL,  
                {C.UtcTimeStamp} TIMESTAMP(7) NOT NULL,   
                {C.EventType} {OracleGuidType} NOT NULL,    
                {C.Event} NCLOB NOT NULL,
                {C.EventId} {OracleGuidType} NOT NULL,
                {C.InsertedVersion} NUMBER(10) NOT NULL,
                {C.SqlInsertTimeStamp} TIMESTAMP(9) default CURRENT_TIMESTAMP NOT NULL,
                {C.TargetEvent} {OracleGuidType} null,
                {C.RefactoringType} NUMBER(3) null,
                {C.ReadOrder} NUMBER(19) null,
                {C.ReadOrderOrderOffset} NUMBER(19) null,
                {C.EffectiveOrder} {EventTable.ReadOrderType} null,    
                {C.EffectiveVersion} NUMBER(10) NULL,

                CONSTRAINT {EventTable.Name}_PK PRIMARY KEY ({C.AggregateId}, {C.InsertedVersion}),

                CONSTRAINT {EventTable.Name}_Unique_{C.EventId} UNIQUE ( {C.EventId} ),
                CONSTRAINT {EventTable.Name}_Unique_{C.InsertionOrder} UNIQUE ( {C.InsertionOrder} )
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {EventTable.Name} ADD CONSTRAINT FK_{C.TargetEvent} 
                FOREIGN KEY ( {C.TargetEvent} ) REFERENCES {EventTable.Name} ({C.EventId})';     
        
        EXECUTE IMMEDIATE '
            CREATE INDEX IX_{EventTable.Name}_{C.EffectiveOrder} ON {EventTable.Name} 
                ({C.EffectiveOrder} ASC, {C.EffectiveVersion} ASC)';

    end if;
end;
")
                                                                .ExecuteNonQuery());

                _initialized = true;
            }
        });
    }
}
