using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Centralised ADO.NET data access helper for the Student Registration System.
/// </summary>
public static class DBHelper
{
    private static readonly string ConnStr =
        ConfigurationManager.ConnectionStrings["StudentDBConnection"].ConnectionString;

    public static SqlConnection GetConnection()
    {
        return new SqlConnection(ConnStr);
    }

    /// <summary>Executes a SELECT and returns a DataTable.</summary>
    public static DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
    {
        DataTable dt = new DataTable();
        using (SqlConnection conn = GetConnection())
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
        }
        return dt;
    }

    /// <summary>Executes INSERT/UPDATE/DELETE and returns rows affected.</summary>
    public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
    {
        using (SqlConnection conn = GetConnection())
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Executes INSERT and returns the newly generated identity value.</summary>
    public static int ExecuteInsertReturnId(string sql, params SqlParameter[] parameters)
    {
        using (SqlConnection conn = GetConnection())
        using (SqlCommand cmd = new SqlCommand(sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            object result = cmd.ExecuteScalar();
            return (result == null || result == DBNull.Value) ? -1 : Convert.ToInt32(result);
        }
    }

    /// <summary>Executes a scalar query.</summary>
    public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
    {
        using (SqlConnection conn = GetConnection())
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteScalar();
        }
    }

    /// <summary>
    /// Executes the same INSERT/UPDATE statement once per parameter set, all wrapped in a
    /// single SQL transaction (all-or-nothing). Used by the bulk-insert "Save All" feature.
    /// </summary>
    public static int ExecuteTransactionalBatch(string sql, System.Collections.Generic.List<SqlParameter[]> parameterSets)
    {
        int rowsAffected = 0;
        using (SqlConnection conn = GetConnection())
        {
            conn.Open();
            using (SqlTransaction txn = conn.BeginTransaction())
            {
                try
                {
                    foreach (SqlParameter[] parameters in parameterSets)
                    {
                        using (SqlCommand cmd = new SqlCommand(sql, conn, txn))
                        {
                            cmd.Parameters.AddRange(parameters);
                            rowsAffected += cmd.ExecuteNonQuery();
                        }
                    }
                    txn.Commit();
                }
                catch
                {
                    txn.Rollback();
                    throw;
                }
            }
        }
        return rowsAffected;
    }
}
