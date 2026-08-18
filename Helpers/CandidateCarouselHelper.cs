using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 14: data-access layer for the Home Page "Registered Active Candidates" carousel.
/// Mirrors the style of HomeBannerHelper / DBHelper already used elsewhere in the project —
/// every statement is fully parameterized (protects against SQL Injection).
///
/// "Active candidate" = a student whose registration has been approved by the admin and whose
/// account is currently active (see Database.sql, Task 7 migration, for the ApprovalStatus /
/// AccountStatus columns this relies on).
/// </summary>
public static class CandidateCarouselHelper
{
    /// <summary>Returns the most recently registered active (Approved + Active) candidates,
    /// with photo and a human-readable location, for the public Home Page carousel.</summary>
    public static DataTable GetActiveCandidatesForDisplay(int maxCandidates = 30)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT TOP (@Max)
                   s.StudentID,
                   s.FullName,
                   s.PhotoPath,
                   s.RegistrationDate,
                   (d.DistrictName + ', ' + st.StateName) AS Location
            FROM dbo.Students s
            INNER JOIN dbo.Districts d ON s.DistrictID = d.DistrictID
            INNER JOIN dbo.States st   ON s.StateID    = st.StateID
            WHERE s.ApprovalStatus = 'Approved' AND s.AccountStatus = 'Active'
            ORDER BY s.RegistrationDate DESC;",
            new SqlParameter("@Max", maxCandidates));
    }

    /// <summary>Quick existence/count check — used to decide whether the carousel or the
    /// empty-state panel should render, without pulling the whole result set.</summary>
    public static int GetActiveCandidateCount()
    {
        object result = DBHelper.ExecuteScalar(@"
            SELECT COUNT(*) FROM dbo.Students
            WHERE ApprovalStatus = 'Approved' AND AccountStatus = 'Active';");
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }
}
