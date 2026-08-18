using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Institute self-registration + admin approval, on top of the dbo.Institutes table
/// (originally introduced in Task 12 as a simple lookup, extended by
/// Database_InstituteRegistration.sql with Capacity/Address/contact/Courses/approval
/// columns). Follows the same shape as the Students Pending/Approved/Rejected workflow
/// in AdminDashboard.aspx.cs.
/// </summary>
public static class InstituteRegistrationHelper
{
    /// <summary>True if an institute with this name already exists (any approval status).</summary>
    public static bool IsInstituteNameTaken(string instituteName)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.Institutes WHERE LTRIM(RTRIM(InstituteName)) = LTRIM(RTRIM(@Name))",
            new SqlParameter("@Name", instituteName ?? string.Empty));
        return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Submits a new self-registration. Always lands as Pending/inactive - it only becomes
    /// selectable in the student Registration form once an admin approves it.
    /// Returns the new InstituteID.
    /// </summary>
    public static int RegisterInstitute(string instituteName, int? capacity, string address, string city,
        string contactEmail, string contactPhone, string website, string coursesOffered)
    {
        object result = DBHelper.ExecuteScalar(
            @"INSERT INTO dbo.Institutes
                (InstituteName, IsActive, Capacity, Address, City, ContactEmail, ContactPhone, Website,
                 CoursesOffered, ApprovalStatus, SubmittedDate)
              OUTPUT INSERTED.InstituteID
              VALUES
                (@InstituteName, 0, @Capacity, @Address, @City, @ContactEmail, @ContactPhone, @Website,
                 @CoursesOffered, N'Pending', GETDATE())",
            new SqlParameter("@InstituteName", instituteName),
            new SqlParameter("@Capacity", (object)capacity ?? DBNull.Value),
            new SqlParameter("@Address", (object)address ?? DBNull.Value),
            new SqlParameter("@City", (object)city ?? DBNull.Value),
            new SqlParameter("@ContactEmail", (object)contactEmail ?? DBNull.Value),
            new SqlParameter("@ContactPhone", (object)contactPhone ?? DBNull.Value),
            new SqlParameter("@Website", (object)website ?? DBNull.Value),
            new SqlParameter("@CoursesOffered", (object)coursesOffered ?? DBNull.Value));

        return Convert.ToInt32(result);
    }

    /// <summary>Institutes for the admin's Pending/Approved/Rejected/All tabs, optionally search-filtered.</summary>
    public static DataTable GetInstitutesByStatus(string status, string search)
    {
        string sql = @"
            SELECT InstituteID, InstituteName, IsActive, Capacity, Address, City, ContactEmail,
                   ContactPhone, Website, CoursesOffered, ApprovalStatus, RejectionRemark, SubmittedDate
            FROM dbo.Institutes
            WHERE (@Status = 'All' OR ApprovalStatus = @Status)
              AND (@Search = '' OR InstituteName LIKE '%' + @Search + '%' OR City LIKE '%' + @Search + '%')
            ORDER BY SubmittedDate DESC";

        return DBHelper.ExecuteQuery(sql,
            new SqlParameter("@Status", status ?? "Pending"),
            new SqlParameter("@Search", search ?? string.Empty));
    }

    /// <summary>
    /// Approves an institute: makes it selectable (IsActive = 1) in the student Registration
    /// form, and copies each non-blank line of its CoursesOffered free text into dbo.Courses
    /// (skipping any that already exist for this institute) so the Course dropdown that
    /// cascades from Institute in Register.aspx has real rows to show immediately.
    /// </summary>
    public static void ApproveInstitute(int instituteId, string approvedBy)
    {
        DBHelper.ExecuteNonQuery(
            @"UPDATE dbo.Institutes
              SET ApprovalStatus = N'Approved', IsActive = 1,
                  ApprovedBy = @ApprovedBy, ApprovedDate = GETDATE(),
                  RejectedBy = NULL, RejectedDate = NULL, RejectionRemark = NULL
              WHERE InstituteID = @InstituteID",
            new SqlParameter("@ApprovedBy", approvedBy),
            new SqlParameter("@InstituteID", instituteId));

        CreateCoursesFromOfferedList(instituteId);
    }

    public static void RejectInstitute(int instituteId, string rejectedBy, string remark)
    {
        DBHelper.ExecuteNonQuery(
            @"UPDATE dbo.Institutes
              SET ApprovalStatus = N'Rejected', IsActive = 0,
                  RejectedBy = @RejectedBy, RejectedDate = GETDATE(), RejectionRemark = @Remark
              WHERE InstituteID = @InstituteID",
            new SqlParameter("@RejectedBy", rejectedBy),
            new SqlParameter("@Remark", remark),
            new SqlParameter("@InstituteID", instituteId));
    }

    private static void CreateCoursesFromOfferedList(int instituteId)
    {
        object coursesObj = DBHelper.ExecuteScalar(
            "SELECT CoursesOffered FROM dbo.Institutes WHERE InstituteID = @InstituteID",
            new SqlParameter("@InstituteID", instituteId));

        string coursesText = coursesObj == null || coursesObj == DBNull.Value ? string.Empty : coursesObj.ToString();
        if (string.IsNullOrWhiteSpace(coursesText)) return;

        // Accept either one course per line or a comma-separated list.
        string[] rawNames = coursesText.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawName in rawNames)
        {
            string courseName = rawName.Trim();
            if (string.IsNullOrEmpty(courseName)) continue;

            object exists = DBHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM dbo.Courses WHERE InstituteID = @InstituteID AND LTRIM(RTRIM(CourseName)) = LTRIM(RTRIM(@CourseName))",
                new SqlParameter("@InstituteID", instituteId),
                new SqlParameter("@CourseName", courseName));

            if (Convert.ToInt32(exists) > 0) continue;

            DBHelper.ExecuteNonQuery(
                "INSERT INTO dbo.Courses (CourseName, InstituteID, IsActive) VALUES (@CourseName, @InstituteID, 1)",
                new SqlParameter("@CourseName", courseName),
                new SqlParameter("@InstituteID", instituteId));
        }
    }
}
