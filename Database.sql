/* =====================================================================
   ADVANCED STUDENT REGISTRATION SYSTEM
   Database.sql  -  Schema + Lookup Data + 50 Mock Student Records
   Target: SQL Server 2016+
   -----------------------------------------------------------------------
   FIRST-TIME SETUP ONLY. Do not re-run this file once any of
   Database_Task12_RegistrationFee.sql / _Task15_Timetable.sql /
   _Task16_CentralisedDashboard.sql / InstituteRegistration.sql have been
   applied: those add foreign keys into dbo.Students, so the unconditional
   "DROP TABLE dbo.Students" below will fail on a re-run, which in turn
   leaves dbo.Countries/States/Districts un-dropped while their INSERT
   statements still run - silently duplicating every country/state/
   district row. If that has already happened, run
   Database_Fix_DuplicateLocations.sql once to clean it up.
   ===================================================================== */

IF DB_ID('StudentRegistrationDB') IS NULL
BEGIN
    CREATE DATABASE StudentRegistrationDB;
END
GO

USE StudentRegistrationDB;
GO

/* ---------------------------------------------------------------------
   DROP EXISTING (safe re-run)
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Students', 'U') IS NOT NULL DROP TABLE dbo.Students;
IF OBJECT_ID('dbo.Districts', 'U') IS NOT NULL DROP TABLE dbo.Districts;
IF OBJECT_ID('dbo.States', 'U') IS NOT NULL DROP TABLE dbo.States;
IF OBJECT_ID('dbo.Countries', 'U') IS NOT NULL DROP TABLE dbo.Countries;
GO

/* ---------------------------------------------------------------------
   LOOKUP TABLES (cascading Country -> State -> District)
--------------------------------------------------------------------- */
CREATE TABLE dbo.Countries
(
    CountryID   INT IDENTITY(1,1) PRIMARY KEY,
    CountryName NVARCHAR(100) NOT NULL,
    CountryCode NVARCHAR(5)   NOT NULL,   -- ISO2, used for intl-tel-input
    DialCode    NVARCHAR(10)  NOT NULL
);
GO

CREATE TABLE dbo.States
(
    StateID     INT IDENTITY(1,1) PRIMARY KEY,
    StateName   NVARCHAR(100) NOT NULL,
    CountryID   INT NOT NULL FOREIGN KEY REFERENCES dbo.Countries(CountryID)
);
GO

CREATE TABLE dbo.Districts
(
    DistrictID   INT IDENTITY(1,1) PRIMARY KEY,
    DistrictName NVARCHAR(100) NOT NULL,
    StateID      INT NOT NULL FOREIGN KEY REFERENCES dbo.States(StateID)
);
GO

/* ---------------------------------------------------------------------
   STUDENTS TABLE
--------------------------------------------------------------------- */
CREATE TABLE dbo.Students
(
    StudentID        INT IDENTITY(1001,1) PRIMARY KEY,
    FullName         NVARCHAR(150)  NOT NULL,
    Email            NVARCHAR(150)  NOT NULL,
    Mobile           NVARCHAR(25)   NOT NULL,
    CountryID        INT            NOT NULL FOREIGN KEY REFERENCES dbo.Countries(CountryID),
    StateID          INT            NOT NULL FOREIGN KEY REFERENCES dbo.States(StateID),
    DistrictID       INT            NOT NULL FOREIGN KEY REFERENCES dbo.Districts(DistrictID),
    Address          NVARCHAR(300)  NULL,
    Gender           NVARCHAR(10)   NULL,
    DOB              DATE           NULL,
    PhotoPath        NVARCHAR(255)  NULL,
    IsEmailVerified  BIT            NOT NULL DEFAULT (0),
    RegistrationDate DATETIME       NOT NULL DEFAULT (GETDATE()),
    LastLoginDate    DATETIME       NULL
);
GO

CREATE UNIQUE INDEX IX_Students_Email ON dbo.Students(Email);
CREATE INDEX IX_Students_Mobile ON dbo.Students(Mobile);
GO

/* =====================================================================
   SEED: COUNTRIES
   ===================================================================== */
INSERT INTO dbo.Countries (CountryName, CountryCode, DialCode) VALUES
('India',          'in', '+91'),
('United States',  'us', '+1'),
('United Kingdom', 'gb', '+44'),
('Canada',         'ca', '+1'),
('Australia',      'au', '+61');
GO

/* =====================================================================
   SEED: STATES
   ===================================================================== */
INSERT INTO dbo.States (StateName, CountryID) VALUES
('Maharashtra', 1), ('Karnataka', 1), ('Gujarat', 1), ('Tamil Nadu', 1), ('Delhi', 1), ('Rajasthan', 1),
('California', 2), ('Texas', 2), ('New York', 2),
('England', 3), ('Scotland', 3),
('Ontario', 4), ('Quebec', 4),
('New South Wales', 5), ('Victoria', 5);
GO

/* =====================================================================
   SEED: DISTRICTS
   ===================================================================== */
INSERT INTO dbo.Districts (DistrictName, StateID) VALUES
('Pune', 1), ('Mumbai', 1), ('Nagpur', 1), ('Nashik', 1), ('Baramati', 1),      -- Maharashtra (1-5)
('Bengaluru', 2), ('Mysuru', 2), ('Hubli', 2),                                  -- Karnataka (6-8)
('Ahmedabad', 3), ('Surat', 3), ('Vadodara', 3),                                -- Gujarat (9-11)
('Chennai', 4), ('Coimbatore', 4),                                              -- Tamil Nadu (12-13)
('New Delhi', 5),                                                               -- Delhi (14)
('Jaipur', 6), ('Udaipur', 6),                                                  -- Rajasthan (15-16)
('Los Angeles', 7), ('San Francisco', 7),                                       -- California (17-18)
('Houston', 8), ('Dallas', 8),                                                  -- Texas (19-20)
('New York City', 9), ('Buffalo', 9),                                          -- New York (21-22)
('London', 10), ('Manchester', 10),                                            -- England (23-24)
('Edinburgh', 11),                                                              -- Scotland (25)
('Toronto', 12), ('Ottawa', 12),                                                -- Ontario (26-27)
('Montreal', 13),                                                               -- Quebec (28)
('Sydney', 14),                                                                 -- New South Wales (29)
('Melbourne', 15);                                                              -- Victoria (30)
GO

/* =====================================================================
   SEED: 50 MOCK STUDENT RECORDS
   ===================================================================== */
INSERT INTO dbo.Students (FullName, Email, Mobile, CountryID, StateID, DistrictID, Address, Gender, DOB, PhotoPath, IsEmailVerified, RegistrationDate) VALUES
('Aarav Sharma',        'aarav.sharma1@example.com',   '+919820011223', 1, 1, 1,  'Flat 12, Koregaon Park',        'Male',   '2001-03-14', NULL, 1, DATEADD(DAY,-120,GETDATE())),
('Isha Patil',          'isha.patil2@example.com',     '+919820011224', 1, 1, 2,  'A-45, Andheri West',            'Female', '2002-07-22', NULL, 1, DATEADD(DAY,-118,GETDATE())),
('Rohan Deshmukh',      'rohan.deshmukh3@example.com', '+919820011225', 1, 1, 3,  'Civil Lines, Nagpur',           'Male',   '2000-11-05', NULL, 1, DATEADD(DAY,-116,GETDATE())),
('Sneha Kulkarni',      'sneha.kulkarni4@example.com', '+919820011226', 1, 1, 4,  'College Road, Nashik',          'Female', '2001-01-30', NULL, 1, DATEADD(DAY,-114,GETDATE())),
('Vivaan Jadhav',       'vivaan.jadhav5@example.com',  '+919820011227', 1, 1, 5,  'MG Road, Baramati',             'Male',   '2003-05-18', NULL, 0, DATEADD(DAY,-112,GETDATE())),
('Ananya Rao',          'ananya.rao6@example.com',     '+919820011228', 1, 2, 6,  'Indiranagar, Bengaluru',        'Female', '2002-09-09', NULL, 1, DATEADD(DAY,-110,GETDATE())),
('Kabir Gowda',         'kabir.gowda7@example.com',    '+919820011229', 1, 2, 7,  'Vijayanagar, Mysuru',           'Male',   '2001-12-25', NULL, 1, DATEADD(DAY,-108,GETDATE())),
('Diya Hegde',          'diya.hegde8@example.com',     '+919820011230', 1, 2, 8,  'Deshpande Nagar, Hubli',        'Female', '2000-04-17', NULL, 1, DATEADD(DAY,-106,GETDATE())),
('Aditya Shah',         'aditya.shah9@example.com',    '+919820011231', 1, 3, 9,  'Navrangpura, Ahmedabad',        'Male',   '2001-06-11', NULL, 1, DATEADD(DAY,-104,GETDATE())),
('Kavya Mehta',         'kavya.mehta10@example.com',   '+919820011232', 1, 3, 10, 'Athwa, Surat',                  'Female', '2002-02-28', NULL, 1, DATEADD(DAY,-102,GETDATE())),
('Reyansh Trivedi',     'reyansh.trivedi11@example.com','+919820011233',1, 3, 11, 'Alkapuri, Vadodara',            'Male',   '2000-08-08', NULL, 0, DATEADD(DAY,-100,GETDATE())),
('Myra Subramaniam',    'myra.s12@example.com',        '+919820011234', 1, 4, 12, 'T Nagar, Chennai',              'Female', '2001-10-19', NULL, 1, DATEADD(DAY,-98,GETDATE())),
('Arjun Pillai',        'arjun.pillai13@example.com',  '+919820011235', 1, 4, 13, 'RS Puram, Coimbatore',          'Male',   '2003-03-03', NULL, 1, DATEADD(DAY,-96,GETDATE())),
('Saanvi Kapoor',       'saanvi.kapoor14@example.com', '+919820011236', 1, 5, 14, 'Karol Bagh, New Delhi',         'Female', '2002-05-27', NULL, 1, DATEADD(DAY,-94,GETDATE())),
('Vihaan Malhotra',     'vihaan.malhotra15@example.com','+919820011237',1, 5, 14, 'Dwarka Sector 12, New Delhi',   'Male',   '2001-07-07', NULL, 1, DATEADD(DAY,-92,GETDATE())),
('Aadhya Rathore',      'aadhya.rathore16@example.com','+919820011238', 1, 6, 15, 'C-Scheme, Jaipur',              'Female', '2000-12-12', NULL, 0, DATEADD(DAY,-90,GETDATE())),
('Ayaan Sisodia',       'ayaan.sisodia17@example.com', '+919820011239', 1, 6, 16, 'Fatehpura, Udaipur',            'Male',   '2001-09-23', NULL, 1, DATEADD(DAY,-88,GETDATE())),
('Ishaan Verma',        'ishaan.verma18@example.com',  '+919820011240', 1, 1, 1,  'Kothrud, Pune',                 'Male',   '2002-01-15', NULL, 1, DATEADD(DAY,-86,GETDATE())),
('Anika Bhosale',       'anika.bhosale19@example.com', '+919820011241', 1, 1, 2,  'Bandra East, Mumbai',           'Female', '2003-04-04', NULL, 1, DATEADD(DAY,-84,GETDATE())),
('Advik Naik',          'advik.naik20@example.com',    '+919820011242', 1, 1, 3,  'Dharampeth, Nagpur',            'Male',   '2000-06-30', NULL, 1, DATEADD(DAY,-82,GETDATE())),
('Emily Johnson',       'emily.johnson21@example.com', '+13105550101',  2, 7, 17, '221 Sunset Blvd, Los Angeles',  'Female', '2001-02-14', NULL, 1, DATEADD(DAY,-80,GETDATE())),
('Michael Brown',       'michael.brown22@example.com', '+14155550102',  2, 7, 18, '55 Market St, San Francisco',   'Male',   '2000-10-10', NULL, 1, DATEADD(DAY,-78,GETDATE())),
('Olivia Davis',        'olivia.davis23@example.com',  '+17135550103',  2, 8, 19, '400 Main St, Houston',          'Female', '2002-08-19', NULL, 0, DATEADD(DAY,-76,GETDATE())),
('James Wilson',        'james.wilson24@example.com',  '+12145550104',  2, 8, 20, '120 Elm St, Dallas',            'Male',   '2001-11-11', NULL, 1, DATEADD(DAY,-74,GETDATE())),
('Sophia Martinez',     'sophia.martinez25@example.com','+12125550105', 2, 9, 21, '5th Avenue, New York City',     'Female', '2003-01-01', NULL, 1, DATEADD(DAY,-72,GETDATE())),
('William Anderson',    'william.anderson26@example.com','+17165550106',2, 9, 22, 'Delaware Ave, Buffalo',         'Male',   '2000-03-21', NULL, 1, DATEADD(DAY,-70,GETDATE())),
('Charlotte Taylor',    'charlotte.taylor27@example.com','+442075550107',3,10,23, 'Baker Street, London',          'Female', '2002-06-06', NULL, 1, DATEADD(DAY,-68,GETDATE())),
('Benjamin Thomas',     'benjamin.thomas28@example.com','+441615550108',3,10,24, 'Deansgate, Manchester',          'Male',   '2001-09-09', NULL, 0, DATEADD(DAY,-66,GETDATE())),
('Amelia White',        'amelia.white29@example.com',  '+441315550109', 3,11,25, 'Princes Street, Edinburgh',      'Female', '2000-12-20', NULL, 1, DATEADD(DAY,-64,GETDATE())),
('Lucas Harris',        'lucas.harris30@example.com',  '+14165550110',  4,12,26, 'Queen St, Toronto',              'Male',   '2001-04-25', NULL, 1, DATEADD(DAY,-62,GETDATE())),
('Mia Clark',           'mia.clark31@example.com',     '+16135550111',  4,12,27, 'Bank Street, Ottawa',            'Female', '2002-07-15', NULL, 1, DATEADD(DAY,-60,GETDATE())),
('Ethan Lewis',         'ethan.lewis32@example.com',   '+15145550112',  4,13,28, 'Rue Sainte-Catherine, Montreal', 'Male',   '2003-02-02', NULL, 1, DATEADD(DAY,-58,GETDATE())),
('Ava Walker',          'ava.walker33@example.com',    '+61295550113',  5,14,29, 'George Street, Sydney',          'Female', '2000-05-05', NULL, 0, DATEADD(DAY,-56,GETDATE())),
('Alexander Hall',      'alexander.hall34@example.com','+61395550114',  5,15,30, 'Collins Street, Melbourne',      'Male',   '2001-08-08', NULL, 1, DATEADD(DAY,-54,GETDATE())),
('Riya Iyer',           'riya.iyer35@example.com',     '+919820011243', 1, 2, 6,  'Jayanagar, Bengaluru',          'Female', '2002-10-10', NULL, 1, DATEADD(DAY,-52,GETDATE())),
('Sarthak Joshi',       'sarthak.joshi36@example.com', '+919820011244', 1, 1, 1,  'Aundh, Pune',                   'Male',   '2001-03-03', NULL, 1, DATEADD(DAY,-50,GETDATE())),
('Prisha Nair',         'prisha.nair37@example.com',   '+919820011245', 1, 4, 12, 'Adyar, Chennai',                'Female', '2000-01-01', NULL, 1, DATEADD(DAY,-48,GETDATE())),
('Yash Choudhary',      'yash.choudhary38@example.com','+919820011246', 1, 6, 15, 'Malviya Nagar, Jaipur',         'Male',   '2003-06-16', NULL, 0, DATEADD(DAY,-46,GETDATE())),
('Navya Bhatt',         'navya.bhatt39@example.com',   '+919820011247', 1, 3, 9,  'Satellite, Ahmedabad',          'Female', '2002-04-12', NULL, 1, DATEADD(DAY,-44,GETDATE())),
('Krishna Pawar',       'krishna.pawar40@example.com', '+919820011248', 1, 1, 2,  'Powai, Mumbai',                 'Male',   '2001-11-27', NULL, 1, DATEADD(DAY,-42,GETDATE())),
('Ella Robinson',       'ella.robinson41@example.com', '+13125550115',  2, 9, 21, 'Broadway, New York City',       'Female', '2000-09-19', NULL, 1, DATEADD(DAY,-40,GETDATE())),
('Daniel Young',        'daniel.young42@example.com',  '+14085550116',  2, 7, 18, 'Mission St, San Francisco',     'Male',   '2001-05-05', NULL, 1, DATEADD(DAY,-38,GETDATE())),
('Grace King',          'grace.king43@example.com',    '+442075550117', 3,10,23, 'Oxford Street, London',         'Female', '2002-03-28', NULL, 0, DATEADD(DAY,-36,GETDATE())),
('Henry Wright',        'henry.wright44@example.com',  '+14165550118',  4,12,26, 'Yonge St, Toronto',              'Male',   '2000-07-07', NULL, 1, DATEADD(DAY,-34,GETDATE())),
('Chloe Scott',         'chloe.scott45@example.com',   '+61295550119',  5,14,29, 'Pitt Street, Sydney',            'Female', '2001-12-01', NULL, 1, DATEADD(DAY,-32,GETDATE())),
('Aryan Bhatia',        'aryan.bhatia46@example.com',  '+919820011249', 1, 5, 14, 'Rohini, New Delhi',             'Male',   '2003-08-23', NULL, 1, DATEADD(DAY,-30,GETDATE())),
('Zara Khan',           'zara.khan47@example.com',     '+919820011250', 1, 1, 4,  'College Road, Nashik',          'Female', '2002-02-14', NULL, 1, DATEADD(DAY,-20,GETDATE())),
('Kunal Ghosh',         'kunal.ghosh48@example.com',   '+919820011251', 1, 2, 7,  'Kuvempunagar, Mysuru',          'Male',   '2001-10-30', NULL, 0, DATEADD(DAY,-14,GETDATE())),
('Tara Menon',          'tara.menon49@example.com',    '+919820011252', 1, 4, 13, 'Race Course, Coimbatore',       'Female', '2000-04-04', NULL, 1, DATEADD(DAY,-7,GETDATE())),
('Devansh Chatterjee',  'devansh.c50@example.com',     '+919820011253', 1, 1, 5,  'Station Road, Baramati',        'Male',   '2001-01-09', NULL, 1, DATEADD(DAY,-2,GETDATE()));
GO

/* =====================================================================
   TASK 7: ADMIN PANEL, ADMIN DASHBOARD, CANDIDATE APPROVAL & LOGIN MGMT
   -----------------------------------------------------------------------
   Adds approval workflow / account-status columns to Students, plus a
   dedicated Admins table for the secure Admin Login.
   ===================================================================== */

ALTER TABLE dbo.Students ADD ApprovalStatus  NVARCHAR(20)  NOT NULL DEFAULT ('Pending');   -- Pending / Approved / Rejected
ALTER TABLE dbo.Students ADD AccountStatus   NVARCHAR(20)  NOT NULL DEFAULT ('Active');    -- Active / Inactive
ALTER TABLE dbo.Students ADD RejectionRemark NVARCHAR(500) NULL;
ALTER TABLE dbo.Students ADD ApprovedBy      NVARCHAR(100) NULL;
ALTER TABLE dbo.Students ADD ApprovedDate    DATETIME      NULL;
ALTER TABLE dbo.Students ADD RejectedBy      NVARCHAR(100) NULL;
ALTER TABLE dbo.Students ADD RejectedDate    DATETIME      NULL;
ALTER TABLE dbo.Students ADD CreatedDate     DATETIME      NOT NULL DEFAULT (GETDATE());
ALTER TABLE dbo.Students ADD LastModifiedDate DATETIME     NOT NULL DEFAULT (GETDATE());
GO

ALTER TABLE dbo.Students ADD CONSTRAINT CK_Students_ApprovalStatus
    CHECK (ApprovalStatus IN ('Pending','Approved','Rejected'));
ALTER TABLE dbo.Students ADD CONSTRAINT CK_Students_AccountStatus
    CHECK (AccountStatus IN ('Active','Inactive'));
GO

-- The 50 mock records above were inserted before this migration, so they
-- defaulted to Pending/Active. Treat pre-existing demo data as already
-- vetted so the system is usable out of the box.
UPDATE dbo.Students
SET ApprovalStatus = 'Approved',
    ApprovedBy = 'System (seed data)',
    ApprovedDate = RegistrationDate,
    CreatedDate = RegistrationDate,
    LastModifiedDate = RegistrationDate;
GO

IF OBJECT_ID('dbo.Admins', 'U') IS NOT NULL DROP TABLE dbo.Admins;
GO

CREATE TABLE dbo.Admins
(
    AdminID      INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    FullName     NVARCHAR(100) NOT NULL,
    IsActive     BIT           NOT NULL DEFAULT (1),
    CreatedDate  DATETIME      NOT NULL DEFAULT (GETDATE())
);
GO

CREATE UNIQUE INDEX IX_Admins_Username ON dbo.Admins(Username);
GO

-- Default admin: Username = admin | Password = Admin@123
-- PasswordHash below is the SHA-256 hex digest of "Admin@123" (matches AdminAuth.ComputeHash in App_Code).
INSERT INTO dbo.Admins (Username, PasswordHash, FullName) VALUES
('admin', 'E86F78A8A3CAF0B60D8E74E5942AA6D86DC150CD3C03338AEF25B7D2D7E3ACC7', 'System Administrator');
GO

PRINT 'Task 7 migration complete: approval workflow columns added, Admins table created and seeded.';

/* =====================================================================
   TASK 7 MIGRATION NOTE (idempotent, for existing databases)
   -----------------------------------------------------------------------
   If you already ran the base script above on a live database and only
   want to layer in Task 7 without dropping Students data, run just this
   block instead:

   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'ApprovalStatus')
   BEGIN
       ALTER TABLE dbo.Students ADD ApprovalStatus  NVARCHAR(20)  NOT NULL DEFAULT ('Pending');
       ALTER TABLE dbo.Students ADD AccountStatus   NVARCHAR(20)  NOT NULL DEFAULT ('Active');
       ALTER TABLE dbo.Students ADD RejectionRemark NVARCHAR(500) NULL;
       ALTER TABLE dbo.Students ADD ApprovedBy      NVARCHAR(100) NULL;
       ALTER TABLE dbo.Students ADD ApprovedDate    DATETIME      NULL;
       ALTER TABLE dbo.Students ADD RejectedBy      NVARCHAR(100) NULL;
       ALTER TABLE dbo.Students ADD RejectedDate    DATETIME      NULL;
       ALTER TABLE dbo.Students ADD CreatedDate     DATETIME      NOT NULL DEFAULT (GETDATE());
       ALTER TABLE dbo.Students ADD LastModifiedDate DATETIME    NOT NULL DEFAULT (GETDATE());
       ALTER TABLE dbo.Students ADD CONSTRAINT CK_Students_ApprovalStatus CHECK (ApprovalStatus IN ('Pending','Approved','Rejected'));
       ALTER TABLE dbo.Students ADD CONSTRAINT CK_Students_AccountStatus CHECK (AccountStatus IN ('Active','Inactive'));
       UPDATE dbo.Students SET ApprovalStatus = 'Approved', ApprovedBy = 'System (seed data)', ApprovedDate = RegistrationDate;
   END
   GO

   IF OBJECT_ID('dbo.Admins', 'U') IS NULL
   BEGIN
       CREATE TABLE dbo.Admins
       (
           AdminID      INT IDENTITY(1,1) PRIMARY KEY,
           Username     NVARCHAR(50)  NOT NULL,
           PasswordHash NVARCHAR(256) NOT NULL,
           FullName     NVARCHAR(100) NOT NULL,
           IsActive     BIT           NOT NULL DEFAULT (1),
           CreatedDate  DATETIME      NOT NULL DEFAULT (GETDATE())
       );
       CREATE UNIQUE INDEX IX_Admins_Username ON dbo.Admins(Username);
       INSERT INTO dbo.Admins (Username, PasswordHash, FullName) VALUES
       ('admin', 'E86F78A8A3CAF0B60D8E74E5942AA6D86DC150CD3C03338AEF25B7D2D7E3ACC7', 'System Administrator');
   END
   GO
   ===================================================================== */

PRINT 'Database schema created and seeded with 50 student records successfully.';

/* =====================================================================
   TASK 6 MIGRATION NOTE
   -----------------------------------------------------------------------
   The block above DROPS and recreates all tables (including Students),
   so it already includes the new LastLoginDate column for a fresh install.

   If you already have a database with real registered students and do NOT
   want to lose that data, do NOT re-run the script above. Instead, run
   just this small idempotent block against your existing database:

   IF NOT EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'LastLoginDate'
   )
   BEGIN
       ALTER TABLE dbo.Students ADD LastLoginDate DATETIME NULL;
   END
   GO

   -- Also upgrade the Email index to UNIQUE (only works if no duplicate emails already exist):
   IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Students_Email' AND is_unique = 0)
   BEGIN
       DROP INDEX IX_Students_Email ON dbo.Students;
       CREATE UNIQUE INDEX IX_Students_Email ON dbo.Students(Email);
   END
   GO
   ===================================================================== */


/* =====================================================================
   TASK 10: Rich Text Editor (Admin Panel document management)
   -----------------------------------------------------------------------
   Stores documents authored with the Admin Panel's Rich Text Editor
   (Requirement 6). ContentHtml holds the sanitized HTML produced by
   TinyMCE and re-sanitized server-side (RichTextSanitizer) before it is
   ever written here, so formatting is preserved exactly on reload.
   ===================================================================== */

IF OBJECT_ID('dbo.RichTextDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RichTextDocuments
    (
        DocumentID     INT IDENTITY(1,1) PRIMARY KEY,
        Title          NVARCHAR(255)  NOT NULL,
        ContentHtml    NVARCHAR(MAX)  NOT NULL,
        CreatedDate    DATETIME       NOT NULL DEFAULT (GETDATE()),
        ModifiedDate   DATETIME       NOT NULL DEFAULT (GETDATE()),
        CreatedBy      NVARCHAR(100)  NULL,
        ModifiedBy     NVARCHAR(100)  NULL,
        Status         NVARCHAR(20)   NOT NULL DEFAULT ('Published'),
        CONSTRAINT CK_RichTextDocuments_Status CHECK (Status IN ('Draft', 'Published'))
    );

    CREATE INDEX IX_RichTextDocuments_Title ON dbo.RichTextDocuments(Title);
    CREATE INDEX IX_RichTextDocuments_ModifiedDate ON dbo.RichTextDocuments(ModifiedDate DESC);

    PRINT 'Task 10 migration complete: RichTextDocuments table created.';
END
ELSE
BEGIN
    PRINT 'Task 10 migration skipped: RichTextDocuments table already exists.';
END
GO


/* =====================================================================
   TASK 11: Advertisement Modal (Student Registration page)
   -----------------------------------------------------------------------
   Stores the advertisement/notification banners shown in a dynamic modal
   when the Student Registration page loads (Requirement 4). Administrators
   manage everything here through the Admin Panel > Advertisements screen
   without ever touching code. AdvertisementSettings is a single-row table
   holding the global on/off switch for the whole modal feature.
   ===================================================================== */

IF OBJECT_ID('dbo.Advertisements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Advertisements
    (
        AdvertisementID INT IDENTITY(1,1) PRIMARY KEY,
        Title           NVARCHAR(200)  NOT NULL,
        Description     NVARCHAR(1000) NULL,
        ImagePath       NVARCHAR(500)  NULL,
        DisplayOrder    INT            NOT NULL DEFAULT (0),
        IsActive        BIT            NOT NULL DEFAULT (1),
        CreatedDate     DATETIME       NOT NULL DEFAULT (GETDATE()),
        UpdatedDate     DATETIME       NOT NULL DEFAULT (GETDATE())
    );

    CREATE INDEX IX_Advertisements_DisplayOrder ON dbo.Advertisements(DisplayOrder);
    CREATE INDEX IX_Advertisements_IsActive ON dbo.Advertisements(IsActive);

    PRINT 'Task 11 migration complete: Advertisements table created.';
END
ELSE
BEGIN
    PRINT 'Task 11 migration skipped: Advertisements table already exists.';
END
GO

IF OBJECT_ID('dbo.AdvertisementSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdvertisementSettings
    (
        SettingID     INT     NOT NULL PRIMARY KEY,
        ModalEnabled  BIT     NOT NULL DEFAULT (1)
    );

    -- Single, fixed settings row (SettingID = 1) that the admin toggle reads/writes.
    INSERT INTO dbo.AdvertisementSettings (SettingID, ModalEnabled) VALUES (1, 1);

    PRINT 'Task 11 migration complete: AdvertisementSettings table created and seeded.';
END
ELSE
BEGIN
    PRINT 'Task 11 migration skipped: AdvertisementSettings table already exists.';
END
GO
