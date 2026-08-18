/* =====================================================================
   INSTITUTE SELF-REGISTRATION + ADMIN APPROVAL
   ===================================================================== */

USE StudentRegistrationDB;
GO

/* ---------------------------------------------------------------
   1. Add self-registration columns if they don't already exist
   --------------------------------------------------------------- */

IF COL_LENGTH('dbo.Institutes', 'Capacity') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD Capacity INT NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'Address') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD Address NVARCHAR(300) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'City') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD City NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'ContactEmail') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD ContactEmail NVARCHAR(150) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'ContactPhone') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD ContactPhone NVARCHAR(25) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'Website') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD Website NVARCHAR(200) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'CoursesOffered') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD CoursesOffered NVARCHAR(1000) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'ApprovalStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD ApprovalStatus NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'RejectionRemark') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD RejectionRemark NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'SubmittedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD SubmittedDate DATETIME NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'ApprovedBy') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD ApprovedBy NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'ApprovedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD ApprovedDate DATETIME NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'RejectedBy') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD RejectedBy NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.Institutes', 'RejectedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Institutes
    ADD RejectedDate DATETIME NULL;
END
GO


/* ---------------------------------------------------------------
   2. Give existing institutes their default approval information
   --------------------------------------------------------------- */

UPDATE dbo.Institutes
SET
    ApprovalStatus = ISNULL(ApprovalStatus, N'Approved'),
    ApprovedBy = ISNULL(ApprovedBy, N'System (pre-existing)'),
    ApprovedDate = ISNULL(ApprovedDate, GETDATE()),
    SubmittedDate = ISNULL(SubmittedDate, GETDATE());
GO


/* ---------------------------------------------------------------
   3. Add approval-status validation
   --------------------------------------------------------------- */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Institutes_ApprovalStatus'
      AND parent_object_id = OBJECT_ID('dbo.Institutes')
)
BEGIN
    ALTER TABLE dbo.Institutes
    ADD CONSTRAINT CK_Institutes_ApprovalStatus
    CHECK (ApprovalStatus IN (N'Pending', N'Approved', N'Rejected'));
END
GO


PRINT 'Institute self-registration and approval workflow setup completed successfully.';
GO