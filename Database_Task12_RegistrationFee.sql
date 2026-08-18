/* =====================================================================
   TASK 12: REGISTRATION FEE MANAGEMENT (Admin Panel)
   -----------------------------------------------------------------------
   Appends to Database.sql. Every block is guarded with IF OBJECT_ID(...)
   IS NULL, matching the Task 10 / Task 11 migration pattern, so this is
   safe to run against the existing StudentRegistrationDB at any time
   (fresh install or an already-running database).

   Run this whole file against StudentRegistrationDB after the base
   Database.sql (and its Task 7/10/11 blocks) have already been applied.
   ===================================================================== */

USE StudentRegistrationDB;
GO

/* ---------------------------------------------------------------------
   1. CONFIGURATION LOOKUPS
   Fee structures are configured against these six axes. Lookups keep the
   dropdowns and referential integrity clean without a full CRUD screen
   per lookup (seed/extend these lists directly via SQL as the institute's
   academic catalogue changes — infrequent, admin/DBA-level data).
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.AcademicYears', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicYears
    (
        AcademicYearID INT IDENTITY(1,1) PRIMARY KEY,
        YearLabel      NVARCHAR(20)  NOT NULL,   -- e.g. '2025-26'
        IsActive       BIT           NOT NULL DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_AcademicYears_Label ON dbo.AcademicYears(YearLabel);
    INSERT INTO dbo.AcademicYears (YearLabel) VALUES ('2024-25'), ('2025-26'), ('2026-27');
    PRINT 'Task 12: AcademicYears created and seeded.';
END
GO

IF OBJECT_ID('dbo.Institutes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Institutes
    (
        InstituteID   INT IDENTITY(1,1) PRIMARY KEY,
        InstituteName NVARCHAR(150) NOT NULL,
        IsActive      BIT           NOT NULL DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_Institutes_Name ON dbo.Institutes(InstituteName);
    INSERT INTO dbo.Institutes (InstituteName) VALUES
        (N'Government College of Engineering, Kolhapur'),
        (N'Government Polytechnic, Kolhapur');
    PRINT 'Task 12: Institutes created and seeded.';
END
GO

IF OBJECT_ID('dbo.Courses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Courses
    (
        CourseID     INT IDENTITY(1,1) PRIMARY KEY,
        CourseName   NVARCHAR(150) NOT NULL,
        InstituteID  INT NOT NULL FOREIGN KEY REFERENCES dbo.Institutes(InstituteID),
        IsActive     BIT NOT NULL DEFAULT (1)
    );
    CREATE INDEX IX_Courses_Institute ON dbo.Courses(InstituteID);
    INSERT INTO dbo.Courses (CourseName, InstituteID) VALUES
        (N'B.Tech Computer Science', 1),
        (N'B.Tech Electronics', 1),
        (N'B.Tech Mechanical', 1),
        (N'Diploma Computer Engineering', 2);
    PRINT 'Task 12: Courses created and seeded.';
END
GO

IF OBJECT_ID('dbo.StudentCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StudentCategories
    (
        StudentCategoryID INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName       NVARCHAR(50) NOT NULL,
        IsActive           BIT NOT NULL DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_StudentCategories_Name ON dbo.StudentCategories(CategoryName);
    INSERT INTO dbo.StudentCategories (CategoryName) VALUES
        (N'Open'), (N'OBC'), (N'SC'), (N'ST'), (N'EWS'), (N'Management Quota'), (N'NRI/Foreign');
    PRINT 'Task 12: StudentCategories created and seeded.';
END
GO

IF OBJECT_ID('dbo.FeeTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeTypes
    (
        FeeTypeID   INT IDENTITY(1,1) PRIMARY KEY,
        FeeTypeName NVARCHAR(100) NOT NULL,     -- fee "head" e.g. Tuition Fee, Registration Fee
        IsActive    BIT NOT NULL DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_FeeTypes_Name ON dbo.FeeTypes(FeeTypeName);
    INSERT INTO dbo.FeeTypes (FeeTypeName) VALUES
        (N'Registration Fee'), (N'Tuition Fee'), (N'Examination Fee'),
        (N'Library Fee'), (N'Laboratory Fee'), (N'Development Fee');
    PRINT 'Task 12: FeeTypes created and seeded.';
END
GO

/* ---------------------------------------------------------------------
   2. STUDENT ACADEMIC PROFILE
   The base Students table (Task 1-11) has no Course/Year/Category/
   Institute columns — those are set here by the admin (e.g. at
   admission/enrolment confirmation) and are what the fee-matching engine
   uses to find every applicable fee structure for a given student.
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.StudentAcademicProfile', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StudentAcademicProfile
    (
        StudentID          INT PRIMARY KEY FOREIGN KEY REFERENCES dbo.Students(StudentID),
        AcademicYearID     INT NOT NULL FOREIGN KEY REFERENCES dbo.AcademicYears(AcademicYearID),
        InstituteID        INT NOT NULL FOREIGN KEY REFERENCES dbo.Institutes(InstituteID),
        CourseID           INT NOT NULL FOREIGN KEY REFERENCES dbo.Courses(CourseID),
        YearSemester       NVARCHAR(20) NOT NULL,      -- e.g. 'Year 1', 'Semester 2'
        StudentCategoryID  INT NOT NULL FOREIGN KEY REFERENCES dbo.StudentCategories(StudentCategoryID),
        UpdatedBy          NVARCHAR(100) NULL,
        UpdatedDate        DATETIME NOT NULL DEFAULT (GETDATE())
    );
    PRINT 'Task 12: StudentAcademicProfile created.';
END
GO

/* ---------------------------------------------------------------------
   3. FEE STRUCTURES (configuration)
   One row = one fee head applicable to a specific
   (AcademicYear, Institute, Course, Year/Semester, StudentCategory).
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FeeStructures', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeStructures
    (
        FeeStructureID      INT IDENTITY(1,1) PRIMARY KEY,
        AcademicYearID      INT NOT NULL FOREIGN KEY REFERENCES dbo.AcademicYears(AcademicYearID),
        InstituteID         INT NOT NULL FOREIGN KEY REFERENCES dbo.Institutes(InstituteID),
        CourseID            INT NOT NULL FOREIGN KEY REFERENCES dbo.Courses(CourseID),
        YearSemester        NVARCHAR(20)   NOT NULL,
        StudentCategoryID   INT NOT NULL FOREIGN KEY REFERENCES dbo.StudentCategories(StudentCategoryID),
        FeeTypeID           INT NOT NULL FOREIGN KEY REFERENCES dbo.FeeTypes(FeeTypeID),

        FeeAmount           DECIMAL(12,2)  NOT NULL,
        DueDate             DATE           NOT NULL,

        InstallmentsAllowed BIT            NOT NULL DEFAULT (0),
        NumberOfInstallments INT           NOT NULL DEFAULT (1),

        LateFeeType         NVARCHAR(20)   NOT NULL DEFAULT ('Flat'),   -- Flat | PerDay | Percentage
        LateFeeValue        DECIMAL(10,2)  NOT NULL DEFAULT (0),
        LateFeeGraceDays    INT            NOT NULL DEFAULT (0),
        LateFeeMaxAmount    DECIMAL(10,2)  NULL,                        -- optional cap; NULL = uncapped

        IsActive            BIT            NOT NULL DEFAULT (1),

        CreatedBy           NVARCHAR(100)  NULL,
        CreatedDate         DATETIME       NOT NULL DEFAULT (GETDATE()),
        UpdatedBy           NVARCHAR(100)  NULL,
        UpdatedDate         DATETIME       NOT NULL DEFAULT (GETDATE()),

        CONSTRAINT CK_FeeStructures_Amount CHECK (FeeAmount > 0),
        CONSTRAINT CK_FeeStructures_Installments CHECK (NumberOfInstallments BETWEEN 1 AND 12),
        CONSTRAINT CK_FeeStructures_LateFeeType CHECK (LateFeeType IN ('Flat','PerDay','Percentage')),
        CONSTRAINT CK_FeeStructures_LateFeeValue CHECK (LateFeeValue >= 0)
    );

    -- One configured fee head per axis combination; re-configuring means
    -- edit the existing row (or deactivate it and add a new one), never a
    -- silent duplicate.
    CREATE UNIQUE INDEX UX_FeeStructures_Axes ON dbo.FeeStructures
        (AcademicYearID, InstituteID, CourseID, YearSemester, StudentCategoryID, FeeTypeID);
    CREATE INDEX IX_FeeStructures_Active ON dbo.FeeStructures(IsActive);

    PRINT 'Task 12: FeeStructures created.';
END
GO

IF OBJECT_ID('dbo.FeeStructureInstallments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeStructureInstallments
    (
        InstallmentID   INT IDENTITY(1,1) PRIMARY KEY,
        FeeStructureID  INT NOT NULL FOREIGN KEY REFERENCES dbo.FeeStructures(FeeStructureID) ON DELETE CASCADE,
        InstallmentNo   INT NOT NULL,
        DueDate         DATE NOT NULL,
        AmountPercent   DECIMAL(5,2) NOT NULL,     -- share of FeeAmount; all rows for a structure sum to 100.00

        CONSTRAINT CK_FSI_Percent CHECK (AmountPercent > 0 AND AmountPercent <= 100)
    );
    CREATE UNIQUE INDEX UX_FSI_StructureInstallment ON dbo.FeeStructureInstallments(FeeStructureID, InstallmentNo);
    PRINT 'Task 12: FeeStructureInstallments created.';
END
GO

/* ---------------------------------------------------------------------
   4. STUDENT FEE DEMANDS (auto-generated)
   Generated per student per applicable FeeStructure. GrossAmount is
   snapshotted from the structure at generation time (so a later change to
   the structure never silently rewrites an already-issued demand).
   DiscountAmount is admin-editable (scholarship/concession). NetPayable
   and Outstanding are computed columns so they can never drift out of
   sync with what has actually been paid/discounted.
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.StudentFeeDemands', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StudentFeeDemands
    (
        FeeDemandID     INT IDENTITY(1,1) PRIMARY KEY,
        StudentID       INT NOT NULL FOREIGN KEY REFERENCES dbo.Students(StudentID),
        FeeStructureID  INT NOT NULL FOREIGN KEY REFERENCES dbo.FeeStructures(FeeStructureID),
        FeeTypeID       INT NOT NULL FOREIGN KEY REFERENCES dbo.FeeTypes(FeeTypeID),

        GrossAmount     DECIMAL(12,2) NOT NULL,
        DiscountAmount  DECIMAL(12,2) NOT NULL DEFAULT (0),
        DiscountReason  NVARCHAR(300) NULL,
        AmountPaid      DECIMAL(12,2) NOT NULL DEFAULT (0),
        LateFeeCharged  DECIMAL(12,2) NOT NULL DEFAULT (0),   -- sum of late fee actually allocated via payments

        DueDate         DATE NOT NULL,
        Status          NVARCHAR(20) NOT NULL DEFAULT ('Pending'),  -- Pending|PartiallyPaid|Paid|Overdue|Waived

        GeneratedBy     NVARCHAR(100) NULL,
        GeneratedDate   DATETIME NOT NULL DEFAULT (GETDATE()),

        CONSTRAINT CK_SFD_Status CHECK (Status IN ('Pending','PartiallyPaid','Paid','Overdue','Waived')),
        CONSTRAINT CK_SFD_Discount CHECK (DiscountAmount >= 0),
        CONSTRAINT CK_SFD_Paid CHECK (AmountPaid >= 0),
        CONSTRAINT UQ_SFD_StudentStructure UNIQUE (StudentID, FeeStructureID)
    );
    CREATE INDEX IX_SFD_Student ON dbo.StudentFeeDemands(StudentID);
    CREATE INDEX IX_SFD_Status ON dbo.StudentFeeDemands(Status);
    PRINT 'Task 12: StudentFeeDemands created.';
END
GO

IF OBJECT_ID('dbo.FeeInstallmentDemands', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeInstallmentDemands
    (
        InstallmentDemandID INT IDENTITY(1,1) PRIMARY KEY,
        FeeDemandID          INT NOT NULL FOREIGN KEY REFERENCES dbo.StudentFeeDemands(FeeDemandID) ON DELETE CASCADE,
        InstallmentNo        INT NOT NULL,
        DueDate              DATE NOT NULL,
        AmountDue            DECIMAL(12,2) NOT NULL,
        AmountPaid           DECIMAL(12,2) NOT NULL DEFAULT (0),
        LateFeeCharged       DECIMAL(12,2) NOT NULL DEFAULT (0),
        Status               NVARCHAR(20) NOT NULL DEFAULT ('Pending'), -- Pending|PartiallyPaid|Paid|Overdue

        CONSTRAINT CK_FID_Status CHECK (Status IN ('Pending','PartiallyPaid','Paid','Overdue')),
        CONSTRAINT UQ_FID_DemandInstallment UNIQUE (FeeDemandID, InstallmentNo)
    );
    CREATE INDEX IX_FID_Demand ON dbo.FeeInstallmentDemands(FeeDemandID);
    CREATE INDEX IX_FID_DueDate ON dbo.FeeInstallmentDemands(DueDate);
    PRINT 'Task 12: FeeInstallmentDemands created.';
END
GO

/* ---------------------------------------------------------------------
   5. PAYMENT TRANSACTIONS + ALLOCATIONS
   A transaction is the money received (one payment event, online or
   offline). Allocations record exactly how that money was split across
   fee heads/installments (principal vs late fee) — the audit trail that
   makes "amount paid" per fee head reconstructible and reconcilable.
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FeeTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeTransactions
    (
        TransactionID         INT IDENTITY(1,1) PRIMARY KEY,
        StudentID              INT NOT NULL FOREIGN KEY REFERENCES dbo.Students(StudentID),
        TransactionRef          NVARCHAR(50)  NOT NULL,   -- internal receipt number, e.g. RCPT-000123
        PaymentMode             NVARCHAR(20)  NOT NULL,   -- Online | Cash | Cheque | DD | BankTransfer | UPI
        Amount                  DECIMAL(12,2) NOT NULL,
        PaymentDate              DATETIME      NOT NULL DEFAULT (GETDATE()),

        GatewayName              NVARCHAR(50)  NULL,
        GatewayTransactionID     NVARCHAR(100) NULL,
        BankReferenceNumber      NVARCHAR(100) NULL,
        ChequeOrDDNumber         NVARCHAR(50)  NULL,
        Remarks                  NVARCHAR(300) NULL,

        Status                   NVARCHAR(20)  NOT NULL DEFAULT ('Success'), -- Success|Pending|Failed|Reversed
        ReconciliationStatus     NVARCHAR(20)  NOT NULL DEFAULT ('Unreconciled'), -- Unreconciled|Reconciled|Disputed
        ReconciledBy             NVARCHAR(100) NULL,
        ReconciledDate           DATETIME      NULL,

        CreatedBy                NVARCHAR(100) NULL,
        CreatedDate               DATETIME      NOT NULL DEFAULT (GETDATE()),

        CONSTRAINT CK_FT_Mode CHECK (PaymentMode IN ('Online','Cash','Cheque','DD','BankTransfer','UPI')),
        CONSTRAINT CK_FT_Status CHECK (Status IN ('Success','Pending','Failed','Reversed')),
        CONSTRAINT CK_FT_Reconciliation CHECK (ReconciliationStatus IN ('Unreconciled','Reconciled','Disputed')),
        CONSTRAINT CK_FT_Amount CHECK (Amount > 0)
    );
    CREATE UNIQUE INDEX UX_FT_TransactionRef ON dbo.FeeTransactions(TransactionRef);
    CREATE INDEX IX_FT_Student ON dbo.FeeTransactions(StudentID);
    CREATE INDEX IX_FT_Reconciliation ON dbo.FeeTransactions(ReconciliationStatus);
    CREATE INDEX IX_FT_GatewayTxnId ON dbo.FeeTransactions(GatewayTransactionID);
    CREATE INDEX IX_FT_BankRef ON dbo.FeeTransactions(BankReferenceNumber);
    PRINT 'Task 12: FeeTransactions created.';
END
GO

IF OBJECT_ID('dbo.FeeTransactionAllocations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeTransactionAllocations
    (
        AllocationID         INT IDENTITY(1,1) PRIMARY KEY,
        TransactionID         INT NOT NULL FOREIGN KEY REFERENCES dbo.FeeTransactions(TransactionID),
        FeeDemandID            INT NOT NULL FOREIGN KEY REFERENCES dbo.StudentFeeDemands(FeeDemandID),
        InstallmentDemandID    INT NULL FOREIGN KEY REFERENCES dbo.FeeInstallmentDemands(InstallmentDemandID),
        AllocationType         NVARCHAR(20) NOT NULL,   -- Principal | LateFee
        AllocatedAmount        DECIMAL(12,2) NOT NULL,
        CreatedDate            DATETIME NOT NULL DEFAULT (GETDATE()),

        CONSTRAINT CK_FTA_Type CHECK (AllocationType IN ('Principal','LateFee')),
        CONSTRAINT CK_FTA_Amount CHECK (AllocatedAmount > 0)
    );
    CREATE INDEX IX_FTA_Transaction ON dbo.FeeTransactionAllocations(TransactionID);
    CREATE INDEX IX_FTA_Demand ON dbo.FeeTransactionAllocations(FeeDemandID);
    PRINT 'Task 12: FeeTransactionAllocations created.';
END
GO

/* ---------------------------------------------------------------------
   6. RECONCILIATION BATCHES
   A batch = one bank/gateway settlement statement uploaded by the admin
   for matching against FeeTransactions (by reference + amount).
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.FeeReconciliationBatches', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeReconciliationBatches
    (
        BatchID          INT IDENTITY(1,1) PRIMARY KEY,
        SourceLabel       NVARCHAR(100) NOT NULL,   -- e.g. 'HDFC Bank - Aug 2026' or 'Razorpay Settlement'
        UploadedFileName  NVARCHAR(255) NULL,
        UploadedBy        NVARCHAR(100) NULL,
        UploadedDate      DATETIME NOT NULL DEFAULT (GETDATE()),
        TotalRecords      INT NOT NULL DEFAULT (0),
        MatchedRecords    INT NOT NULL DEFAULT (0),
        UnmatchedRecords  INT NOT NULL DEFAULT (0)
    );
    PRINT 'Task 12: FeeReconciliationBatches created.';
END
GO

IF OBJECT_ID('dbo.FeeReconciliationRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeeReconciliationRecords
    (
        RecordID              INT IDENTITY(1,1) PRIMARY KEY,
        BatchID                INT NOT NULL FOREIGN KEY REFERENCES dbo.FeeReconciliationBatches(BatchID) ON DELETE CASCADE,
        BankReferenceNumber     NVARCHAR(100) NOT NULL,
        BankAmount               DECIMAL(12,2) NOT NULL,
        BankTransactionDate      DATE NULL,
        MatchedTransactionID     INT NULL FOREIGN KEY REFERENCES dbo.FeeTransactions(TransactionID),
        MatchStatus               NVARCHAR(20) NOT NULL DEFAULT ('Unmatched'), -- Matched|Unmatched|Mismatch

        CONSTRAINT CK_FRR_MatchStatus CHECK (MatchStatus IN ('Matched','Unmatched','Mismatch'))
    );
    CREATE INDEX IX_FRR_Batch ON dbo.FeeReconciliationRecords(BatchID);
    CREATE INDEX IX_FRR_BankRef ON dbo.FeeReconciliationRecords(BankReferenceNumber);
    PRINT 'Task 12: FeeReconciliationRecords created.';
END
GO

PRINT 'Task 12 migration complete: Registration Fee Management schema ready.';
