SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'LoadNum' AND xtype = 'U')
BEGIN
CREATE TABLE [dbo].[LoadNum](
    [LoadNum] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [DateInserted] [datetime] NOT NULL DEFAULT (GETDATE())
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[BadRows]') AND type in (N'U'))

BEGIN
CREATE TABLE [dbo].[BadRows](
    [LoadNum] [int] NOT NULL,
    [DataSourceCode] [char](30) NOT NULL,
    [RowNumber] [int] NOT NULL,
    [DestColumn] [nvarchar] (50) NOT NULL,
    [Reason] [nvarchar] (500) NULL,
    [ForeignId] [nvarchar] (100) NULL,
    [RowData] [xml] NULL
)
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[WarnRows]') AND type in (N'U'))

BEGIN
CREATE TABLE [dbo].[WarnRows](
    [LoadNum] [int] NOT NULL,
    [DataSourceCode] [char](30) NOT NULL,
    [RowNumber] [int] NOT NULL,
    [DestColumn] [nvarchar] (50) NOT NULL,
    [Reason] [nvarchar] (500) NULL,
    [ForeignId] [nvarchar] (100) NULL,
    [RowData] [xml] NULL
)
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[DataMigrationPhaseLog]') AND type in (N'U'))

CREATE TABLE [dbo].[DataMigrationPhaseLog](
    [RecordDate] [datetime] NOT NULL DEFAULT GetDate(),
    [LogSource] [int] NOT NULL,
    [DataSourceCode] [char] (30) NOT NULL,
    [LoadNumber] [int] NULL,
    [Phase] [int] NULL,
    [NumberOfRecords] [int] NULL,
    [Description] [nvarchar] (300) NOT NULL
) ON [PRIMARY]
GO


