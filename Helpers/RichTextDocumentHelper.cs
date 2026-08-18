using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 10: data-access layer for the Rich Text Editor's document CRUD operations.
/// Mirrors the style of DBHelper/AdminAuth already used elsewhere in the project —
/// every statement is fully parameterized (Requirement 10: protects against SQL Injection).
/// </summary>
public static class RichTextDocumentHelper
{
    /// <summary>Returns one page of documents, optionally filtered by a search term, sorted
    /// by the given column/direction. Also outputs the total matching row count for paging.</summary>
    public static DataTable GetDocuments(string searchTerm, string sortColumn, string sortDirection,
        int pageIndex, int pageSize, out int totalCount)
    {
        string safeSort = MapSortColumn(sortColumn);
        string safeDir = (sortDirection == "DESC") ? "DESC" : "ASC";
        searchTerm = searchTerm ?? string.Empty;

        DataTable dt = DBHelper.ExecuteQuery(@"
            SELECT DocumentID, Title, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Status,
                   COUNT(*) OVER() AS TotalCount
            FROM dbo.RichTextDocuments
            WHERE (@Search = '' OR Title LIKE '%' + @Search + '%')
            ORDER BY " + safeSort + " " + safeDir + @"
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;",
            new SqlParameter("@Search", searchTerm),
            new SqlParameter("@Offset", pageIndex * pageSize),
            new SqlParameter("@PageSize", pageSize));

        totalCount = (dt.Rows.Count > 0) ? Convert.ToInt32(dt.Rows[0]["TotalCount"]) : 0;
        return dt;
    }

    private static string MapSortColumn(string column)
    {
        switch (column)
        {
            case "Title": return "Title";
            case "CreatedDate": return "CreatedDate";
            case "ModifiedDate": return "ModifiedDate";
            default: return "ModifiedDate";
        }
    }

    public static DataRow GetDocumentById(int documentId)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT DocumentID, Title, ContentHtml, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Status " +
            "FROM dbo.RichTextDocuments WHERE DocumentID = @Id",
            new SqlParameter("@Id", documentId));

        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public static int InsertDocument(string title, string contentHtml, string status, string createdBy)
    {
        return DBHelper.ExecuteInsertReturnId(
            "INSERT INTO dbo.RichTextDocuments (Title, ContentHtml, Status, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate) " +
            "VALUES (@Title, @Content, @Status, @CreatedBy, @CreatedBy, GETDATE(), GETDATE())",
            new SqlParameter("@Title", title),
            new SqlParameter("@Content", contentHtml),
            new SqlParameter("@Status", status),
            new SqlParameter("@CreatedBy", (object)createdBy ?? DBNull.Value));
    }

    public static int UpdateDocument(int documentId, string title, string contentHtml, string status, string modifiedBy)
    {
        return DBHelper.ExecuteNonQuery(
            "UPDATE dbo.RichTextDocuments SET Title = @Title, ContentHtml = @Content, Status = @Status, " +
            "ModifiedBy = @ModifiedBy, ModifiedDate = GETDATE() WHERE DocumentID = @Id",
            new SqlParameter("@Title", title),
            new SqlParameter("@Content", contentHtml),
            new SqlParameter("@Status", status),
            new SqlParameter("@ModifiedBy", (object)modifiedBy ?? DBNull.Value),
            new SqlParameter("@Id", documentId));
    }

    public static int DeleteDocument(int documentId)
    {
        return DBHelper.ExecuteNonQuery(
            "DELETE FROM dbo.RichTextDocuments WHERE DocumentID = @Id",
            new SqlParameter("@Id", documentId));
    }

    public static bool DocumentExists(int documentId)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.RichTextDocuments WHERE DocumentID = @Id",
            new SqlParameter("@Id", documentId));
        return result != null && Convert.ToInt32(result) > 0;
    }
}
