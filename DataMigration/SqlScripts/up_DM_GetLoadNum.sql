SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create date: Feb. 16, 2014

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'up_DM_GetLoadNum')
DROP PROCEDURE [dbo].[up_DM_GetLoadNum]
GO

CREATE PROCEDURE [dbo].[up_DM_GetLoadNum]
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON;
    insert dbo.LoadNum DEFAULT VALUES;
    SELECT SCOPE_IDENTITY() as 'LoadNum'
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

GRANT EXEC ON [dbo].[up_DM_GetLoadNum] TO PUBLIC
GO


