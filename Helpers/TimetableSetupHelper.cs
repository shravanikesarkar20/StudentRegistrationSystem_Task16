using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 15: data-access layer for Timetable "Academic Setup" — working days/periods,
/// divisions, subjects, faculty (+ their subject/class assignments and availability),
/// and rooms/labs (+ availability). Mirrors the ADO.NET/DataTable style already used by
/// DBHelper / RegistrationFeeHelper / HomeBannerHelper elsewhere in the project.
/// </summary>
public static class TimetableSetupHelper
{
    // ---------------------------------------------------------------- Working schedule

    public static DataTable GetWorkingDays()
    {
        return DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_WorkingDays ORDER BY DayOrder");
    }

    public static void SetWorkingDay(int dayId, bool isWorking)
    {
        DBHelper.ExecuteNonQuery("UPDATE dbo.TT_WorkingDays SET IsWorkingDay=@W WHERE DayID=@D",
            new SqlParameter("@W", isWorking), new SqlParameter("@D", dayId));
    }

    public static DataTable GetPeriods()
    {
        return DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_Periods ORDER BY PeriodNumber");
    }

    public static DataTable GetTeachingPeriods()
    {
        return DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_Periods WHERE IsBreak = 0 ORDER BY PeriodNumber");
    }

    public static void SavePeriod(int periodId, string label, string startTime, string endTime, bool isBreak)
    {
        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.TT_Periods SET Label=@L, StartTime=@S, EndTime=@E, IsBreak=@B WHERE PeriodID=@P",
            new SqlParameter("@L", label), new SqlParameter("@S", startTime),
            new SqlParameter("@E", endTime), new SqlParameter("@B", isBreak),
            new SqlParameter("@P", periodId));
    }

    public static int AddPeriod(int periodNumber, string label, string startTime, string endTime, bool isBreak)
    {
        return DBHelper.ExecuteInsertReturnId(
            "INSERT INTO dbo.TT_Periods (PeriodNumber, Label, StartTime, EndTime, IsBreak) VALUES (@N,@L,@S,@E,@B)",
            new SqlParameter("@N", periodNumber), new SqlParameter("@L", label),
            new SqlParameter("@S", startTime), new SqlParameter("@E", endTime), new SqlParameter("@B", isBreak));
    }

    public static void DeletePeriod(int periodId)
    {
        DBHelper.ExecuteNonQuery("DELETE FROM dbo.TT_Periods WHERE PeriodID=@P", new SqlParameter("@P", periodId));
    }

    public static int GetMaxClassesPerDay()
    {
        object result = DBHelper.ExecuteScalar("SELECT TOP 1 MaxClassesPerDay FROM dbo.TT_ScheduleConfig ORDER BY ConfigID DESC");
        return (result == null || result == DBNull.Value) ? 6 : Convert.ToInt32(result);
    }

    public static void SetMaxClassesPerDay(int value, string updatedBy)
    {
        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.TT_ScheduleConfig SET MaxClassesPerDay=@V, UpdatedBy=@U, UpdatedDate=GETDATE() WHERE ConfigID = (SELECT TOP 1 ConfigID FROM dbo.TT_ScheduleConfig ORDER BY ConfigID DESC)",
            new SqlParameter("@V", value), new SqlParameter("@U", (object)updatedBy ?? DBNull.Value));
    }

    // ---------------------------------------------------------------- Divisions

    public static DataTable GetDivisions(bool activeOnly = false)
    {
        string sql = @"SELECT d.DivisionID, d.AcademicYearID, ay.YearLabel, d.CourseID, c.CourseName,
                               d.YearSemester, d.DivisionName, d.StudentStrength, d.IsActive
                        FROM dbo.TT_Divisions d
                        INNER JOIN dbo.Courses c ON d.CourseID = c.CourseID
                        INNER JOIN dbo.AcademicYears ay ON d.AcademicYearID = ay.AcademicYearID"
                        + (activeOnly ? " WHERE d.IsActive = 1" : "")
                        + " ORDER BY ay.YearLabel DESC, c.CourseName, d.YearSemester, d.DivisionName";
        return DBHelper.ExecuteQuery(sql);
    }

    public static int SaveDivision(int divisionId, int academicYearId, int courseId, string yearSemester,
        string divisionName, int strength, bool isActive)
    {
        if (divisionId <= 0)
        {
            return DBHelper.ExecuteInsertReturnId(
                @"INSERT INTO dbo.TT_Divisions (AcademicYearID, CourseID, YearSemester, DivisionName, StudentStrength, IsActive)
                  VALUES (@AY,@C,@YS,@DN,@S,@A)",
                new SqlParameter("@AY", academicYearId), new SqlParameter("@C", courseId),
                new SqlParameter("@YS", yearSemester), new SqlParameter("@DN", divisionName),
                new SqlParameter("@S", strength), new SqlParameter("@A", isActive));
        }
        DBHelper.ExecuteNonQuery(
            @"UPDATE dbo.TT_Divisions SET AcademicYearID=@AY, CourseID=@C, YearSemester=@YS, DivisionName=@DN,
              StudentStrength=@S, IsActive=@A WHERE DivisionID=@ID",
            new SqlParameter("@AY", academicYearId), new SqlParameter("@C", courseId),
            new SqlParameter("@YS", yearSemester), new SqlParameter("@DN", divisionName),
            new SqlParameter("@S", strength), new SqlParameter("@A", isActive), new SqlParameter("@ID", divisionId));
        return divisionId;
    }

    public static DataRow GetDivision(int divisionId)
    {
        DataTable dt = DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_Divisions WHERE DivisionID=@ID", new SqlParameter("@ID", divisionId));
        return dt.Rows.Count == 0 ? null : dt.Rows[0];
    }

    // ---------------------------------------------------------------- Subjects

    public static DataTable GetSubjects(bool activeOnly = false)
    {
        string sql = @"SELECT s.SubjectID, s.SubjectCode, s.SubjectName, s.CourseID, c.CourseName,
                               s.YearSemester, s.SubjectType, s.WeeklyHours, s.IsActive
                        FROM dbo.TT_Subjects s
                        INNER JOIN dbo.Courses c ON s.CourseID = c.CourseID"
                        + (activeOnly ? " WHERE s.IsActive = 1" : "")
                        + " ORDER BY c.CourseName, s.YearSemester, s.SubjectName";
        return DBHelper.ExecuteQuery(sql);
    }

    public static DataTable GetSubjectsForDivision(int divisionId)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT s.* FROM dbo.TT_Subjects s
            INNER JOIN dbo.TT_Divisions d ON s.CourseID = d.CourseID AND s.YearSemester = d.YearSemester
            WHERE d.DivisionID = @DID AND s.IsActive = 1",
            new SqlParameter("@DID", divisionId));
    }

    public static int SaveSubject(int subjectId, string code, string name, int courseId, string yearSemester,
        string subjectType, int weeklyHours, bool isActive)
    {
        if (subjectId <= 0)
        {
            return DBHelper.ExecuteInsertReturnId(
                @"INSERT INTO dbo.TT_Subjects (SubjectCode, SubjectName, CourseID, YearSemester, SubjectType, WeeklyHours, IsActive)
                  VALUES (@CO,@N,@C,@YS,@T,@W,@A)",
                new SqlParameter("@CO", code), new SqlParameter("@N", name), new SqlParameter("@C", courseId),
                new SqlParameter("@YS", yearSemester), new SqlParameter("@T", subjectType),
                new SqlParameter("@W", weeklyHours), new SqlParameter("@A", isActive));
        }
        DBHelper.ExecuteNonQuery(
            @"UPDATE dbo.TT_Subjects SET SubjectCode=@CO, SubjectName=@N, CourseID=@C, YearSemester=@YS,
              SubjectType=@T, WeeklyHours=@W, IsActive=@A WHERE SubjectID=@ID",
            new SqlParameter("@CO", code), new SqlParameter("@N", name), new SqlParameter("@C", courseId),
            new SqlParameter("@YS", yearSemester), new SqlParameter("@T", subjectType),
            new SqlParameter("@W", weeklyHours), new SqlParameter("@A", isActive), new SqlParameter("@ID", subjectId));
        return subjectId;
    }

    public static DataRow GetSubject(int subjectId)
    {
        DataTable dt = DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_Subjects WHERE SubjectID=@ID", new SqlParameter("@ID", subjectId));
        return dt.Rows.Count == 0 ? null : dt.Rows[0];
    }

    // ---------------------------------------------------------------- Faculty

    public static DataTable GetFaculty(bool activeOnly = false)
    {
        string sql = "SELECT * FROM dbo.TT_Faculty" + (activeOnly ? " WHERE IsActive = 1" : "") + " ORDER BY FacultyName";
        return DBHelper.ExecuteQuery(sql);
    }

    public static DataRow GetFacultyById(int facultyId)
    {
        DataTable dt = DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_Faculty WHERE FacultyID=@ID", new SqlParameter("@ID", facultyId));
        return dt.Rows.Count == 0 ? null : dt.Rows[0];
    }

    public static int SaveFaculty(int facultyId, string name, string email, string department, bool isActive)
    {
        if (facultyId <= 0)
        {
            return DBHelper.ExecuteInsertReturnId(
                "INSERT INTO dbo.TT_Faculty (FacultyName, Email, Department, IsActive) VALUES (@N,@E,@D,@A)",
                new SqlParameter("@N", name), new SqlParameter("@E", (object)email ?? DBNull.Value),
                new SqlParameter("@D", (object)department ?? DBNull.Value), new SqlParameter("@A", isActive));
        }
        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.TT_Faculty SET FacultyName=@N, Email=@E, Department=@D, IsActive=@A WHERE FacultyID=@ID",
            new SqlParameter("@N", name), new SqlParameter("@E", (object)email ?? DBNull.Value),
            new SqlParameter("@D", (object)department ?? DBNull.Value), new SqlParameter("@A", isActive),
            new SqlParameter("@ID", facultyId));
        return facultyId;
    }

    public static DataTable GetFacultyAssignments(int facultyId)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT fs.FacultySubjectID, fs.SubjectID, s.SubjectCode, s.SubjectName, s.SubjectType,
                   fs.DivisionID, d.DivisionName, d.YearSemester, c.CourseName
            FROM dbo.TT_FacultySubjects fs
            INNER JOIN dbo.TT_Subjects s ON fs.SubjectID = s.SubjectID
            INNER JOIN dbo.TT_Divisions d ON fs.DivisionID = d.DivisionID
            INNER JOIN dbo.Courses c ON d.CourseID = c.CourseID
            WHERE fs.FacultyID = @FID ORDER BY c.CourseName, d.DivisionName, s.SubjectName",
            new SqlParameter("@FID", facultyId));
    }

    public static void AddFacultyAssignment(int facultyId, int subjectId, int divisionId)
    {
        object exists = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.TT_FacultySubjects WHERE FacultyID=@F AND SubjectID=@S AND DivisionID=@D",
            new SqlParameter("@F", facultyId), new SqlParameter("@S", subjectId), new SqlParameter("@D", divisionId));
        if (Convert.ToInt32(exists) > 0) return;

        DBHelper.ExecuteNonQuery(
            "INSERT INTO dbo.TT_FacultySubjects (FacultyID, SubjectID, DivisionID) VALUES (@F,@S,@D)",
            new SqlParameter("@F", facultyId), new SqlParameter("@S", subjectId), new SqlParameter("@D", divisionId));
    }

    public static void RemoveFacultyAssignment(int facultySubjectId)
    {
        DBHelper.ExecuteNonQuery("DELETE FROM dbo.TT_FacultySubjects WHERE FacultySubjectID=@ID", new SqlParameter("@ID", facultySubjectId));
    }

    public static DataTable GetFacultyAvailability(int facultyId)
    {
        return DBHelper.ExecuteQuery("SELECT DayID, PeriodID FROM dbo.TT_FacultyAvailability WHERE FacultyID=@F", new SqlParameter("@F", facultyId));
    }

    /// <summary>Replaces a faculty member's explicit availability grid in one transaction.
    /// Passing an empty list clears all rows, which reverts the faculty to the "available
    /// every non-break period" default used across the rest of the module.</summary>
    public static void SetFacultyAvailability(int facultyId, System.Collections.Generic.List<Tuple<int, int>> slots)
    {
        DBHelper.ExecuteNonQuery("DELETE FROM dbo.TT_FacultyAvailability WHERE FacultyID=@F", new SqlParameter("@F", facultyId));
        if (slots == null) return;
        foreach (var slot in slots)
        {
            DBHelper.ExecuteNonQuery(
                "INSERT INTO dbo.TT_FacultyAvailability (FacultyID, DayID, PeriodID) VALUES (@F,@D,@P)",
                new SqlParameter("@F", facultyId), new SqlParameter("@D", slot.Item1), new SqlParameter("@P", slot.Item2));
        }
    }

    // ---------------------------------------------------------------- Rooms

    public static DataTable GetRooms(bool activeOnly = false)
    {
        string sql = "SELECT * FROM dbo.TT_Rooms" + (activeOnly ? " WHERE IsActive = 1" : "") + " ORDER BY RoomType, RoomNumber";
        return DBHelper.ExecuteQuery(sql);
    }

    public static DataRow GetRoomById(int roomId)
    {
        DataTable dt = DBHelper.ExecuteQuery("SELECT * FROM dbo.TT_Rooms WHERE RoomID=@ID", new SqlParameter("@ID", roomId));
        return dt.Rows.Count == 0 ? null : dt.Rows[0];
    }

    public static int SaveRoom(int roomId, string roomNumber, string roomType, int capacity, string building, bool isActive)
    {
        if (roomId <= 0)
        {
            return DBHelper.ExecuteInsertReturnId(
                "INSERT INTO dbo.TT_Rooms (RoomNumber, RoomType, Capacity, Building, IsActive) VALUES (@N,@T,@C,@B,@A)",
                new SqlParameter("@N", roomNumber), new SqlParameter("@T", roomType), new SqlParameter("@C", capacity),
                new SqlParameter("@B", (object)building ?? DBNull.Value), new SqlParameter("@A", isActive));
        }
        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.TT_Rooms SET RoomNumber=@N, RoomType=@T, Capacity=@C, Building=@B, IsActive=@A WHERE RoomID=@ID",
            new SqlParameter("@N", roomNumber), new SqlParameter("@T", roomType), new SqlParameter("@C", capacity),
            new SqlParameter("@B", (object)building ?? DBNull.Value), new SqlParameter("@A", isActive), new SqlParameter("@ID", roomId));
        return roomId;
    }

    public static DataTable GetRoomAvailability(int roomId)
    {
        return DBHelper.ExecuteQuery("SELECT DayID, PeriodID FROM dbo.TT_RoomAvailability WHERE RoomID=@R", new SqlParameter("@R", roomId));
    }

    public static void SetRoomAvailability(int roomId, System.Collections.Generic.List<Tuple<int, int>> slots)
    {
        DBHelper.ExecuteNonQuery("DELETE FROM dbo.TT_RoomAvailability WHERE RoomID=@R", new SqlParameter("@R", roomId));
        if (slots == null) return;
        foreach (var slot in slots)
        {
            DBHelper.ExecuteNonQuery(
                "INSERT INTO dbo.TT_RoomAvailability (RoomID, DayID, PeriodID) VALUES (@R,@D,@P)",
                new SqlParameter("@R", roomId), new SqlParameter("@D", slot.Item1), new SqlParameter("@P", slot.Item2));
        }
    }
}
