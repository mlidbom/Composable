namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventTableSchemaManager : SqlServerTableSchemaManager
    {
        internal override string Name { get; } = SqlServerEventTable.Name;
        //Urgent: Consider changing the event ordering scheme. SqlDecimal is not portable and quite quirky to use. https://github.com/mlidbom/Composable/issues/46
        //Urgent: Computed columns is not portable and are used here to implement policy level logic that we do not want in the individual persistence layers.
        internal override string CreateTableSql => $@"
CREATE TABLE dbo.{Name}(
    {SqlServerEventTable.Columns.InsertionOrder} bigint IDENTITY(1,1) NOT NULL,
    {SqlServerEventTable.Columns.AggregateId} uniqueidentifier NOT NULL,
    {SqlServerEventTable.Columns.EffectiveVersion} as case 
        when {SqlServerEventTable.Columns.ManualVersion} is not null then {SqlServerEventTable.Columns.ManualVersion}
        when {SqlServerEventTable.Columns.InsertAfter} is null and {SqlServerEventTable.Columns.InsertBefore} is null and {SqlServerEventTable.Columns.Replaces} is null then {SqlServerEventTable.Columns.InsertedVersion}
        else null
    end,    
    {SqlServerEventTable.Columns.UtcTimeStamp} datetime2 NOT NULL,   
    {SqlServerEventTable.Columns.EventType} uniqueidentifier NOT NULL,    
    {SqlServerEventTable.Columns.Event} nvarchar(max) NOT NULL,
    {SqlServerEventTable.Columns.EffectiveReadOrder} as case 
        when {SqlServerEventTable.Columns.ManualReadOrder} is not null then {SqlServerEventTable.Columns.ManualReadOrder}
        when {SqlServerEventTable.Columns.InsertAfter} is null and {SqlServerEventTable.Columns.InsertBefore} is null and {SqlServerEventTable.Columns.Replaces} is null then cast({SqlServerEventTable.Columns.InsertionOrder} as {SqlServerEventTable.ReadOrderType})
        else null
    end,
    {SqlServerEventTable.Columns.EventId} uniqueidentifier NOT NULL,
    {SqlServerEventTable.Columns.InsertedVersion} int NOT NULL,
    {SqlServerEventTable.Columns.SqlInsertTimeStamp} datetime2 default SYSUTCDATETIME(),
    {SqlServerEventTable.Columns.InsertAfter} uniqueidentifier null,
    {SqlServerEventTable.Columns.InsertBefore} uniqueidentifier null,
    {SqlServerEventTable.Columns.Replaces} uniqueidentifier null,
    {SqlServerEventTable.Columns.ManualReadOrder} {SqlServerEventTable.ReadOrderType} null,    
    {SqlServerEventTable.Columns.ManualVersion} int NULL,

    CONSTRAINT PK_{Name} PRIMARY KEY CLUSTERED 
    (
        {SqlServerEventTable.Columns.AggregateId} ASC,
        {SqlServerEventTable.Columns.InsertedVersion} ASC
    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

    CONSTRAINT IX_{Name}_Unique_{SqlServerEventTable.Columns.EventId} UNIQUE
    (
        {SqlServerEventTable.Columns.EventId}
    ),

    CONSTRAINT IX_{Name}_Unique_{SqlServerEventTable.Columns.InsertionOrder} UNIQUE
    (
        {SqlServerEventTable.Columns.InsertionOrder}
    ),

    CONSTRAINT CK_{Name}_Only_one_reordering_column_allowed_for_use
    CHECK 
    (
        ({SqlServerEventTable.Columns.InsertAfter} is null and {SqlServerEventTable.Columns.InsertBefore} is null)
        or
        ({SqlServerEventTable.Columns.InsertAfter} is null and {SqlServerEventTable.Columns.Replaces} is null)
        or
        ({SqlServerEventTable.Columns.InsertBefore} is null and {SqlServerEventTable.Columns.Replaces} is null) 
    ),

    CONSTRAINT FK_{Name}_{SqlServerEventTable.Columns.Replaces} FOREIGN KEY ( {SqlServerEventTable.Columns.Replaces} ) 
        REFERENCES {Name} ({SqlServerEventTable.Columns.EventId}),

    CONSTRAINT FK_{Name}_{SqlServerEventTable.Columns.InsertBefore} FOREIGN KEY ( {SqlServerEventTable.Columns.InsertBefore} )
        REFERENCES {Name} ({SqlServerEventTable.Columns.EventId}),

    CONSTRAINT FK_{Name}_{SqlServerEventTable.Columns.InsertAfter} FOREIGN KEY ( {SqlServerEventTable.Columns.InsertAfter} ) 
        REFERENCES {Name} ({SqlServerEventTable.Columns.EventId}) 
)

    CREATE NONCLUSTERED INDEX IX_{Name}_{SqlServerEventTable.Columns.EffectiveReadOrder} ON dbo.{Name}
        ({SqlServerEventTable.Columns.EffectiveReadOrder}, {SqlServerEventTable.Columns.EffectiveVersion})
        INCLUDE ({SqlServerEventTable.Columns.EventType}, {SqlServerEventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{SqlServerEventTable.Columns.Replaces}	ON dbo.{Name}
        ({SqlServerEventTable.Columns.Replaces})
        INCLUDE ({SqlServerEventTable.Columns.EventId})

    CREATE NONCLUSTERED INDEX IX_{Name}_{SqlServerEventTable.Columns.InsertAfter}	ON dbo.{Name}
        ({SqlServerEventTable.Columns.InsertAfter})
        INCLUDE ({SqlServerEventTable.Columns.EventId})

    CREATE NONCLUSTERED INDEX IX_{Name}_{SqlServerEventTable.Columns.InsertBefore}	ON dbo.{Name} 
        ({SqlServerEventTable.Columns.InsertBefore})
        INCLUDE ({SqlServerEventTable.Columns.EventId})

    CREATE NONCLUSTERED INDEX IX_{Name}_{SqlServerEventTable.Columns.EffectiveVersion}	ON dbo.{Name} 
        ({SqlServerEventTable.Columns.EffectiveVersion})
";

    }
}