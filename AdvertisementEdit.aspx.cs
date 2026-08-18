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
    /// Task 11: Admin Panel &gt; Advertisements &gt; create/edit page.
    /// Requirement 2: title, description, image/banner upload, display order, active status.
    /// </summary>
    public partial class AdvertisementEdit : Page
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private string CurrentAdminName
        {
            get { return Session["AdminName"] as string; }
        }

        private int AdvertisementId
        {
            get { return string.IsNullOrEmpty(hdnAdvertisementId.Value) ? 0 : Convert.ToInt32(hdnAdvertisementId.Value); }
        }

        private bool IsEditMode
        {
            get { return AdvertisementId > 0; }
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
                    LoadAdvertisement(requestedId);
                }
                else
                {
                    hdnAdvertisementId.Value = "0";
                    txtDisplayOrder.Text = AdvertisementHelper.GetNextDisplayOrder().ToString();
                    chkIsActive.Checked = true;
                    litPageTitle.Text = "New Advertisement";
                    litBreadcrumb.Text = "New Advertisement";
                    litFormHeading.Text = "Create Advertisement";
                }
            }
        }

        private void LoadAdvertisement(int advertisementId)
        {
            try
            {
                DataRow ad = AdvertisementHelper.GetAdvertisementById(advertisementId);
                if (ad == null)
                {
                    ShowAlert("The requested advertisement could not be found. It may have been deleted.", "warning");
                    hdnAdvertisementId.Value = "0";
                    txtDisplayOrder.Text = AdvertisementHelper.GetNextDisplayOrder().ToString();
                    litPageTitle.Text = "New Advertisement";
                    litBreadcrumb.Text = "New Advertisement";
                    litFormHeading.Text = "Create Advertisement";
                    return;
                }

                hdnAdvertisementId.Value = ad["AdvertisementID"].ToString();
                txtTitle.Text = ad["Title"].ToString();
                txtDescription.Text = ad["Description"] == DBNull.Value ? string.Empty : ad["Description"].ToString();
                txtDisplayOrder.Text = ad["DisplayOrder"].ToString();
                chkIsActive.Checked = Convert.ToBoolean(ad["IsActive"]);

                string imagePath = ad["ImagePath"] == DBNull.Value ? null : ad["ImagePath"].ToString();
                if (!string.IsNullOrEmpty(imagePath))
                {
                    adImagePreview.Src = ResolveUrl("~/" + imagePath.TrimStart('~', '/'));
                }

                litPageTitle.Text = "Edit Advertisement";
                litBreadcrumb.Text = "Edit Advertisement";
                litFormHeading.Text = "Edit Advertisement \u2014 " + ad["Title"];
            }
            catch (Exception ex)
            {
                AppLogger.Error("AdvertisementEdit.LoadAdvertisement", "Failed to load advertisement " + advertisementId, ex);
                ShowAlert("Unable to load this advertisement right now. Please try again shortly.", "danger");
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                string title = txtTitle.Text.Trim();
                string description = txtDescription.Text.Trim();
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
                    if (!AdvertisementHelper.AdvertisementExists(AdvertisementId))
                    {
                        ShowAlert("This advertisement no longer exists — it may have been deleted by another admin.", "warning");
                        return;
                    }

                    AdvertisementHelper.UpdateAdvertisement(AdvertisementId, title, description, imagePath, displayOrder, isActive);
                    AppLogger.Info("AdvertisementEdit", "Advertisement " + AdvertisementId + " updated by " + CurrentAdminName);
                    Response.Redirect("AdvertisementManagement.aspx?msg=updated");
                }
                else
                {
                    if (string.IsNullOrEmpty(imagePath))
                    {
                        // Insert always writes an explicit value (possibly null) for a brand new ad.
                        imagePath = null;
                    }

                    int newId = AdvertisementHelper.InsertAdvertisement(title, description, imagePath, displayOrder, isActive);
                    AppLogger.Info("AdvertisementEdit", "Advertisement " + newId + " created by " + CurrentAdminName);
                    Response.Redirect("AdvertisementManagement.aspx?msg=created");
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // Raised internally by Response.Redirect() above on success — must propagate.
                throw;
            }
            catch (Exception ex)
            {
                AppLogger.Error("AdvertisementEdit.btnSave_Click", "Failed to save advertisement.", ex);
                ShowAlert("Could not save the advertisement due to a server error. Please try again.", "danger");
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
            double.TryParse(ConfigurationManager.AppSettings["AdvertisementImageMaxSizeMB"], out maxMb);
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

            string uploadPathSetting = ConfigurationManager.AppSettings["AdvertisementImageUploadPath"] ?? "~/Uploads/Advertisements/";
            string uploadFolder = Server.MapPath(uploadPathSetting);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Random file name — never trust the client-supplied name (path traversal / overwrite defense).
            string uniqueFileName = Guid.NewGuid().ToString("N") + ext;
            string fullPath = Path.Combine(uploadFolder, uniqueFileName);
            upload.SaveAs(fullPath);

            return "Uploads/Advertisements/" + uniqueFileName;
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
                LoadAdvertisement(AdvertisementId);
                ShowAlert("Changes reverted to the last saved version.", "info");
            }
            else
            {
                txtTitle.Text = string.Empty;
                txtDescription.Text = string.Empty;
                txtDisplayOrder.Text = AdvertisementHelper.GetNextDisplayOrder().ToString();
                chkIsActive.Checked = true;
                adImagePreview.Src = "https://via.placeholder.com/360x180?text=No+Image";
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
