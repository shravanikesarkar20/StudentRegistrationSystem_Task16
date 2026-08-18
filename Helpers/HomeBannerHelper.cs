using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Task 13: data-access layer for the Home Page slide/banner feature — CRUD for individual
/// banners plus active/inactive status and display order. Mirrors the style of DBHelper /
/// AdvertisementHelper already used elsewhere in the project — every statement is fully
/// parameterized (protects against SQL Injection).
/// </summary>
public static class HomeBannerHelper
{
    /// <summary>Returns every banner (active and inactive), ordered by DisplayOrder, for the
    /// Admin Panel list — optionally filtered by a title search term.</summary>
    public static DataTable GetAllBanners(string searchTerm)
    {
        searchTerm = searchTerm ?? string.Empty;

        return DBHelper.ExecuteQuery(@"
            SELECT BannerID, Title, Caption, ImagePath, DisplayOrder, IsActive,
                   CreatedDate, UpdatedDate
            FROM dbo.HomeBanners
            WHERE (@Search = '' OR Title LIKE '%' + @Search + '%')
            ORDER BY DisplayOrder ASC, BannerID ASC;",
            new SqlParameter("@Search", searchTerm));
    }

    /// <summary>Returns only the active banners, ordered for display, for the public Home
    /// Page slider.</summary>
    public static DataTable GetActiveBannersForDisplay()
    {
        return DBHelper.ExecuteQuery(@"
            SELECT BannerID, Title, Caption, ImagePath, DisplayOrder
            FROM dbo.HomeBanners
            WHERE IsActive = 1 AND ImagePath IS NOT NULL AND ImagePath <> ''
            ORDER BY DisplayOrder ASC, BannerID ASC;");
    }

    public static DataRow GetBannerById(int bannerId)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT BannerID, Title, Caption, ImagePath, DisplayOrder, IsActive, " +
            "CreatedDate, UpdatedDate FROM dbo.HomeBanners WHERE BannerID = @Id",
            new SqlParameter("@Id", bannerId));

        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public static int InsertBanner(string title, string caption, string imagePath,
        int displayOrder, bool isActive)
    {
        return DBHelper.ExecuteInsertReturnId(
            "INSERT INTO dbo.HomeBanners (Title, Caption, ImagePath, DisplayOrder, IsActive, CreatedDate, UpdatedDate) " +
            "VALUES (@Title, @Caption, @ImagePath, @DisplayOrder, @IsActive, GETDATE(), GETDATE())",
            new SqlParameter("@Title", title),
            new SqlParameter("@Caption", (object)caption ?? DBNull.Value),
            new SqlParameter("@ImagePath", (object)imagePath ?? DBNull.Value),
            new SqlParameter("@DisplayOrder", displayOrder),
            new SqlParameter("@IsActive", isActive));
    }

    /// <summary>Updates a banner. Pass null for imagePath to keep the existing image (used
    /// when the admin edits a banner without re-uploading a new image).</summary>
    public static int UpdateBanner(int bannerId, string title, string caption,
        string imagePath, int displayOrder, bool isActive)
    {
        string sql = "UPDATE dbo.HomeBanners SET Title = @Title, Caption = @Caption, " +
                     "DisplayOrder = @DisplayOrder, IsActive = @IsActive, UpdatedDate = GETDATE()";
        if (imagePath != null)
        {
            sql += ", ImagePath = @ImagePath";
        }
        sql += " WHERE BannerID = @Id";

        if (imagePath != null)
        {
            return DBHelper.ExecuteNonQuery(sql,
                new SqlParameter("@Title", title),
                new SqlParameter("@Caption", (object)caption ?? DBNull.Value),
                new SqlParameter("@DisplayOrder", displayOrder),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@ImagePath", imagePath),
                new SqlParameter("@Id", bannerId));
        }

        return DBHelper.ExecuteNonQuery(sql,
            new SqlParameter("@Title", title),
            new SqlParameter("@Caption", (object)caption ?? DBNull.Value),
            new SqlParameter("@DisplayOrder", displayOrder),
            new SqlParameter("@IsActive", isActive),
            new SqlParameter("@Id", bannerId));
    }

    public static int DeleteBanner(int bannerId)
    {
        return DBHelper.ExecuteNonQuery(
            "DELETE FROM dbo.HomeBanners WHERE BannerID = @Id",
            new SqlParameter("@Id", bannerId));
    }

    public static bool BannerExists(int bannerId)
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.HomeBanners WHERE BannerID = @Id",
            new SqlParameter("@Id", bannerId));
        return result != null && Convert.ToInt32(result) > 0;
    }

    /// <summary>Flips a single banner's active/inactive status.</summary>
    public static int SetActiveStatus(int bannerId, bool isActive)
    {
        return DBHelper.ExecuteNonQuery(
            "UPDATE dbo.HomeBanners SET IsActive = @IsActive, UpdatedDate = GETDATE() WHERE BannerID = @Id",
            new SqlParameter("@IsActive", isActive),
            new SqlParameter("@Id", bannerId));
    }

    /// <summary>Swaps DisplayOrder between two banners — used by the Admin Panel's Move Up /
    /// Move Down actions (set and update the display order of slides).</summary>
    public static void SwapDisplayOrder(int bannerIdA, int bannerIdB)
    {
        object orderAObj = DBHelper.ExecuteScalar(
            "SELECT DisplayOrder FROM dbo.HomeBanners WHERE BannerID = @Id",
            new SqlParameter("@Id", bannerIdA));
        object orderBObj = DBHelper.ExecuteScalar(
            "SELECT DisplayOrder FROM dbo.HomeBanners WHERE BannerID = @Id",
            new SqlParameter("@Id", bannerIdB));

        if (orderAObj == null || orderBObj == null) return;

        int orderA = Convert.ToInt32(orderAObj);
        int orderB = Convert.ToInt32(orderBObj);

        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.HomeBanners SET DisplayOrder = @Order, UpdatedDate = GETDATE() WHERE BannerID = @Id",
            new SqlParameter("@Order", orderB),
            new SqlParameter("@Id", bannerIdA));

        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.HomeBanners SET DisplayOrder = @Order, UpdatedDate = GETDATE() WHERE BannerID = @Id",
            new SqlParameter("@Order", orderA),
            new SqlParameter("@Id", bannerIdB));
    }

    public static int GetNextDisplayOrder()
    {
        object result = DBHelper.ExecuteScalar(
            "SELECT ISNULL(MAX(DisplayOrder), 0) + 1 FROM dbo.HomeBanners");
        return result == null || result == DBNull.Value ? 1 : Convert.ToInt32(result);
    }
}
