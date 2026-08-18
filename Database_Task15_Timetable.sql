/* =====================================================================
   TASK 15: AUTOMATED TIMETABLE MANAGEMENT SYSTEM
   -----------------------------------------------------------------------
   Appends to Database.sql. Every block is guarded with IF OBJECT_ID(...)
   IS NULL, matching the Task 10/11/12/13 migration pattern, so this is
   safe to run against the existing StudentRegistrationDB at any time.

   Reuses dbo.AcademicYears / dbo.Institutes / dbo.Courses from the
   Task 12 migration (run that file first if it hasn't been applied yet).

   Run this whole file against StudentRegistrationDB after Database.sql
   and Database_Task12_RegistrationFee.sql have already been applied.
   ===================================================================== */

USE StudentRegistrationDB;
GO

/* ---------------------------------------------------------------------
   1. WORKING SCHEDULE (Section B of the task brief)
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.TT_WorkingDays', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_WorkingDays
    (
        DayID        INT IDENTITY(1,1) PRIMARY KEY,
        DayName      NVARCHAR(15) NOT NULL,
        DayOrder     INT          NOT NULL,   -- Mon=1 ... Sun=7, display order
        IsWorkingDay BIT          NOT NULL DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_TT_WorkingDays_Order ON dbo.TT_WorkingDays(DayOrder);
    INSERT INTO dbo.TT_WorkingDays (DayName, DayOrder, IsWorkingDay) VALUES
        (N'Monday', 1, 1), (N'Tuesday', 2, 1), (N'Wednesday', 3, 1),
        (N'Thursday', 4, 1), (N'Friday', 5, 1), (N'Saturday', 6, 1), (N'Sunday', 7, 0);
    PRINT 'Task 15: TT_WorkingDays created and seeded.';
END
GO

IF OBJECT_ID('dbo.TT_Periods', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_Periods
    (
        PeriodID     INT IDENTITY(1,1) PRIMARY KEY,
        PeriodNumber INT           NOT NULL,
        Label        NVARCHAR(30)  NOT NULL,   -- e.g. 'Period 1', 'Lunch Break'
        StartTime    VARCHAR(5)    NOT NULL,   -- 'HH:mm', 24hr
        EndTime      VARCHAR(5)    NOT NULL,
        IsBreak      BIT           NOT NULL DEFAULT (0)
    );
    CREATE UNIQUE INDEX IX_TT_Periods_Number ON dbo.TT_Periods(PeriodNumber);
    INSERT INTO dbo.TT_Periods (PeriodNumber, Label, StartTime, EndTime, IsBreak) VALUES
        (1, N'Period 1', '09:30', '10:25', 0),
        (2, N'Period 2', '10:25', '11:20', 0),
        (3, N'Period 3', '11:20', '11:35', 1),   -- short break
        (4, N'Period 4', '11:35', '12:30', 0),
        (5, N'Period 5', '12:30', '13:25', 0),
        (6, N'Lunch',    '13:25', '14:10', 1),
        (7, N'Period 6', '14:10', '15:05', 0),
        (8, N'Period 7', '15:05', '16:00', 0);
    PRINT 'Task 15: TT_Periods created and seeded.';
END
GO

IF OBJECT_ID('dbo.TT_ScheduleConfig', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_ScheduleConfig
    (
        ConfigID         INT IDENTITY(1,1) PRIMARY KEY,
        MaxClassesPerDay INT NOT NULL DEFAULT (6),   -- per division, per faculty (soft cap used by the generator)
        UpdatedBy        NVARCHAR(100) NULL,
        UpdatedDate      DATETIME NOT NULL DEFAULT (GETDATE())
    );
    INSERT INTO dbo.TT_ScheduleConfig (MaxClassesPerDay) VALUES (6);
    PRINT 'Task 15: TT_ScheduleConfig created and seeded.';
END
GO

/* ---------------------------------------------------------------------
   2. ACADEMIC SETUP (Section A) — Divisions & Subjects.
   AcademicYear / Institute / Course reuse the Task 12 lookup tables.
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.TT_Divisions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_Divisions
    (
        DivisionID      INT IDENTITY(1,1) PRIMARY KEY,
        AcademicYearID  INT NOT NULL FOREIGN KEY REFERENCES dbo.AcademicYears(AcademicYearID),
        CourseID        INT NOT NULL FOREIGN KEY REFERENCES dbo.Courses(CourseID),
        YearSemester    NVARCHAR(20)  NOT NULL,     -- e.g. 'Year 2 - Sem 3'
        DivisionName    NVARCHAR(20)  NOT NULL,     -- e.g. 'A', 'B'
        StudentStrength INT NOT NULL DEFAULT (0),
        IsActive        BIT NOT NULL DEFAULT (1)
    );
    CREATE INDEX IX_TT_Divisions_Course ON dbo.TT_Divisions(CourseID, YearSemester);
    PRINT 'Task 15: TT_Divisions created.';
END
GO

IF OBJECT_ID('dbo.TT_Subjects', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_Subjects
    (
        SubjectID    INT IDENTITY(1,1) PRIMARY KEY,
        SubjectCode  NVARCHAR(20)  NOT NULL,
        SubjectName  NVARCHAR(150) NOT NULL,
        CourseID     INT NOT NULL FOREIGN KEY REFERENCES dbo.Courses(CourseID),
        YearSemester NVARCHAR(20)  NOT NULL,
        SubjectType  NVARCHAR(15)  NOT NULL,   -- 'Theory' | 'Practical' | 'Tutorial'
        WeeklyHours  INT NOT NULL DEFAULT (1), -- periods/week the generator must place
        IsActive     BIT NOT NULL DEFAULT (1),
        CONSTRAINT CK_TT_Subjects_Type CHECK (SubjectType IN (N'Theory', N'Practical', N'Tutorial'))
    );
    CREATE INDEX IX_TT_Subjects_Course ON dbo.TT_Subjects(CourseID, YearSemester);
    PRINT 'Task 15: TT_Subjects created.';
END
GO

/* ---------------------------------------------------------------------
   3. FACULTY (Section C)
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.TT_Faculty', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_Faculty
    (
        FacultyID   INT IDENTITY(1,1) PRIMARY KEY,
        FacultyName NVARCHAR(100) NOT NULL,
        Email       NVARCHAR(150) NULL,
        Department  NVARCHAR(100) NULL,
        IsActive    BIT NOT NULL DEFAULT (1)
    );
    PRINT 'Task 15: TT_Faculty created.';
END
GO

IF OBJECT_ID('dbo.TT_FacultySubjects', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_FacultySubjects
    (
        FacultySubjectID INT IDENTITY(1,1) PRIMARY KEY,
        FacultyID        INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Faculty(FacultyID),
        SubjectID        INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Subjects(SubjectID),
        DivisionID       INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Divisions(DivisionID)
    );
    CREATE UNIQUE INDEX IX_TT_FacultySubjects_Unique ON dbo.TT_FacultySubjects(FacultyID, SubjectID, DivisionID);
    PRINT 'Task 15: TT_FacultySubjects created.';
END
GO

-- Explicit "available" slots for a faculty member. A faculty member with NO rows here is
-- treated as available for every non-break working period (default-open model, so admin
-- setup stays optional for the common case) — see TimetableAvailabilityHelper.cs.
IF OBJECT_ID('dbo.TT_FacultyAvailability', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_FacultyAvailability
    (
        FacultyID INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Faculty(FacultyID),
        DayID     INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_WorkingDays(DayID),
        PeriodID  INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Periods(PeriodID),
        PRIMARY KEY (FacultyID, DayID, PeriodID)
    );
    PRINT 'Task 15: TT_FacultyAvailability created.';
END
GO

/* ---------------------------------------------------------------------
   4. ROOMS & LABS (Section D)
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.TT_Rooms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_Rooms
    (
        RoomID     INT IDENTITY(1,1) PRIMARY KEY,
        RoomNumber NVARCHAR(20)  NOT NULL,
        RoomType   NVARCHAR(15)  NOT NULL,   -- 'Classroom' | 'Laboratory'
        Capacity   INT NOT NULL DEFAULT (0),
        Building   NVARCHAR(100) NULL,
        IsActive   BIT NOT NULL DEFAULT (1),
        CONSTRAINT CK_TT_Rooms_Type CHECK (RoomType IN (N'Classroom', N'Laboratory'))
    );
    CREATE UNIQUE INDEX IX_TT_Rooms_Number ON dbo.TT_Rooms(RoomNumber);
    PRINT 'Task 15: TT_Rooms created.';
END
GO

-- Same default-open model as faculty availability: no rows = available every non-break period.
IF OBJECT_ID('dbo.TT_RoomAvailability', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_RoomAvailability
    (
        RoomID   INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Rooms(RoomID),
        DayID    INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_WorkingDays(DayID),
        PeriodID INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Periods(PeriodID),
        PRIMARY KEY (RoomID, DayID, PeriodID)
    );
    PRINT 'Task 15: TT_RoomAvailability created.';
END
GO

/* ---------------------------------------------------------------------
   5. THE TIMETABLE ITSELF (Sections E/F/G/H) — one row = one class placed
   in one (Division, Day, Period[, Batch]) slot.
--------------------------------------------------------------------- */
IF OBJECT_ID('dbo.TT_Timetable', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_Timetable
    (
        EntryID        INT IDENTITY(1,1) PRIMARY KEY,
        AcademicYearID INT NOT NULL FOREIGN KEY REFERENCES dbo.AcademicYears(AcademicYearID),
        DivisionID     INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Divisions(DivisionID),
        DayID          INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_WorkingDays(DayID),
        PeriodID       INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Periods(PeriodID),
        SubjectID      INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Subjects(SubjectID),
        FacultyID      INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Faculty(FacultyID),
        RoomID         INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Rooms(RoomID),
        BatchLabel     NVARCHAR(20) NOT NULL DEFAULT (N''),  -- '' = whole division; 'B1'/'B2' = lab batch split
        IsAutoGenerated BIT NOT NULL DEFAULT (1),
        CreatedDate    DATETIME NOT NULL DEFAULT (GETDATE()),
        ModifiedDate   DATETIME NOT NULL DEFAULT (GETDATE())
    );
    -- A division (or one of its lab batches) can only have one class in a given day/period.
    CREATE UNIQUE INDEX IX_TT_Timetable_DivisionSlot
        ON dbo.TT_Timetable(DivisionID, DayID, PeriodID, BatchLabel);
    CREATE INDEX IX_TT_Timetable_Faculty ON dbo.TT_Timetable(FacultyID, DayID, PeriodID);
    CREATE INDEX IX_TT_Timetable_Room    ON dbo.TT_Timetable(RoomID, DayID, PeriodID);
    PRINT 'Task 15: TT_Timetable created.';
END
GO

-- Conflicts the generator could not auto-resolve, surfaced to the admin (Section F).
IF OBJECT_ID('dbo.TT_UnresolvedRequirements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TT_UnresolvedRequirements
    (
        UnresolvedID    INT IDENTITY(1,1) PRIMARY KEY,
        AcademicYearID  INT NOT NULL FOREIGN KEY REFERENCES dbo.AcademicYears(AcademicYearID),
        DivisionID      INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Divisions(DivisionID),
        SubjectID       INT NOT NULL FOREIGN KEY REFERENCES dbo.TT_Subjects(SubjectID),
        Reason          NVARCHAR(400) NOT NULL,
        RunDate         DATETIME NOT NULL DEFAULT (GETDATE())
    );
    PRINT 'Task 15: TT_UnresolvedRequirements created.';
END
GO
