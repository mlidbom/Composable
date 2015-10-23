
CREATE TABLE [dbo].[Events_Temp](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[AggregateVersion] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[SqlTimeStamp] [timestamp] NOT NULL,
	[EventType] [varchar](300) NOT NULL,
	[EventId] [uniqueidentifier] NOT NULL,
	[Event] [nvarchar](max) NOT NULL,
	[InsertionOrder] [bigint] IDENTITY NOT NULL,
 CONSTRAINT [PK_Events_Temp] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[AggregateVersion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = OFF, FILLFACTOR = 85) ON [PRIMARY],
 CONSTRAINT [IX_Uniq_EventId_Temp] UNIQUE NONCLUSTERED 
(
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 85) ON [PRIMARY],
CONSTRAINT [IX_Uniq_InsertionOrder_Temp] UNIQUE NONCLUSTERED 
(
	[InsertionOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 85) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

CREATE TABLE [dbo].[EventInsertionOrder](
	[EventId] [uniqueidentifier] NOT NULL,
	[InsertionOrder] [bigint] NOT NULL,
 CONSTRAINT [PK_EventInsertionOrder] PRIMARY KEY CLUSTERED 
(
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

INSERT EventInsertionOrder(EventId, InsertionOrder)
SELECT EventId, 
ROW_NUMBER() OVER(ORDER BY SqlTimeStamp ASC) AS InsertionOrder
FROM Events 
ORDER BY SqlTimestamp ASC

GO

ALTER TABLE Events 
Add InsertionOrder bigint null

GO

UPDATE Events
SET InsertionOrder = EventInsertionOrder.InsertionOrder
FROM Events
INNER JOIN EventInsertionOrder 
	ON EventInsertionOrder.EventId = Events.EventId

GO

ALTER TABLE Events 
ALTER COLUMN InsertionOrder bigint not null

GO

ALTER TABLE Events 
ADD CONSTRAINT [IX_Uniq_InsertionOrder] UNIQUE NONCLUSTERED 
(
	[InsertionOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 85) ON [PRIMARY]

GO 

ALTER TABLE Events SWITCH TO Events_Temp



GO

EXEC sp_rename 'Events' ,'Events_old'
EXEC sp_rename 'PK_Events' ,'PK_Events_old'
EXEC sp_rename 'IX_Uniq_EventId', 'IX_Uniq_EventId_old'
EXEC sp_rename 'IX_Uniq_InsertionOrder' ,'IX_Uniq_InsertionOrder_old'

EXEC sp_rename 'Events_Temp' ,'Events'
EXEC sp_rename 'PK_Events_Temp' ,'PK_Events'
EXEC sp_rename 'IX_Uniq_EventId_Temp', 'IX_Uniq_EventId'
EXEC sp_rename 'IX_Uniq_InsertionOrder_Temp' ,'IX_Uniq_InsertionOrder'