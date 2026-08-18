using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 13: Admin Panel &gt; Home Banners &gt; create/edit page. Upload new slide/banner
    /// images, set title/caption/display order, and set active status — everything the Home
    /// Page task brief asks the Admin Panel to manage.
    /// </summary>
    public partial class BannerEdit : Page
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private string CurrentAdminName
        {
            get { return Session["AdminName"] as string; }
        }

        private int BannerId
        {
            get { return string.IsNullOrEmpty(hdnBannerId.Value) ? 0 : Convert.ToInt32(hdnBannerId.Value); }
        }

        private bool IsEditMode
        {
            get { return BannerId > 0; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            if (CurrentAdminName == null)
            {
                Response.Redirect("AdminLogin.aspx");
                return;
            }

            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack)
            {
                int requestedId;
                int.TryParse(Request.QueryString["id"], out requestedId);

                if (requestedId > 0)
                {
                    LoadBanner(requestedId);
                }
                else
                {
                    hdnBannerId.Value = "0";
                    txtDisplayOrder.Text = HomeBannerHelper.GetNextDisplayOrder().ToString();
                    chkIsActive.Checked = true;
                    litPageTitle.Text = "New Banner";
                    litBreadcrumb.Text = "New Banner";
                    litFormHeading.Text = "Create Banner";
                }
            }
        }

        private void LoadBanner(int bannerId)
        {
            try
            {
                DataRow banner = HomeBannerHelper.GetBannerById(bannerId);
                if (banner == null)
                {
                    ShowAlert("The requested banner could not be found. It may have been deleted.", "warning");
                    hdnBannerId.Value = "0";
                    txtDisplayOrder.Text = HomeBannerHelper.GetNextDisplayOrder().ToString();
                    litPageTitle.Text = "New Banner";
                    litBreadcrumb.Text = "New Banner";
                    litFormHeading.Text = "Create Banner";
                    return;
                }

                hdnBannerId.Value = banner["BannerID"].ToString();
                txtTitle.Text = banner["Title"].ToString();
                txtCaption.Text = banner["Caption"] == DBNull.Value ? string.Empty : banner["Caption"].ToString();
                txtDisplayOrder.Text = banner["DisplayOrder"].ToString();
                chkIsActive.Checked = Convert.ToBoolean(banner["IsActive"]);

                string imagePath = banner["ImagePath"] == DBNull.Value ? null : banner["ImagePath"].ToString();
                if (!string.IsNullOrEmpty(imagePath))
                {
                    bannerImagePreview.Src = ResolveUrl("~/" + imagePath.TrimStart('~', '/'));
                }

                litPageTitle.Text = "Edit Banner";
                litBreadcrumb.Text = "Edit Banner";
                litFormHeading.Text = "Edit Banner \u2014 " + banner["Title"];
            }
            catch (Exception ex)
            {
                AppLogger.Error("BannerEdit.LoadBanner", "Failed to load banner " + bannerId, ex);
                ShowAlert("Unable to load this banner right now. Please try again shortly.", "danger");
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                string title = txtTitle.Text.Trim();
                string caption = txtCaption.Text.Trim();
                int displayOrder = Convert.ToInt32(txtDisplayOrder.Text.Trim());
                bool isActive = chkIsActive.Checked;

                string imagePath = null; // null = "no new image supplied"; keep existing on update
                if (fuImage.HasFile)
                {
                    string validationError;
                    imagePath = SaveUploadedImage(fuImage, out validationError);
                    if (validationError != null)
                    {
                        lblImageError.Text = validationError;
                        return;
                    }
                }

                if (IsEditMode)
                {
                    if (!HomeBannerHelper.BannerExists(BannerId))
                    {
                        ShowAlert("This banner no longer exists — it may have been deleted by another admin.", "warning");
                        return;
                    }

                    HomeBannerHelper.UpdateBanner(BannerId, title, caption, imagePath, displayOrder, isActive);
                    AppLogger.Info("BannerEdit", "Banner " + BannerId + " updated by " + CurrentAdminName);
                    Response.Redirect("BannerManagement.aspx?msg=updated");
                }
                else
                {
                    // A brand new banner needs an image — there is nothing sensible to show in
                    // the Home Page slider otherwise (GetActiveBannersForDisplay skips blank paths).
                    if (string.IsNullOrEmpty(imagePath))
                    {
                        lblImageError.Text = "Please upload a banner image for the new slide.";
                        return;
                    }

                    int newId = HomeBannerHelper.InsertBanner(title, caption, imagePath, displayOrder, isActive);
                    AppLogger.Info("BannerEdit", "Banner " + newId + " created by " + CurrentAdminName);
                    Response.Redirect("BannerManagement.aspx?msg=created");
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // Raised internally by Response.Redirect() above on success — must propagate.
                throw;
            }
            catch (Exception ex)
            {
                AppLogger.Error("BannerEdit.btnSave_Click", "Failed to save banner.", ex);
                ShowAlert("Could not save the banner due to a server error. Please try again.", "danger");
            }
        }

        private string SaveUploadedImage(FileUpload upload, out string errorMessage)
        {
            errorMessage = null;
            string ext = Path.GetExtension(upload.FileName).ToLowerInvariant();

            if (Array.IndexOf(AllowedExtensions, ext) < 0)
            {
                errorMessage = "Only .jpg, .jpeg, .png, .gif and .webp files are allowed.";
                return null;
            }

            double maxMb = 5;
            double.TryParse(ConfigurationManager.AppSettings["HomeBannerImageMaxSizeMB"], out maxMb);
            if (maxMb <= 0) maxMb = 5;

            if (upload.PostedFile.ContentLength > maxMb * 1024 * 1024)
            {
                errorMessage = "Image size must not exceed " + maxMb + " MB.";
                return null;
            }

            if (!LooksLikeImage(upload.PostedFile))
            {
                errorMessage = "File content does not look like a valid image.";
                return null;
            }

            string uploadPathSetting = ConfigurationManager.AppSettings["HomeBannerImageUploadPath"] ?? "~/Uploads/HomeBanners/";
            string uploadFolder = Server.MapPath(uploadPathSetting);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Random file name — never trust the client-supplied name (path traversal / overwrite defense).
            string uniqueFileName = Guid.NewGuid().ToString("N") + ext;
            string fullPath = Path.Combine(uploadFolder, uniqueFileName);
            upload.SaveAs(fullPath);

            return "Uploads/HomeBanners/" + uniqueFileName;
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

                    if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true; // PNG
                    if (header[0] == 0xFF && header[1] == 0xD8) return true; // JPEG
                    if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) return true; // GIF
                    if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46) return true; // WEBP (RIFF)

                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            if (IsEditMode)
            {
                LoadBanner(BannerId);
                ShowAlert("Changes reverted to the last saved version.", "info");
            }
            else
            {
                txtTitle.Text = string.Empty;
                txtCaption.Text = string.Empty;
                txtDisplayOrder.Text = HomeBannerHelper.GetNextDisplayOrder().ToString();
                chkIsActive.Checked = true;
                bannerImagePreview.Src = "https://via.placeholder.com/420x200?text=No+Image";
                ShowAlert("Form cleared.", "info");
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("AdminLogin.aspx");
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(Server.HtmlEncode(message)));
        }
    }
}
