using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 15, Section F: central conflict-checking logic used by both the auto-generation
/// engine and the manual editor/swap function, so "the system should immediately validate
/// conflicts" holds true everywhere a slot is written, not just at generation time.
/// </summary>
public static class TimetableConflictDetector
{
    /// <summary>
    /// Checks whether placing (subjectId, facultyId, roomId) into (divisionId, dayId, periodId,
    /// batchLabel) for academicYearId is valid. excludeEntryId lets an existing row check itself
    /// (used when moving/swapping an entry that already occupies the slot it's leaving).
    /// Returns an empty list when the placement is valid.
    /// </summary>
    public static List<string> CheckConflicts(int academicYearId, int divisionId, int dayId, int periodId,
        int subjectId, int facultyId, int roomId, string batchLabel, int excludeEntryId)
    {
        var reasons = new List<string>();
        batchLabel = batchLabel ?? string.Empty;

        // -- Break period guard
        object isBreakObj = DBHelper.ExecuteScalar("SELECT IsBreak FROM dbo.TT_Periods WHERE PeriodID=@P", new SqlParameter("@P", periodId));
        if (isBreakObj != null && isBreakObj != DBNull.Value && Convert.ToBoolean(isBreakObj))
            reasons.Add("This period is a break/lunch period and cannot hold a class.");

        // -- Working day guard
        object isWorkingObj = DBHelper.ExecuteScalar("SELECT IsWorkingDay FROM dbo.TT_WorkingDays WHERE DayID=@D", new SqlParameter("@D", dayId));
        if (isWorkingObj != null && isWorkingObj != DBNull.Value && !Convert.ToBoolean(isWorkingObj))
            reasons.Add("This day is not configured as a working day.");

        // -- Room capacity vs class strength
        DataTable roomDt = DBHelper.ExecuteQuery("SELECT Capacity, RoomType FROM dbo.TT_Rooms WHERE RoomID=@R", new SqlParameter("@R", roomId));
        DataTable divDt = DBHelper.ExecuteQuery("SELECT StudentStrength FROM dbo.TT_Divisions WHERE DivisionID=@D", new SqlParameter("@D", divisionId));
        DataTable subDt = DBHelper.ExecuteQuery("SELECT SubjectType FROM dbo.TT_Subjects WHERE SubjectID=@S", new SqlParameter("@S", subjectId));

        if (roomDt.Rows.Count > 0 && divDt.Rows.Count > 0)
        {
            int capacity = Convert.ToInt32(roomDt.Rows[0]["Capacity"]);
            int strength = Convert.ToInt32(divDt.Rows[0]["StudentStrength"]);
            // A lab-batch split only brings part of the division, so capacity is checked
            // against the whole division only for whole-class (non-batch) slots.
            if (string.IsNullOrEmpty(batchLabel) && strength > capacity)
                reasons.Add(string.Format("Capacity conflict: class strength ({0}) exceeds room capacity ({1}).", strength, capacity));
        }

        // -- Practical subjects require a laboratory, not a plain classroom
        if (subDt.Rows.Count > 0 && roomDt.Rows.Count > 0)
        {
            string subjectType = subDt.Rows[0]["SubjectType"].ToString();
            string roomType = roomDt.Rows[0]["RoomType"].ToString();
            if (subjectType == "Practical" && roomType != "Laboratory")
                reasons.Add("Lab conflict: a Practical subject cannot be assigned to a non-laboratory room.");
        }

        // -- Faculty availability (explicit rows override the default-open model)
        object facHasRows = DBHelper.ExecuteScalar("SELECT COUNT(*) FROM dbo.TT_FacultyAvailability WHERE FacultyID=@F", new SqlParameter("@F", facultyId));
        if (Convert.ToInt32(facHasRows) > 0)
        {
            object avail = DBHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM dbo.TT_FacultyAvailability WHERE FacultyID=@F AND DayID=@D AND PeriodID=@P",
                new SqlParameter("@F", facultyId), new SqlParameter("@D", dayId), new SqlParameter("@P", periodId));
            if (Convert.ToInt32(avail) == 0)
                reasons.Add("Availability conflict: faculty is not marked available for this day/period.");
        }

        // -- Room availability (explicit rows override the default-open model)
        object roomHasRows = DBHelper.ExecuteScalar("SELECT COUNT(*) FROM dbo.TT_RoomAvailability WHERE RoomID=@R", new SqlParameter("@R", roomId));
        if (Convert.ToInt32(roomHasRows) > 0)
        {
            object avail = DBHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM dbo.TT_RoomAvailability WHERE RoomID=@R AND DayID=@D AND PeriodID=@P",
                new SqlParameter("@R", roomId), new SqlParameter("@D", dayId), new SqlParameter("@P", periodId));
            if (Convert.ToInt32(avail) == 0)
                reasons.Add("Availability conflict: room is not marked available for this day/period.");
        }

        // -- Faculty conflict: same faculty already teaching a different class this slot
        DataTable facClash = DBHelper.ExecuteQuery(@"
            SELECT t.EntryID, d.DivisionName FROM dbo.TT_Timetable t
            INNER JOIN dbo.TT_Divisions d ON t.DivisionID = d.DivisionID
            WHERE t.FacultyID=@F AND t.DayID=@D AND t.PeriodID=@P AND t.EntryID <> @Ex
              AND NOT (t.DivisionID=@Div AND t.BatchLabel=@BL)",
            new SqlParameter("@F", facultyId), new SqlParameter("@D", dayId), new SqlParameter("@P", periodId),
            new SqlParameter("@Ex", excludeEntryId), new SqlParameter("@Div", divisionId), new SqlParameter("@BL", batchLabel));
        if (facClash.Rows.Count > 0)
            reasons.Add(string.Format("Faculty conflict: already teaching Division {0} at this time.", facClash.Rows[0]["DivisionName"]));

        // -- Room conflict: same room already booked this slot (a different batch of the SAME
        // division+subject is allowed to reuse a lab across parallel batches, so only flag when
        // it isn't the identical division/subject/batch relationship being placed).
        DataTable roomClash = DBHelper.ExecuteQuery(@"
            SELECT t.EntryID, d.DivisionName FROM dbo.TT_Timetable t
            INNER JOIN dbo.TT_Divisions d ON t.DivisionID = d.DivisionID
            WHERE t.RoomID=@R AND t.DayID=@D AND t.PeriodID=@P AND t.EntryID <> @Ex",
            new SqlParameter("@R", roomId), new SqlParameter("@D", dayId), new SqlParameter("@P", periodId), new SqlParameter("@Ex", excludeEntryId));
        if (roomClash.Rows.Count > 0)
            reasons.Add(string.Format("Room conflict: room already booked for Division {0} at this time.", roomClash.Rows[0]["DivisionName"]));

        // -- Student/Class conflict: this division already has a class this slot (any subject)
        DataTable classClash = DBHelper.ExecuteQuery(@"
            SELECT t.EntryID, s.SubjectName FROM dbo.TT_Timetable t
            INNER JOIN dbo.TT_Subjects s ON t.SubjectID = s.SubjectID
            WHERE t.DivisionID=@Div AND t.DayID=@D AND t.PeriodID=@P AND t.BatchLabel=@BL AND t.EntryID <> @Ex",
            new SqlParameter("@Div", divisionId), new SqlParameter("@D", dayId), new SqlParameter("@P", periodId),
            new SqlParameter("@BL", batchLabel), new SqlParameter("@Ex", excludeEntryId));
        if (classClash.Rows.Count > 0)
            reasons.Add(string.Format("Class conflict: division already has {0} scheduled at this time.", classClash.Rows[0]["SubjectName"]));

        return reasons;
    }
}
