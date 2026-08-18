using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 16: Authentication helper for the Centralised Institute Dashboard.
/// Mirrors AdminAuth.cs (same hashing scheme, reused via AdminAuth.ComputeHash)
/// but validates against the Task 16 <c>instreg</c> table instead of Admins.
/// </summary>
public static class InstituteAuth
{
    /// <summary>
    /// Validates institute credentials against dbo.instreg. Returns the matching row
    /// (instid, instname, status) on success, or null on invalid credentials.
    /// The institute's status is returned (not checked here) so the caller can decide
    /// whether an Inactive institute is still allowed to log in and see a "contact
    /// admin" message, rather than silently failing like a wrong password would.
    /// </summary>
    public static DataRow ValidateInstitute(string instId, string password)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT instid, pwd, instname, status FROM dbo.instreg WHERE instid = @InstId",
            new SqlParameter("@InstId", instId));

        if (dt.Rows.Count == 0) return null;

        string storedHash = dt.Rows[0]["pwd"].ToString();
        string enteredHash = AdminAuth.ComputeHash(password);

        return string.Equals(storedHash, enteredHash, StringComparison.OrdinalIgnoreCase)
            ? dt.Rows[0]
            : null;
    }
}
