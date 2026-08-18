using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Task 7: Authentication + password hashing helper for the Admin Panel.
/// Kept separate from DBHelper so Admin-specific queries live in one place.
/// </summary>
public static class AdminAuth
{
    /// <summary>Computes an uppercase hex SHA-256 digest of the given plain text.</summary>
    public static string ComputeHash(string plainText)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainText ?? string.Empty));
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Validates admin credentials. Returns the admin's FullName on success, or null on failure.
    /// </summary>
    public static string ValidateAdmin(string username, string password)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT FullName, PasswordHash FROM Admins WHERE Username = @Username AND IsActive = 1",
            new SqlParameter("@Username", username));

        if (dt.Rows.Count == 0) return null;

        string storedHash = dt.Rows[0]["PasswordHash"].ToString();
        string enteredHash = ComputeHash(password);

        return string.Equals(storedHash, enteredHash, StringComparison.OrdinalIgnoreCase)
            ? dt.Rows[0]["FullName"].ToString()
            : null;
    }
}
