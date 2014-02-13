CREATE TABLE [dbo].[ValueType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ValueType] [varchar](500) NOT NULL,
 CONSTRAINT [PK_ValueType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
--Create Keys
INSERT INTO ValueType(ValueType) 
SELECT DISTINCT ValueType FROM Store;
--Add column
ALTER TABLE Store
ADD ValueTypeId int

GO

--Insert Keys
UPDATE Store
SET ValueTypeId = (SELECT Id FROM ValueType WHERE ValueType = Store.ValueType)
--Add Constraints
ALTER TABLE [dbo].[Store]
ALTER COLUMN [ValueTypeId] INT NOT NULL

ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_ValueType_Store] FOREIGN KEY([ValueTypeId])
REFERENCES [dbo].[ValueType] ([Id])

ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_ValueType_Store]
--Change PK
ALTER TABLE [dbo].[Store]
DROP CONSTRAINT PK_Store

DROP INDEX IX_ValueType
ON [dbo].[Store]

ALTER TABLE [dbo].[Store] 
ADD CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC,
	[ValueTypeId] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
--Drop column
ALTER TABLE [dbo].[Store]
DROP COLUMN ValueType