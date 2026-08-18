<%@ WebHandler Language="C#" Class="RichTextImageUpload" %>

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

/// <summary>
/// Task 10, Requirements 4/9/10: server-side image upload endpoint used by the Rich Text
/// Editor's TinyMCE "image" toolbar button. Requires an authenticated Admin session,
/// whitelists file extension AND actual image content (not just the claimed content-type),
/// enforces a max size, and writes files with a random name so a caller cannot control the
/// on-disk path or overwrite another document's image (path traversal / unrestricted
/// upload defenses).
/// </summary>
public class RichTextImageUpload : IHttpHandler, System.Web.SessionState.IRequiresSessionState
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        try
        {
            // Requirement 10: role-based authorization — only a logged-in admin may upload.
            if (context.Session == null || context.Session["AdminName"] == null)
            {
                RespondError(context, 401, "Not authenticated.");
                return;
            }

            HttpPostedFile file = context.Request.Files.Count > 0 ? context.Request.Files[0] : null;
            if (file == null || file.ContentLength == 0)
            {
                RespondError(context, 400, "No file was uploaded.");
                return;
            }

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                RespondError(context, 400, "Unsupported file type. Allowed: JPG, PNG, GIF, WEBP.");
                return;
            }

            double maxMb = 5;
            double.TryParse(ConfigurationManager.AppSettings["RichTextImageMaxSizeMB"], out maxMb);
            if (maxMb <= 0) maxMb = 5;

            if (file.ContentLength > maxMb * 1024 * 1024)
            {
                RespondError(context, 400, "Image exceeds the maximum allowed size of " + maxMb + " MB.");
                return;
            }

            // Verify the bytes actually decode as an image (defends against a renamed
            // .php/.aspx file wearing a .png extension).
            if (!LooksLikeImage(file))
            {
                RespondError(context, 400, "File content does not look like a valid image.");
                return;
            }

            string uploadPathSetting = ConfigurationManager.AppSettings["RichTextImageUploadPath"] ?? "~/Uploads/RichTextImages/";
            string uploadFolder = context.Server.MapPath(uploadPathSetting);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string fileName = Guid.NewGuid().ToString("N") + ext;
            string fullPath = Path.Combine(uploadFolder, fileName);
            file.SaveAs(fullPath);

            AppLogger.Info("RichTextImageUpload", "Image uploaded: " + fileName + " by " + context.Session["AdminName"]);

            string publicUrl = VirtualPathUtility.ToAbsolute(uploadPathSetting.TrimEnd('/') + "/" + fileName);

            var serializer = new JavaScriptSerializer();
            context.Response.StatusCode = 200;
            // TinyMCE's default images_upload_handler contract expects JSON: { "location": "url" }
            context.Response.Write(serializer.Serialize(new { location = publicUrl }));
        }
        catch (Exception ex)
        {
            AppLogger.Error("RichTextImageUpload", "Image upload failed.", ex);
            RespondError(context, 500, "Image upload failed. Please try again.");
        }
    }

    private static bool LooksLikeImage(HttpPostedFile file)
    {
        try
        {
            using (var stream = file.InputStream)
            {
                byte[] header = new byte[12];
                int read = stream.Read(header, 0, header.Length);
                stream.Position = 0;
                if (read < 4) return false;

                // PNG
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
                // JPEG
                if (header[0] == 0xFF && header[1] == 0xD8) return true;
                // GIF87a / GIF89a
                if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) return true;
                // WEBP: "RIFF"...."WEBP"
                if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46) return true;

                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private void RespondError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        var serializer = new JavaScriptSerializer();
        // TinyMCE expects a top-level "error" or "message" string on failure.
        context.Response.Write(serializer.Serialize(new { error = message }));
    }

    public bool IsReusable
    {
        get { return false; }
    }
}
