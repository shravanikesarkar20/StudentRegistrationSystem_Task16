/* =====================================================================
   FIX: DUPLICATE COUNTRY / STATE / DISTRICT ROWS
   -----------------------------------------------------------------------
   WHY THIS HAPPENED
   Database.sql's very first block unconditionally does:
       DROP TABLE dbo.Students; DROP TABLE dbo.Districts;
       DROP TABLE dbo.States;   DROP TABLE dbo.Countries;
   ...then unconditionally re-CREATEs and re-INSERTs seed data. That's fine
   the FIRST time the database is built. But once Database_Task12_Registration
   Fee.sql has been applied, dbo.StudentAcademicProfile / dbo.StudentFeeDemands /
   dbo.FeeTransactions all hold foreign keys into dbo.Students - so if
   Database.sql is ever re-run after that point, "DROP TABLE dbo.Students"
   fails with a foreign-key error, which means Districts/States/Countries
   fail to drop too (Students still references them). The script does NOT
   stop on that error - it carries on to "CREATE TABLE dbo.Countries" (fails,
   already exists) and then the INSERT statements, which succeed and just
   APPEND a second copy of every country/state/district on top of the
   existing rows. That's the "Country/State appearing double" symptom in the
   Registration form's dropdowns.

   WHAT THIS SCRIPT DOES
   For each of Countries -> States -> Districts (in that order, since each
   step's grouping key depends on the previous step's IDs already being
   fixed): find duplicate rows (same name, same parent), keep the
   lowest-ID copy, re-point every table that references the duplicate IDs
   over to the kept ID, then delete the now-unreferenced duplicates.

   This is idempotent - if there are no duplicates (a fresh database, or a
   database this script has already been run against), every step is a
   no-op. Safe to re-run at any time.
   ===================================================================== */

USE StudentRegistrationDB;
GO

/* ---------------------------------------------------------------------
   1. COUNTRIES - dedupe by CountryName
--------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#CountryMap') IS NOT NULL DROP TABLE #CountryMap;

SELECT CountryName, MIN(CountryID) AS KeepID
INTO #CountryMap
FROM dbo.Countries
GROUP BY CountryName
HAVING COUNT(*) > 1;

UPDATE s
SET s.CountryID = m.KeepID
FROM dbo.States s
INNER JOIN dbo.Countries c ON c.CountryID = s.CountryID
INNER JOIN #CountryMap m ON m.CountryName = c.CountryName
WHERE s.CountryID <> m.KeepID;

UPDATE st
SET st.CountryID = m.KeepID
FROM dbo.Students st
INNER JOIN dbo.Countries c ON c.CountryID = st.CountryID
INNER JOIN #CountryMap m ON m.CountryName = c.CountryName
WHERE st.CountryID <> m.KeepID;

DELETE c
FROM dbo.Countries c
INNER JOIN #CountryMap m ON m.CountryName = c.CountryName
WHERE c.CountryID <> m.KeepID;

PRINT 'Countries de-duplicated: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate row(s) removed.';
GO

/* ---------------------------------------------------------------------
   2. STATES - dedupe by (StateName, CountryID) - run AFTER Countries fix,
      so duplicate states that only differed because they pointed at two
      copies of the same country are now correctly seen as duplicates too.
--------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#StateMap') IS NOT NULL DROP TABLE #StateMap;

SELECT StateName, CountryID, MIN(StateID) AS KeepID
INTO #StateMap
FROM dbo.States
GROUP BY StateName, CountryID
HAVING COUNT(*) > 1;

UPDATE d
SET d.StateID = m.KeepID
FROM dbo.Districts d
INNER JOIN dbo.States s ON s.StateID = d.StateID
INNER JOIN #StateMap m ON m.StateName = s.StateName AND m.CountryID = s.CountryID
WHERE d.StateID <> m.KeepID;

UPDATE st
SET st.StateID = m.KeepID
FROM dbo.Students st
INNER JOIN dbo.States s ON s.StateID = st.StateID
INNER JOIN #StateMap m ON m.StateName = s.StateName AND m.CountryID = s.CountryID
WHERE st.StateID <> m.KeepID;

DELETE s
FROM dbo.States s
INNER JOIN #StateMap m ON m.StateName = s.StateName AND m.CountryID = s.CountryID
WHERE s.StateID <> m.KeepID;

PRINT 'States de-duplicated: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate row(s) removed.';
GO

/* ---------------------------------------------------------------------
   3. DISTRICTS - dedupe by (DistrictName, StateID) - run AFTER States fix.
--------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#DistrictMap') IS NOT NULL DROP TABLE #DistrictMap;

SELECT DistrictName, StateID, MIN(DistrictID) AS KeepID
INTO #DistrictMap
FROM dbo.Districts
GROUP BY DistrictName, StateID
HAVING COUNT(*) > 1;

UPDATE st
SET st.DistrictID = m.KeepID
FROM dbo.Students st
INNER JOIN dbo.Districts d ON d.DistrictID = st.DistrictID
INNER JOIN #DistrictMap m ON m.DistrictName = d.DistrictName AND m.StateID = d.StateID
WHERE st.DistrictID <> m.KeepID;

DELETE d
FROM dbo.Districts d
INNER JOIN #DistrictMap m ON m.DistrictName = d.DistrictName AND m.StateID = d.StateID
WHERE d.DistrictID <> m.KeepID;

PRINT 'Districts de-duplicated: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate row(s) removed.';
GO

PRINT 'Duplicate-location cleanup complete. Refresh the Registration form to see a single, clean Country/State/District list.';
GO
