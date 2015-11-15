namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTableSchemaManager : TableSchemaManager
    {
        override public string Name { get; } = EventTable.Name;

        override public string CreateTableSql => $@"
CREATE TABLE dbo.{Name}(
    {EventTable.Columns.InsertionOrder} bigint IDENTITY(1,1) NOT NULL,
    {EventTable.Columns.AggregateId} uniqueidentifier NOT NULL,
    {EventTable.Columns.EffectiveVersion} as case 
        when {EventTable.Columns.ManualVersion} is not null then {EventTable.Columns.ManualVersion}
        when {EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null then {EventTable.Columns.InsertedVersion}
        else null
    end,    
    {EventTable.Columns.TimeStamp} datetime NOT NULL,    
    {EventTable.Columns.EventType} int NOT NULL,    
    {EventTable.Columns.Event} nvarchar(max) NOT NULL,
    {EventTable.Columns.EffectiveReadOrder} as case 
        when {EventTable.Columns.ManualReadOrder} is not null then {EventTable.Columns.ManualReadOrder}
        when {EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null then cast({EventTable.Columns.InsertionOrder} as {EventTable.ReadOrderType})
        else null
    end,
    {EventTable.Columns.EventId} uniqueidentifier NOT NULL,
    {EventTable.Columns.InsertedVersion} int NOT NULL,
    {EventTable.Columns.SqlInsertDateTime} datetime2 default SYSUTCDATETIME(),
    {EventTable.Columns.InsertAfter} bigint null,
    {EventTable.Columns.InsertBefore} bigint null,
    {EventTable.Columns.Replaces} bigint null,
    {EventTable.Columns.ManualReadOrder} {EventTable.ReadOrderType} null,    
    {EventTable.Columns.ManualVersion} int NULL,

    CONSTRAINT PK_{Name} PRIMARY KEY CLUSTERED 
    (
        {EventTable.Columns.AggregateId} ASC,
        {EventTable.Columns.InsertedVersion} ASC
    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

    CONSTRAINT IX_{Name}_Unique_{EventTable.Columns.EventId} UNIQUE
    (
        {EventTable.Columns.EventId}
    ),

    CONSTRAINT IX_{Name}_Unique_{EventTable.Columns.InsertionOrder} UNIQUE
    (
        {EventTable.Columns.InsertionOrder}
    ),

    CONSTRAINT CK_{Name}_Only_one_reordering_column_allowed_for_use
    CHECK 
    (
        ({EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null)
        or
        ({EventTable.Columns.InsertAfter} is null and {EventTable.Columns.Replaces} is null)
        or
        ({EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null) 
    ),

    CONSTRAINT FK_{Name}_{EventTable.Columns.EventType} FOREIGN KEY ({EventTable.Columns.EventType}) 
        REFERENCES {EventTypeTable.Name} ({EventTypeTable.Columns.Id}),

    CONSTRAINT FK_{Name}_{EventTable.Columns.Replaces} FOREIGN KEY ( {EventTable.Columns.Replaces} ) 
        REFERENCES {Name} ({EventTable.Columns.InsertionOrder}),

    CONSTRAINT FK_{Name}_{EventTable.Columns.InsertBefore} FOREIGN KEY ( {EventTable.Columns.InsertBefore} )
        REFERENCES {Name} ({EventTable.Columns.InsertionOrder}),

    CONSTRAINT FK_{Name}_{EventTable.Columns.InsertAfter} FOREIGN KEY ( {EventTable.Columns.InsertAfter} ) 
        REFERENCES {Name} ({EventTable.Columns.InsertionOrder}) 
)

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.EffectiveReadOrder} ON dbo.{Name}
        ({EventTable.Columns.EffectiveReadOrder}, {EventTable.Columns.EffectiveVersion})
        INCLUDE ({EventTable.Columns.EventType}, {EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.Replaces}	ON dbo.{Name}
        ({EventTable.Columns.Replaces})
        INCLUDE ({EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.InsertAfter}	ON dbo.{Name}
        ({EventTable.Columns.InsertAfter})
        INCLUDE ({EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.InsertBefore}	ON dbo.{Name} 
        ({EventTable.Columns.InsertBefore})
        INCLUDE ({EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.EffectiveVersion}	ON dbo.{Name} 
        ({EventTable.Columns.EffectiveVersion})
";

    }
}