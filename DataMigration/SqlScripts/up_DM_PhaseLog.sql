SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create date: Feb. 16, 2014

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'up_DM_PhaseLog')
DROP PROCEDURE [dbo].[up_DM_PhaseLog]
GO

CREATE PROCEDURE [dbo].[up_DM_PhaseLog]
@LogSource int,
@DataSourceCode nchar(3),
@LoadNumber int,
@Phase int,
@NumberOfRecords int,
@Description nvarchar(300)
AS
BEGIN
    SET NOCOUNT ON;

    insert into [dbo].[DataMigrationPhaseLog](LogSource, DataSourceCode, LoadNumber, Phase, NumberOfRecords, Description)
    VALUES(@LogSource, @DataSourceCode, @LoadNumber, @Phase, @NumberOfRecords, @Description)
END
GO

GRANT EXEC ON [dbo].[up_DM_PhaseLog] TO PUBLIC
GO

