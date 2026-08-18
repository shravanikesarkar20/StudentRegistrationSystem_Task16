/* =====================================================================
   TASK 13: STUDENT MANAGEMENT SYSTEM — HOME PAGE / BANNER MANAGEMENT
   -----------------------------------------------------------------------
   Appends to Database.sql. Guarded with IF OBJECT_ID(...) IS NULL,
   matching the Task 10 / 11 / 12 migration pattern, so this is safe to
   run against the existing StudentRegistrationDB at any time (fresh
   install or an already-running database).

   Run this whole file against StudentRegistrationDB after the base
   Database.sql (and its Task 7/10/11/12 blocks) have already been
   applied.

   Stores the slide / banner images shown on the new public Home.aspx
   landing page. Admins manage everything (upload, activate/deactivate,
   delete, reorder) through the Admin Panel > Home Banners screen,
   without ever touching code — the same self-service pattern used by
   the Task 11 Advertisements module.
   ===================================================================== */

USE StudentRegistrationDB;
GO

IF OBJECT_ID('dbo.HomeBanners', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HomeBanners
    (
        BannerID     INT IDENTITY(1,1) PRIMARY KEY,
        Title        NVARCHAR(200)  NOT NULL,
        Caption      NVARCHAR(500)  NULL,
        ImagePath    NVARCHAR(500)  NOT NULL,
        DisplayOrder INT            NOT NULL DEFAULT (0),
        IsActive     BIT            NOT NULL DEFAULT (1),
        CreatedDate  DATETIME       NOT NULL DEFAULT (GETDATE()),
        UpdatedDate  DATETIME       NOT NULL DEFAULT (GETDATE())
    );

    CREATE INDEX IX_HomeBanners_DisplayOrder ON dbo.HomeBanners(DisplayOrder);
    CREATE INDEX IX_HomeBanners_IsActive ON dbo.HomeBanners(IsActive);

    PRINT 'Task 13 migration complete: HomeBanners table created.';
END
ELSE
BEGIN
    PRINT 'Task 13 migration skipped: HomeBanners table already exists.';
END
GO

-- Optional starter banners so Home.aspx has something to show immediately
-- after a fresh install, before an admin uploads real images. Safe to
-- delete/replace from the Admin Panel at any time.
IF OBJECT_ID('dbo.HomeBanners', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.HomeBanners)
BEGIN
    INSERT INTO dbo.HomeBanners (Title, Caption, ImagePath, DisplayOrder, IsActive)
    VALUES
        (N'Welcome to the Student Management System',
         N'Register, track fee payments, and manage your student profile — all in one place.',
         N'', 1, 1),
        (N'Admissions Open',
         N'Single or bulk registration is quick, secure, and OTP-verified.',
         N'', 2, 1);

    PRINT 'Task 13: HomeBanners seeded with 2 placeholder banners (no image — admin can upload one via the Admin Panel).';
END
GO
