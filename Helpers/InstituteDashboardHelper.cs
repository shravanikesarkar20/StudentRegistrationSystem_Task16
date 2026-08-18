using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 16: Data access for the Centralised Institute Dashboard.
/// Every query is scoped by the logged-in institute's instid (or, for the module detail
/// drill-down, by the InstituteID resolved from instreg.instname), so one institute can
/// never see another institute's data - the mandatory isolation requirement in the brief.
/// </summary>
public static class InstituteDashboardHelper
{
    /// <summary>One label/value pair shown as a stat tile on a module detail view.</summary>
    public class ModuleStat
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public ModuleStat(string label, string value) { Label = label; Value = value; }
    }

    /// <summary>Everything needed to render one module's detail section.</summary>
    public class ModuleDetail
    {
        public string ModuleName { get; set; }
        public List<ModuleStat> Stats { get; set; } = new List<ModuleStat>();
        public string RecentSectionTitle { get; set; }
        public DataTable RecentRecords { get; set; }
        public string Note { get; set; }
    }

    /// <summary>Loads the logged-in institute's profile row (instid, instname, status).</summary>
    public static DataRow GetInstituteProfile(string instId)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT instid, instname, status FROM dbo.instreg WHERE instid = @InstId",
            new SqlParameter("@InstId", instId));
        return dt.Rows.Count == 0 ? null : dt.Rows[0];
    }

    /// <summary>
    /// Active modules assigned to the given institute (mandatory: inactive modules are
    /// never returned, and only this institute's own rows are ever queried).
    /// </summary>
    public static DataTable GetActiveModules(string instId)
    {
        return DBHelper.ExecuteQuery(
            "SELECT modulename FROM dbo.modules WHERE instid = @InstId AND status = 'Active' ORDER BY modulename",
            new SqlParameter("@InstId", instId));
    }

    /// <summary>Total count of active modules assigned to the institute.</summary>
    public static int GetActiveModuleCount(string instId)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.modules WHERE instid = @InstId AND status = 'Active'",
            new SqlParameter("@InstId", instId));
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    /// <summary>
    /// Confirms the requested module is actually an active module of the logged-in institute
    /// before any detail is shown. Blocks both "module belongs to someone else" and
    /// "module exists but is inactive" from ever reaching the detail view.
    /// </summary>
    public static bool IsActiveModuleForInstitute(string instId, string moduleName)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.modules WHERE instid = @InstId AND modulename = @ModuleName AND status = 'Active'",
            new SqlParameter("@InstId", instId), new SqlParameter("@ModuleName", moduleName));
        return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Resolves the legacy numeric dbo.Institutes.InstituteID for this login by matching
    /// instreg.instname against Institutes.InstituteName. That lookup table (from Task 12)
    /// is what the Student/Fees/Timetable modules are already keyed on, so this is how the
    /// dashboard reaches real, institute-specific data instead of hardcoding numbers.
    /// Returns null if this institute's name isn't linked to any academic data yet.
    /// </summary>
    private static int? ResolveLinkedInstituteId(string instName)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT InstituteID FROM dbo.Institutes WHERE LTRIM(RTRIM(InstituteName)) = LTRIM(RTRIM(@InstName))",
            new SqlParameter("@InstName", instName ?? string.Empty));
        return (result == null || result == DBNull.Value) ? (int?)null : Convert.ToInt32(result);
    }

    /// <summary>
    /// Builds the dynamic, institute-specific detail view for one module. The caller must
    /// already have verified (via IsActiveModuleForInstitute) that this module belongs to
    /// the logged-in institute - this method assumes that check has passed.
    /// </summary>
    public static ModuleDetail GetModuleDetail(string instId, string instName, string moduleName)
    {
        int? instituteId = ResolveLinkedInstituteId(instName);

        switch ((moduleName ?? string.Empty).Trim())
        {
            case "Student Management":
                return GetStudentManagementDetail(instituteId);
            case "Fees Management":
                return GetFeesManagementDetail(instituteId);
            case "Timetable Management":
                return GetTimetableManagementDetail(instituteId);
            default:
                // Full freedom per the brief: any module name the admin adds to dbo.modules
                // that isn't one of the three above still renders a valid (if generic) page
                // instead of erroring out.
                return new ModuleDetail
                {
                    ModuleName = moduleName,
                    Note = "No detailed statistics have been configured for this module yet."
                };
        }
    }

    private static ModuleDetail GetStudentManagementDetail(int? instituteId)
    {
        var detail = new ModuleDetail { ModuleName = "Student Management", RecentSectionTitle = "Recent Registrations" };

        if (instituteId == null)
        {
            detail.Note = "This institute isn't linked to any academic records yet.";
            detail.Stats.Add(new ModuleStat("Total Students", "0"));
            return detail;
        }

        DataTable stats = DBHelper.ExecuteQuery(
            @"SELECT
                COUNT(*)                                                      AS TotalStudents,
                SUM(CASE WHEN s.AccountStatus = 'Active' THEN 1 ELSE 0 END)   AS ActiveStudents,
                SUM(CASE WHEN s.ApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingApplications,
                SUM(CASE WHEN s.ApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedStudents
              FROM dbo.Students s
              INNER JOIN dbo.StudentAcademicProfile sap ON sap.StudentID = s.StudentID
              WHERE sap.InstituteID = @InstituteID",
            new SqlParameter("@InstituteID", instituteId.Value));

        DataRow r = stats.Rows[0];
        detail.Stats.Add(new ModuleStat("Total Students", Convert.ToString(r["TotalStudents"] ?? 0)));
        detail.Stats.Add(new ModuleStat("Active Students", Convert.ToString(r["ActiveStudents"] ?? 0)));
        detail.Stats.Add(new ModuleStat("Pending Applications", Convert.ToString(r["PendingApplications"] ?? 0)));
        detail.Stats.Add(new ModuleStat("Approved Students", Convert.ToString(r["ApprovedStudents"] ?? 0)));

        detail.RecentRecords = DBHelper.ExecuteQuery(
            @"SELECT TOP 5
                s.FullName AS [Student Name], s.Email AS [Email], s.ApprovalStatus AS [Approval Status],
                CONVERT(VARCHAR(11), s.RegistrationDate, 106) AS [Registered On]
              FROM dbo.Students s
              INNER JOIN dbo.StudentAcademicProfile sap ON sap.StudentID = s.StudentID
              WHERE sap.InstituteID = @InstituteID
              ORDER BY s.RegistrationDate DESC",
            new SqlParameter("@InstituteID", instituteId.Value));

        return detail;
    }

    private static ModuleDetail GetFeesManagementDetail(int? instituteId)
    {
        var detail = new ModuleDetail { ModuleName = "Fees Management", RecentSectionTitle = "Recent Transactions" };

        if (instituteId == null)
        {
            detail.Note = "This institute isn't linked to any academic records yet.";
            detail.Stats.Add(new ModuleStat("Total Fees Demand", "0"));
            return detail;
        }

        DataTable stats = DBHelper.ExecuteQuery(
            @"SELECT
                ISNULL(SUM(fd.GrossAmount - fd.DiscountAmount), 0) AS TotalDemand,
                ISNULL(SUM(fd.AmountPaid), 0)                      AS TotalCollected,
                ISNULL(SUM(fd.GrossAmount - fd.DiscountAmount - fd.AmountPaid), 0) AS TotalPending
              FROM dbo.StudentFeeDemands fd
              INNER JOIN dbo.Students s ON s.StudentID = fd.StudentID
              INNER JOIN dbo.StudentAcademicProfile sap ON sap.StudentID = s.StudentID
              WHERE sap.InstituteID = @InstituteID",
            new SqlParameter("@InstituteID", instituteId.Value));

        DataRow r = stats.Rows[0];
        detail.Stats.Add(new ModuleStat("Total Fees Demand", "Rs. " + Convert.ToDecimal(r["TotalDemand"]).ToString("N2")));
        detail.Stats.Add(new ModuleStat("Collected Fees", "Rs. " + Convert.ToDecimal(r["TotalCollected"]).ToString("N2")));
        detail.Stats.Add(new ModuleStat("Pending Fees", "Rs. " + Convert.ToDecimal(r["TotalPending"]).ToString("N2")));

        detail.RecentRecords = DBHelper.ExecuteQuery(
            @"SELECT TOP 5
                s.FullName AS [Student Name], ft.TransactionRef AS [Receipt No.],
                ft.Amount AS [Amount], ft.PaymentMode AS [Mode],
                CONVERT(VARCHAR(11), ft.PaymentDate, 106) AS [Paid On]
              FROM dbo.FeeTransactions ft
              INNER JOIN dbo.Students s ON s.StudentID = ft.StudentID
              INNER JOIN dbo.StudentAcademicProfile sap ON sap.StudentID = s.StudentID
              WHERE sap.InstituteID = @InstituteID
              ORDER BY ft.PaymentDate DESC",
            new SqlParameter("@InstituteID", instituteId.Value));

        return detail;
    }

    private static ModuleDetail GetTimetableManagementDetail(int? instituteId)
    {
        var detail = new ModuleDetail { ModuleName = "Timetable Management", RecentSectionTitle = "Recently Updated Classes" };

        if (instituteId == null)
        {
            detail.Note = "This institute isn't linked to any academic records yet.";
            detail.Stats.Add(new ModuleStat("Active Divisions", "0"));
            return detail;
        }

        DataTable stats = DBHelper.ExecuteQuery(
            @"SELECT
                (SELECT COUNT(*) FROM dbo.TT_Divisions dv INNER JOIN dbo.Courses c ON c.CourseID = dv.CourseID
                    WHERE c.InstituteID = @InstituteID AND dv.IsActive = 1)              AS ActiveDivisions,
                (SELECT COUNT(*) FROM dbo.TT_Timetable tt INNER JOIN dbo.TT_Divisions dv ON dv.DivisionID = tt.DivisionID
                    INNER JOIN dbo.Courses c ON c.CourseID = dv.CourseID
                    WHERE c.InstituteID = @InstituteID)                                  AS ScheduledClasses,
                (SELECT COUNT(*) FROM dbo.TT_Subjects sub INNER JOIN dbo.Courses c ON c.CourseID = sub.CourseID
                    WHERE c.InstituteID = @InstituteID AND sub.IsActive = 1)              AS ActiveSubjects",
            new SqlParameter("@InstituteID", instituteId.Value));

        DataRow r = stats.Rows[0];
        detail.Stats.Add(new ModuleStat("Active Divisions", Convert.ToString(r["ActiveDivisions"] ?? 0)));
        detail.Stats.Add(new ModuleStat("Scheduled Classes / Week", Convert.ToString(r["ScheduledClasses"] ?? 0)));
        detail.Stats.Add(new ModuleStat("Active Subjects", Convert.ToString(r["ActiveSubjects"] ?? 0)));

        detail.RecentRecords = DBHelper.ExecuteQuery(
            @"SELECT TOP 5
                dv.DivisionName AS [Division], sub.SubjectName AS [Subject],
                f.FacultyName AS [Faculty], wd.DayName AS [Day],
                CONVERT(VARCHAR(16), tt.ModifiedDate, 113) AS [Last Updated]
              FROM dbo.TT_Timetable tt
              INNER JOIN dbo.TT_Divisions dv ON dv.DivisionID = tt.DivisionID
              INNER JOIN dbo.Courses c ON c.CourseID = dv.CourseID
              INNER JOIN dbo.TT_Subjects sub ON sub.SubjectID = tt.SubjectID
              INNER JOIN dbo.TT_Faculty f ON f.FacultyID = tt.FacultyID
              INNER JOIN dbo.TT_WorkingDays wd ON wd.DayID = tt.DayID
              WHERE c.InstituteID = @InstituteID
              ORDER BY tt.ModifiedDate DESC",
            new SqlParameter("@InstituteID", instituteId.Value));

        return detail;
    }
}
