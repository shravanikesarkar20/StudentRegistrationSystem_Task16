using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 11: Admin Panel &gt; Advertisements &gt; list page.
    /// Requirement 2: full admin control — enable/disable the modal globally, add/edit/delete
    /// advertisements, configure display order, set active/inactive status per advertisement.
    /// </summary>
    public partial class AdvertisementManagement : Page
    {
        private string CurrentAdminName
        {
            get { return Session["AdminName"] as string; }
        }

        private string SearchTerm
        {
            get { return (ViewState["Ad_Search"] as string) ?? string.Empty; }
            set { ViewState["Ad_Search"] = value; }
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
                txtSearch.Text = string.Empty;
                LoadGlobalToggle();
                BindGrid();

                switch (Request.QueryString["msg"])
                {
                    case "created":
                        ShowAlert("Advertisement created successfully.", "success");
                        break;
                    case "updated":
                        ShowAlert("Advertisement updated successfully.", "success");
                        break;
                }
            }
        }

        private void LoadGlobalToggle()
        {
            try
            {
                bool enabled = AdvertisementHelper.IsModalGloballyEnabled();
                chkModalEnabled.Checked = enabled;
                litToggleLabel.Text = enabled ? "Enabled" : "Disabled";
            }
            catch (Exception ex)
            {
                AppLogger.Error("AdvertisementManagement.LoadGlobalToggle", "Failed to load modal toggle state.", ex);
            }
        }

        protected void chkModalEnabled_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                AdvertisementHelper.SetModalGloballyEnabled(chkModalEnabled.Checked);
                litToggleLabel.Text = chkModalEnabled.Checked ? "Enabled" : "Disabled";
                AppLogger.Info("AdvertisementManagement", "Advertisement modal globally " +
                    (chkModalEnabled.Checked ? "enabled" : "disabled") + " by " + CurrentAdminName);
                ShowAlert("Advertisement modal is now " + (chkModalEnabled.Checked ? "enabled." : "disabled."), "success");
            }
            catch (Exception ex)
            {
                AppLogger.Error("AdvertisementManagement.chkModalEnabled_CheckedChanged", "Failed to update modal toggle.", ex);
                ShowAlert("Could not update the setting due to a server error.", "danger");
                LoadGlobalToggle();
            }
        }

        private void BindGrid()
        {
            try
            {
                DataTable dt = AdvertisementHelper.GetAllAdvertisements(SearchTerm);
                gvAds.DataSource = dt;
                gvAds.DataBind();

                litResultCount.Text = dt.Rows.Count == 0
                    ? "No advertisements found."
                    : string.Format("{0} advertisement{1} found.", dt.Rows.Count, dt.Rows.Count == 1 ? "" : "s");
            }
            catch (Exception ex)
            {
                AppLogger.Error("AdvertisementManagement.BindGrid", "Failed to load advertisement list.", ex);
                ShowAlert("Unable to load advertisements right now. Please try again shortly.", "danger");
            }
        }

        protected void gvAds_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            DataRowView row = (DataRowView)e.Row.DataItem;

            bool isActive = Convert.ToBoolean(row["IsActive"]);
            Literal litBadge = (Literal)e.Row.FindControl("litStatusBadge");
            if (litBadge != null)
            {
                string cssClass = isActive ? "badge-active" : "badge-inactive";
                string text = isActive ? "Active" : "Inactive";
                litBadge.Text = string.Format("<span class='badge-status {0}'>{1}</span>", cssClass, text);
            }

            string imagePath = row["ImagePath"] == DBNull.Value ? null : row["ImagePath"].ToString();
            Image imgThumb = (Image)e.Row.FindControl("imgThumb");
            Literal litNoThumb = (Literal)e.Row.FindControl("litNoThumb");
            if (!string.IsNullOrEmpty(imagePath))
            {
                imgThumb.ImageUrl = ResolveUrl("~/" + imagePath.TrimStart('~', '/'));
                imgThumb.Visible = true;
            }
            else if (litNoThumb != null)
            {
                litNoThumb.Text = "<div class='ad-thumb-empty'><i class='bi bi-image'></i></div>";
            }
        }

        protected void gvAds_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int adId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out adId)) return;

            try
            {
                if (e.CommandName == "DeleteAd")
                {
                    int rows = AdvertisementHelper.DeleteAdvertisement(adId);
                    if (rows > 0)
                    {
                        AppLogger.Info("AdvertisementManagement", "Advertisement " + adId + " deleted by " + CurrentAdminName);
                        ShowAlert("Advertisement deleted successfully.", "success");
                    }
                    else
                    {
                        ShowAlert("Advertisement was not found — it may have already been deleted.", "warning");
                    }
                }
                else if (e.CommandName == "ToggleActive")
                {
                    DataRow ad = AdvertisementHelper.GetAdvertisementById(adId);
                    if (ad == null)
                    {
                        ShowAlert("Advertisement was not found.", "warning");
                    }
                    else
                    {
                        bool newStatus = !Convert.ToBoolean(ad["IsActive"]);
                        AdvertisementHelper.SetActiveStatus(adId, newStatus);
                        AppLogger.Info("AdvertisementManagement", "Advertisement " + adId + " set to " +
                            (newStatus ? "Active" : "Inactive") + " by " + CurrentAdminName);
                        ShowAlert("Advertisement is now " + (newStatus ? "active." : "inactive."), "success");
                    }
                }
                else if (e.CommandName == "MoveUp" || e.CommandName == "MoveDown")
                {
                    MoveAdvertisement(adId, e.CommandName == "MoveUp");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("AdvertisementManagement.gvAds_RowCommand", "Command " + e.CommandName + " failed for ad " + adId, ex);
                ShowAlert("The action could not be completed due to a server error.", "danger");
            }

            BindGrid();
        }

        /// <summary>Requirement: configure the display order of advertisements. Swaps this
        /// advertisement's DisplayOrder with its immediate neighbour in the current ordering.</summary>
        private void MoveAdvertisement(int adId, bool moveUp)
        {
            DataTable dt = AdvertisementHelper.GetAllAdvertisements(string.Empty);

            int index = -1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (Convert.ToInt32(dt.Rows[i]["AdvertisementID"]) == adId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return;

            int neighborIndex = moveUp ? index - 1 : index + 1;
            if (neighborIndex < 0 || neighborIndex >= dt.Rows.Count) return;

            int neighborId = Convert.ToInt32(dt.Rows[neighborIndex]["AdvertisementID"]);
            AdvertisementHelper.SwapDisplayOrder(adId, neighborId);
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchTerm = txtSearch.Text.Trim();
            BindGrid();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            SearchTerm = string.Empty;
            BindGrid();
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
