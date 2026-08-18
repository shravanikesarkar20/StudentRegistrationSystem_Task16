using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 11: data-access layer for the Advertisement Modal feature — CRUD for individual
/// advertisements plus the single global on/off switch (AdvertisementSettings). Mirrors the
/// style of DBHelper / RichTextDocumentHelper already used elsewhere in the project — every
/// statement is fully parameterized (Requirement 10 equivalent: protects against SQL Injection).
/// </summary>
public static class AdvertisementHelper
{
    /// <summary>Returns every advertisement (active and inactive), ordered by DisplayOrder,
    /// for the Admin Panel list — optionally filtered by a title search term.</summary>
    public static DataTable GetAllAdvertisements(string searchTerm)
    {
        searchTerm = searchTerm ?? string.Empty;

        return DBHelper.ExecuteQuery(@"
            SELECT AdvertisementID, Title, Description, ImagePath, DisplayOrder, IsActive,
                   CreatedDate, UpdatedDate
            FROM dbo.Advertisements
            WHERE (@Search = '' OR Title LIKE '%' + @Search + '%')
            ORDER BY DisplayOrder ASC, AdvertisementID ASC;",
            new SqlParameter("@Search", searchTerm));
    }

    /// <summary>Returns only the active advertisements, ordered for display, for the public
    /// Student Registration page modal (Requirement 1/3: only active ads, in display order).</summary>
    public static DataTable GetActiveAdvertisementsForDisplay()
    {
        return DBHelper.ExecuteQuery(@"
            SELECT AdvertisementID, Title, Description, ImagePath, DisplayOrder
            FROM dbo.Advertisements
            WHERE IsActive = 1
            ORDER BY DisplayOrder ASC, AdvertisementID ASC;");
    }

    public static DataRow GetAdvertisementById(int advertisementId)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT AdvertisementID, Title, Description, ImagePath, DisplayOrder, IsActive, " +
            "CreatedDate, UpdatedDate FROM dbo.Advertisements WHERE AdvertisementID = @Id",
            new SqlParameter("@Id", advertisementId));

        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public static int InsertAdvertisement(string title, string description, string imagePath,
        int displayOrder, bool isActive)
    {
        return DBHelper.ExecuteInsertReturnId(
            "INSERT INTO dbo.Advertisements (Title, Description, ImagePath, DisplayOrder, IsActive, CreatedDate, UpdatedDate) " +
            "VALUES (@Title, @Description, @ImagePath, @DisplayOrder, @IsActive, GETDATE(), GETDATE())",
            new SqlParameter("@Title", title),
            new SqlParameter("@Description", (object)description ?? DBNull.Value),
            new SqlParameter("@ImagePath", (object)imagePath ?? DBNull.Value),
            new SqlParameter("@DisplayOrder", displayOrder),
            new SqlParameter("@IsActive", isActive));
    }

    /// <summary>Updates an advertisement. Pass null for imagePath to keep the existing image
    /// (used when the admin edits an ad without re-uploading a new banner).</summary>
    public static int UpdateAdvertisement(int advertisementId, string title, string description,
        string imagePath, int displayOrder, bool isActive)
    {
        string sql = "UPDATE dbo.Advertisements SET Title = @Title, Description = @Description, " +
                     "DisplayOrder = @DisplayOrder, IsActive = @IsActive, UpdatedDate = GETDATE()";
        if (imagePath != null)
        {
            sql += ", ImagePath = @ImagePath";
        }
        sql += " WHERE AdvertisementID = @Id";

        if (imagePath != null)
        {
            return DBHelper.ExecuteNonQuery(sql,
                new SqlParameter("@Title", title),
                new SqlParameter("@Description", (object)description ?? DBNull.Value),
                new SqlParameter("@DisplayOrder", displayOrder),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@ImagePath", imagePath),
                new SqlParameter("@Id", advertisementId));
        }

        return DBHelper.ExecuteNonQuery(sql,
            new SqlParameter("@Title", title),
            new SqlParameter("@Description", (object)description ?? DBNull.Value),
            new SqlParameter("@DisplayOrder", displayOrder),
            new SqlParameter("@IsActive", isActive),
            new SqlParameter("@Id", advertisementId));
    }

    public static int DeleteAdvertisement(int advertisementId)
    {
        return DBHelper.ExecuteNonQuery(
            "DELETE FROM dbo.Advertisements WHERE AdvertisementID = @Id",
            new SqlParameter("@Id", advertisementId));
    }

    public static bool AdvertisementExists(int advertisementId)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.Advertisements WHERE AdvertisementID = @Id",
            new SqlParameter("@Id", advertisementId));
        return result != null && Convert.ToInt32(result) > 0;
    }

    /// <summary>Flips a single advertisement's active/inactive status.</summary>
    public static int SetActiveStatus(int advertisementId, bool isActive)
    {
        return DBHelper.ExecuteNonQuery(
            "UPDATE dbo.Advertisements SET IsActive = @IsActive, UpdatedDate = GETDATE() WHERE AdvertisementID = @Id",
            new SqlParameter("@IsActive", isActive),
            new SqlParameter("@Id", advertisementId));
    }

    /// <summary>Swaps DisplayOrder between two advertisements — used by the Admin Panel's
    /// Move Up / Move Down actions (Requirement: configure display order).</summary>
    public static void SwapDisplayOrder(int advertisementIdA, int advertisementIdB)
    {
        object orderAObj = DBHelper.ExecuteScalar(
            "SELECT DisplayOrder FROM dbo.Advertisements WHERE AdvertisementID = @Id",
            new SqlParameter("@Id", advertisementIdA));
        object orderBObj = DBHelper.ExecuteScalar(
            "SELECT DisplayOrder FROM dbo.Advertisements WHERE AdvertisementID = @Id",
            new SqlParameter("@Id", advertisementIdB));

        if (orderAObj == null || orderBObj == null) return;

        int orderA = Convert.ToInt32(orderAObj);
        int orderB = Convert.ToInt32(orderBObj);

        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.Advertisements SET DisplayOrder = @Order, UpdatedDate = GETDATE() WHERE AdvertisementID = @Id",
            new SqlParameter("@Order", orderB),
            new SqlParameter("@Id", advertisementIdA));

        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.Advertisements SET DisplayOrder = @Order, UpdatedDate = GETDATE() WHERE AdvertisementID = @Id",
            new SqlParameter("@Order", orderA),
            new SqlParameter("@Id", advertisementIdB));
    }

    public static int GetNextDisplayOrder()
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT ISNULL(MAX(DisplayOrder), 0) + 1 FROM dbo.Advertisements");
        return result == null || result == DBNull.Value ? 1 : Convert.ToInt32(result);
    }

    /// <summary>Global switch: whether the advertisement modal should ever appear on the
    /// Student Registration page, regardless of individual ad status.</summary>
    public static bool IsModalGloballyEnabled()
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT ModalEnabled FROM dbo.AdvertisementSettings WHERE SettingID = 1");
        // Fail safe: if the settings row is somehow missing, default to enabled so the
        // feature still works — the row is seeded by the Task 11 migration.
        return result == null || result == DBNull.Value || Convert.ToBoolean(result);
    }

    public static void SetModalGloballyEnabled(bool enabled)
    {
        int rows = DBHelper.ExecuteNonQuery(
            "UPDATE dbo.AdvertisementSettings SET ModalEnabled = @Enabled WHERE SettingID = 1",
            new SqlParameter("@Enabled", enabled));

        if (rows == 0)
        {
            // Settings row missing for some reason (e.g. manual DB edit) — recreate it.
            DBHelper.ExecuteNonQuery(
                "INSERT INTO dbo.AdvertisementSettings (SettingID, ModalEnabled) VALUES (1, @Enabled)",
                new SqlParameter("@Enabled", enabled));
        }
    }
}
