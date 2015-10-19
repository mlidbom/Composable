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

        public void LogWarningIfUsingLegacySqlSchema(SqlConnection connection)
        {
            //todo: restore this when the schema is finalized and the warning is correct.
            if (false && IsUsingLegacySchema(connection))
            {
                this.Log().Warn(
                    $@"
/*Database is using a legacy schema. You need to migrate your data into the new schema.
Paste this whole log mesage into a sql management studio window and it will uppgrade the database for you
1: Create new tables: */

USE {connection.Database}

GO

BEGIN TRANSACTION

{EventTypeTableSchema.CreateTableSql}

GO

{EventTableSchema.CreateTableSql}

ALTER TABLE {EventTable.Name} 
Add {LegacySqlTimeStamp} Bigint NULL

GO

{InsertEventTypesSql}

GO

{MigrateEventsSql}

GO

DROP TABLE {Name}

COMMIT TRANSACTION

GO

DBCC SHRINKDATABASE ( {connection.Database} )

");
            }
        }

        public bool IsUsingLegacySchema(SqlConnection connection) { return Exists(connection); }

        public string InsertEventTypesSql => $@"
INSERT INTO {EventTypeTable.Name} ( {EventTypeTable.Columns.EventType} )
SELECT {LegacyEventTable.Columns.EventType} 
FROM {LegacyEventTable.Name}
GROUP BY {EventTable.Columns.EventType} 
";

        public string MigrateEventsSql => $@"
INSERT INTO {EventTableSchema.Name} 
(      {EventTable.Columns.AggregateId}, {EventTable.Columns.AggregateVersion}, {EventTable.Columns.TimeStamp}, {EventTable.Columns.EventType}, {EventTable.Columns.EventId}, {EventTable.Columns.Event}, {LegacySqlTimeStamp}, {EventTable.Columns.SqlTimeStamp})
SELECT {LegacyEventTable.Columns.AggregateId}, {LegacyEventTable.Columns.AggregateVersion},{LegacyEventTable.Columns.TimeStamp}, {EventTypeTable.Name}.{EventTypeTable.Columns.Id}, {LegacyEventTable.Columns.EventId}, {LegacyEventTable.Columns.Event}, CAST({LegacyEventTable.Columns.SqlTimeStamp} AS BIGINT), {EventTable.Columns.TimeStamp}
FROM {LegacyEventTable.Name}
INNER JOIN {EventTypeTable.Name}
ON {LegacyEventTable.Name}.{LegacyEventTable.Columns.EventType} = {EventTypeTable.Name}.{EventTypeTable.Columns.EventType}
ORDER BY {LegacyEventTable.Columns.SqlTimeStamp} ASC
";

        private static readonly LegacyEventTableSchemaManager LegacyEventTableSchema = new LegacyEventTableSchemaManager();
        private static readonly EventTableSchemaManager EventTableSchema = new EventTableSchemaManager();
        private static readonly EventTypeTableSchemaManager EventTypeTableSchema = new EventTypeTableSchemaManager();
    }
}