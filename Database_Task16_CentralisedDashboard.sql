/* =====================================================================
   TASK 16: CENTRALISED INSTITUTE DASHBOARD
   -----------------------------------------------------------------------
   Appends to Database.sql. Guarded with IF OBJECT_ID(...) IS NULL, matching
   every earlier migration file, so this is safe to run against the existing
   StudentRegistrationDB at any time.

   The two tables below (instreg, modules) use the EXACT names/columns
   mandated by the Task 16 brief (email, 17 Aug 2026). Column names are kept
   lowercase, as specified in the brief, unlike the PascalCase used elsewhere
   in this schema.

   instreg.instname is also used to link a dashboard login to the existing
   dbo.Institutes lookup (introduced in Task 12) purely by name match, so the
   module detail pages can show real, institute-specific data pulled from the
   Student Management, Fees Management and Timetable Management tables that
   already exist in this database - nothing on the dashboard is hardcoded.

   Run this whole file against StudentRegistrationDB after Database.sql,
   Database_Task12_RegistrationFee.sql and Database_Task15_Timetable.sql
   have already been applied.
   ===================================================================== */

USE StudentRegistrationDB;
GO

/* ---------------------------------------------------------------------
   1. INSTITUTE REGISTRATION (login credentials)
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.instreg', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.instreg
    (
        srno     INT IDENTITY(1,1) PRIMARY KEY,
        instid   NVARCHAR(50)  NOT NULL,
        pwd      NVARCHAR(200) NOT NULL,   -- SHA-256 hash (see InstituteAuth.cs), never plain text
        instname NVARCHAR(150) NOT NULL,
        status   NVARCHAR(20)  NOT NULL DEFAULT (N'Active')
    );
    CREATE UNIQUE INDEX IX_instreg_instid ON dbo.instreg(instid);

    -- Seed logins for the two institutes already used by Task 12/15 sample data, so the
    -- dashboard shows real, non-empty data out of the box.
    -- Password for both seeded logins is: Inst@123
    -- (SHA-256 hex, uppercase - see InstituteAuth.ComputeHash, reused from AdminAuth.ComputeHash)
    INSERT INTO dbo.instreg (instid, pwd, instname, status) VALUES
        (N'INST001', N'2941AAABD0E2E7F27CCA113B9770855836DEBE9E4BAA3E78CB33A6BB89C07B2B',
         N'Government College of Engineering, Kolhapur', N'Active'),
        (N'INST002', N'2941AAABD0E2E7F27CCA113B9770855836DEBE9E4BAA3E78CB33A6BB89C07B2B',
         N'Government Polytechnic, Kolhapur', N'Active');
    PRINT 'Task 16: instreg created and seeded.';
END
GO

/* ---------------------------------------------------------------------
   2. ACTIVE MODULES (per-institute module assignment)
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.modules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.modules
    (
        srno       INT IDENTITY(1,1) PRIMARY KEY,
        instid     NVARCHAR(50)  NOT NULL,
        modulename NVARCHAR(100) NOT NULL,
        status     NVARCHAR(20)  NOT NULL DEFAULT (N'Active')
    );
    CREATE INDEX IX_modules_instid ON dbo.modules(instid);

    -- INST001: all three modules active.
    INSERT INTO dbo.modules (instid, modulename, status) VALUES
        (N'INST001', N'Student Management',   N'Active'),
        (N'INST001', N'Fees Management',      N'Active'),
        (N'INST001', N'Timetable Management', N'Active');

    -- INST002: one module deliberately Inactive, to demonstrate that inactive
    -- modules are correctly hidden from the dashboard (mandatory requirement).
    INSERT INTO dbo.modules (instid, modulename, status) VALUES
        (N'INST002', N'Student Management',   N'Active'),
        (N'INST002', N'Fees Management',      N'Inactive'),
        (N'INST002', N'Timetable Management', N'Active');

    PRINT 'Task 16: modules created and seeded.';
END
GO
