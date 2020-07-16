using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.System.Transactions;
using Event=Composable.Persistence.Common.EventStore.EventTable.Columns;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTable;

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

            CREATE TABLE {EventTable.Name}
            (
                {Event.InsertionOrder}       NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL ,
                {Event.AggregateId}          {DB2GuidType}                        NOT NULL,  
                {Event.UtcTimeStamp}         TIMESTAMP(7)                            NOT NULL,   
                {Event.EventType}            {DB2GuidType}                        NOT NULL,    
                {Event.Event}                NCLOB                                   NOT NULL,
                {Event.EventId}              {DB2GuidType}                        NOT NULL,
                {Event.InsertedVersion}      NUMBER(10)                              NOT NULL,
                {Event.SqlInsertTimeStamp}   TIMESTAMP(9) default CURRENT_TIMESTAMP  NOT NULL,
                {Event.ReadOrder}            {EventTable.ReadOrderType}              NOT NULL,
                {Event.EffectiveVersion}     NUMBER(10)                              NOT NULL,
                {Event.TargetEvent}          {DB2GuidType}                        NULL,
                {Event.RefactoringType}      NUMBER(3)                               NULL,                

                PRIMARY KEY ({Event.AggregateId}, {Event.InsertedVersion}),

                CONSTRAINT {EventTable.Name}_Unique_{Event.EventId}        UNIQUE ( {Event.EventId} ),
                CONSTRAINT {EventTable.Name}_Unique_{Event.InsertionOrder} UNIQUE ( {Event.InsertionOrder} ),
                CONSTRAINT {EventTable.Name}_Unique_{Event.ReadOrder}      UNIQUE ( {Event.ReadOrder} )
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {EventTable.Name} ADD CONSTRAINT FK_{Event.TargetEvent} 
                FOREIGN KEY ( {Event.TargetEvent} ) REFERENCES {EventTable.Name} ({Event.EventId})';
        
        EXECUTE IMMEDIATE '
            CREATE INDEX IX_{EventTable.Name}_{Event.ReadOrder} ON {EventTable.Name} 
                ({Event.ReadOrder} ASC, {Event.EffectiveVersion} ASC)';


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
