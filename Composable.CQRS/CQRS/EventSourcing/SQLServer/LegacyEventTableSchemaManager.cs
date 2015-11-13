using System;
using System.Data.SqlClient;
using Composable.Logging.Log4Net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class LegacyEventTableSchemaManager : TableSchemaManager
    {
        private static readonly string LegacySqlTimeStamp = nameof(LegacySqlTimeStamp);

        override public string Name { get; } = LegacyEventTable.Name;
        override public string CreateTableSql { get { throw new NotImplementedException(); } }

        private static readonly EventTableSchemaManager EventTableSchema = new EventTableSchemaManager();
        private static readonly EventTypeTableSchemaManager EventTypeTableSchema = new EventTypeTableSchemaManager();
        public bool IsUsingLegacySchema(SqlConnection connection) { return Exists(connection); }



        public void LogAndThrowIfUsingLegacySchema(SqlConnection connection)
        {
            //todo: restore this when the schema is finalized and the warning is correct.
            if (IsUsingLegacySchema(connection))
            {
                PrintMigrationScriptToLog(connection);
                throw new Exception($"--Using unsupported legacy event schema. See log for migration instructions or below here: \n\n{CreateMigrationScript(connection)}");
            }
        }


        public void PrintMigrationScriptToLog(SqlConnection connection)
        {
            //References to read for more efficient ways of doing this:
            //https://msdn.microsoft.com/en-us/library/ms190273.aspx (SWITCH section)
            //http://www.sqlservercentral.com/articles/T-SQL/61979/
            //http://stackoverflow.com/questions/1049210/adding-an-identity-to-an-existing-column
            //http://blog.sqlauthority.com/2009/05/03/sql-server-add-or-remove-identity-property-on-column/
            //https://technet.microsoft.com/en-us/library/ms176057.aspx
            //https://social.msdn.microsoft.com/Forums/sqlserver/en-US/04d69ee6-d4f5-4f8f-a115-d89f7bcbc032/how-to-alter-column-to-identity11?forum=transactsql
            //http://blogs.msdn.com/b/dfurman/archive/2010/04/20/adding-the-identity-property-to-a-column-of-an-existing-table.aspx
            this.Log().Error(CreateMigrationScript(connection));
        }
        private string CreateMigrationScript(SqlConnection connection) {
            return $@"
/*Database is using a legacy schema. You need to migrate your data into the new schema.
Paste this whole log mesage into a sql management studio window and it will uppgrade the database for you
1: Create new tables: */

USE {connection.Database}

GO

BEGIN TRANSACTION

{ActualMigrationScript}


COMMIT TRANSACTION

GO

DBCC SHRINKDATABASE ( {connection.Database} )

";
        }

        public string ActualMigrationScript => $@"

{EventTypeTableSchema.CreateTableSql}

GO

{EventTableSchema.CreateTableSql}

GO

ALTER TABLE {EventTable.Name} 
Add {LegacySqlTimeStamp} Bigint NULL

GO

{InsertEventTypesSql}

GO

{MigrateEventsSql}

GO

--DROP TABLE {Name}
";

        public string InsertEventTypesSql => $@"
INSERT INTO {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} )
SELECT {LegacyEventTable.Columns.EventType} 
FROM {LegacyEventTable.Name}
GROUP BY {EventTable.Columns.EventType} 
";

        public string MigrateEventsSql => $@"
INSERT INTO {EventTableSchema.Name} 
(      {EventTable.Columns.AggregateId}, {EventTable.Columns.InsertedVersion}, {EventTable.Columns.TimeStamp}, {EventTable.Columns.EventType}, {EventTable.Columns.EventId}, {EventTable.Columns.Event}, {LegacySqlTimeStamp}, {EventTable.Columns.SqlInsertDateTime})
SELECT {LegacyEventTable.Columns.AggregateId}, {LegacyEventTable.Columns.AggregateVersion},{LegacyEventTable.Columns.TimeStamp}, {EventTypeTable.Name}.{EventTypeTable.Columns.Id}, {LegacyEventTable.Columns.EventId}, {LegacyEventTable.Columns.Event}, CAST({LegacyEventTable.Columns.SqlTimeStamp} AS BIGINT), {EventTable.Columns.TimeStamp}
FROM {LegacyEventTable.Name}
INNER JOIN {EventTypeTable.Name}
ON {LegacyEventTable.Name}.{LegacyEventTable.Columns.EventType} = {EventTypeTable.Name}.{EventTypeTable.Columns.EventType}
ORDER BY {LegacyEventTable.Columns.SqlTimeStamp} ASC
";


    }
}