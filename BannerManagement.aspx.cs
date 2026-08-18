using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 13: Admin Panel &gt; Home Banners &gt; list page. Full admin control over the Home
    /// Page slide/banner set — add/edit/delete, activate/deactivate, and configure display
    /// order — integrated with the existing Admin Login (Session["AdminName"]).
    /// </summary>
    public partial class BannerManagement : Page
    {
        private string CurrentAdminName
        {
            get { return Session["AdminName"] as string; }
        }

        private string SearchTerm
        {
            get { return (ViewState["Banner_Search"] as string) ?? string.Empty; }
            set { ViewState["Banner_Search"] = value; }
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
                BindGrid();

                switch (Request.QueryString["msg"])
                {
                    case "created":
                        ShowAlert("Banner created successfully.", "success");
                        break;
                    case "updated":
                        ShowAlert("Banner updated successfully.", "success");
                        break;
                }
            }
        }

        private void BindGrid()
        {
            try
            {
                DataTable dt = HomeBannerHelper.GetAllBanners(SearchTerm);
                gvBanners.DataSource = dt;
                gvBanners.DataBind();

                litResultCount.Text = dt.Rows.Count == 0
                    ? "No banners found."
                    : string.Format("{0} banner{1} found.", dt.Rows.Count, dt.Rows.Count == 1 ? "" : "s");
            }
            catch (Exception ex)
            {
                AppLogger.Error("BannerManagement.BindGrid", "Failed to load banner list.", ex);
                ShowAlert("Unable to load banners right now. Please try again shortly.", "danger");
            }
        }

        protected void gvBanners_RowDataBound(object sender, GridViewRowEventArgs e)
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
                litNoThumb.Text = "<div class='banner-thumb-empty'><i class='bi bi-image'></i></div>";
            }
        }

        protected void gvBanners_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int bannerId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out bannerId)) return;

            try
            {
                if (e.CommandName == "DeleteBanner")
                {
                    int rows = HomeBannerHelper.DeleteBanner(bannerId);
                    if (rows > 0)
                    {
                        AppLogger.Info("BannerManagement", "Banner " + bannerId + " deleted by " + CurrentAdminName);
                        ShowAlert("Banner deleted successfully.", "success");
                    }
                    else
                    {
                        ShowAlert("Banner was not found — it may have already been deleted.", "warning");
                    }
                }
                else if (e.CommandName == "ToggleActive")
                {
                    DataRow banner = HomeBannerHelper.GetBannerById(bannerId);
                    if (banner == null)
                    {
                        ShowAlert("Banner was not found.", "warning");
                    }
                    else
                    {
                        bool newStatus = !Convert.ToBoolean(banner["IsActive"]);
                        HomeBannerHelper.SetActiveStatus(bannerId, newStatus);
                        AppLogger.Info("BannerManagement", "Banner " + bannerId + " set to " +
                            (newStatus ? "Active" : "Inactive") + " by " + CurrentAdminName);
                        ShowAlert("Banner is now " + (newStatus ? "active." : "inactive."), "success");
                    }
                }
                else if (e.CommandName == "MoveUp" || e.CommandName == "MoveDown")
                {
                    MoveBanner(bannerId, e.CommandName == "MoveUp");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("BannerManagement.gvBanners_RowCommand", "Command " + e.CommandName + " failed for banner " + bannerId, ex);
                ShowAlert("The action could not be completed due to a server error.", "danger");
            }

            BindGrid();
        }

        /// <summary>Set and update the display order of slides — swaps this banner's
        /// DisplayOrder with its immediate neighbour in the current ordering.</summary>
        private void MoveBanner(int bannerId, bool moveUp)
        {
            DataTable dt = HomeBannerHelper.GetAllBanners(string.Empty);

            int index = -1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (Convert.ToInt32(dt.Rows[i]["BannerID"]) == bannerId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return;

            int neighborIndex = moveUp ? index - 1 : index + 1;
            if (neighborIndex < 0 || neighborIndex >= dt.Rows.Count) return;

            int neighborId = Convert.ToInt32(dt.Rows[neighborIndex]["BannerID"]);
            HomeBannerHelper.SwapDisplayOrder(bannerId, neighborId);
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
