using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Admin Panel: approve/reject institute self-registrations, and toggle an approved
    /// institute's visibility on the student Registration form. Mirrors AdminDashboard.aspx's
    /// Pending/Approved/Rejected/All tab pattern for student candidates.
    /// </summary>
    public partial class InstituteManagement : Page
    {
        private const string SESSION_ADMIN_NAME = "AdminName";
        private const string SESSION_ADMIN_USERNAME = "AdminUsername";
        private const string SESSION_TAB = "InstTab";
        private const string SESSION_SEARCH = "InstSearchTerm";

        private string CurrentAdminName { get { return Session[SESSION_ADMIN_NAME] as string; } }
        private string CurrentAdminUsername { get { return Session[SESSION_ADMIN_USERNAME] as string; } }

        private string CurrentTab
        {
            get { return Session[SESSION_TAB] as string ?? "Pending"; }
            set { Session[SESSION_TAB] = value; }
        }

        private string CurrentSearch
        {
            get { return Session[SESSION_SEARCH] as string ?? string.Empty; }
            set { Session[SESSION_SEARCH] = value; }
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
                CurrentTab = "Pending";
                CurrentSearch = string.Empty;
                txtSearch.Text = string.Empty;
                BindGrid();
            }

            HighlightActiveTab();
        }

        protected void TabButton_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            CurrentTab = btn.CommandArgument;
            BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            CurrentSearch = txtSearch.Text.Trim();
            BindGrid();
        }

        private void HighlightActiveTab()
        {
            lnkTabPending.CssClass = "nav-link" + (CurrentTab == "Pending" ? " active-tab" : "");
            lnkTabApproved.CssClass = "nav-link" + (CurrentTab == "Approved" ? " active-tab" : "");
            lnkTabRejected.CssClass = "nav-link" + (CurrentTab == "Rejected" ? " active-tab" : "");
            lnkTabAll.CssClass = "nav-link" + (CurrentTab == "All" ? " active-tab" : "");
            txtSearch.Text = CurrentSearch;
        }

        private void BindGrid()
        {
            try
            {
                DataTable dt = InstituteRegistrationHelper.GetInstitutesByStatus(CurrentTab, CurrentSearch);
                gvInstitutes.DataSource = dt;
                gvInstitutes.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("InstituteManagement.BindGrid: database error - " + ex.Message);
                ShowAlert("We couldn't load the institute list right now. Please refresh the page in a moment.", "danger");
                gvInstitutes.DataSource = null;
                gvInstitutes.DataBind();
            }

            HighlightActiveTab();
        }

        protected void gvInstitutes_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            DataRowView rowView = (DataRowView)e.Row.DataItem;
            string approval = rowView["ApprovalStatus"].ToString();
            bool isActive = rowView["IsActive"] != DBNull.Value && Convert.ToBoolean(rowView["IsActive"]);
            string remark = rowView["RejectionRemark"] == DBNull.Value ? "" : rowView["RejectionRemark"].ToString();

            Literal litApprovalBadge = (Literal)e.Row.FindControl("litApprovalBadge");
            Literal litActiveBadge = (Literal)e.Row.FindControl("litActiveBadge");

            litApprovalBadge.Text = "<span class=\"badge-status badge-" + approval.ToLower() + "\">" + approval + "</span>";
            litActiveBadge.Text = "<span class=\"badge-status badge-" + (isActive ? "active" : "inactive") + "\">" + (isActive ? "Active" : "Inactive") + "</span>";

            LinkButton btnApprove = (LinkButton)e.Row.FindControl("btnApprove");
            LinkButton btnReject = (LinkButton)e.Row.FindControl("btnReject");
            LinkButton btnActivate = (LinkButton)e.Row.FindControl("btnActivate");
            LinkButton btnDeactivate = (LinkButton)e.Row.FindControl("btnDeactivate");
            Panel pnlRemark = (Panel)e.Row.FindControl("pnlRemark");
            Literal litRemark = (Literal)e.Row.FindControl("litRemark");

            bool isPending = approval == "Pending";
            bool isApproved = approval == "Approved";
            bool isRejected = approval == "Rejected";

            btnApprove.Visible = isPending || isRejected; // allow re-approving a previously rejected institute
            btnReject.Visible = isPending || isApproved;
            btnActivate.Visible = isApproved && !isActive;
            btnDeactivate.Visible = isApproved && isActive;

            if (isRejected && !string.IsNullOrEmpty(remark))
            {
                pnlRemark.Visible = true;
                litRemark.Text = Server.HtmlEncode(remark);
            }
        }

        protected void gvInstitutes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "OpenReject") return; // handled client-side (opens modal)

            int instituteId;
            if (!int.TryParse(e.CommandArgument.ToString(), out instituteId)) return;

            switch (e.CommandName)
            {
                case "Approve":
                    InstituteRegistrationHelper.ApproveInstitute(instituteId, CurrentAdminUsername);
                    ShowAlert("Institute ID " + instituteId + " approved. It now appears in the student Registration form.", "success");
                    break;
                case "Activate":
                    SetActive(instituteId, true);
                    ShowAlert("Institute ID " + instituteId + " activated.", "info");
                    break;
                case "Deactivate":
                    SetActive(instituteId, false);
                    ShowAlert("Institute ID " + instituteId + " deactivated.", "info");
                    break;
            }

            BindGrid();
        }

        private void SetActive(int instituteId, bool active)
        {
            DBHelper.ExecuteNonQuery(
                "UPDATE dbo.Institutes SET IsActive = @IsActive WHERE InstituteID = @InstituteID",
                new SqlParameter("@IsActive", active ? 1 : 0),
                new SqlParameter("@InstituteID", instituteId));
        }

        protected void btnConfirmReject_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int instituteId;
            if (!int.TryParse(hdnRejectInstituteId.Value, out instituteId))
            {
                ShowAlert("Could not identify the institute to reject. Please try again.", "danger");
                return;
            }

            string remark = txtRejectRemark.Text.Trim();
            if (string.IsNullOrEmpty(remark))
            {
                ShowAlert("A rejection remark is required.", "danger");
                return;
            }

            InstituteRegistrationHelper.RejectInstitute(instituteId, CurrentAdminUsername, remark);

            txtRejectRemark.Text = string.Empty;
            hdnRejectInstituteId.Value = string.Empty;

            ShowAlert("Institute ID " + instituteId + " rejected.", "warning");
            BindGrid();
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Remove(SESSION_ADMIN_NAME);
            Session.Remove(SESSION_ADMIN_USERNAME);
            Session.Remove(SESSION_TAB);
            Session.Remove(SESSION_SEARCH);
            Response.Redirect("AdminLogin.aspx");
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(message));
            pnlAlert.CssClass = "alert alert-" + type + " py-2 small";
        }

        /// <summary>Escapes a string for safe use inside a single-quoted inline JS call built during data binding.</summary>
        protected static string JsStringLiteral(string value)
        {
            if (value == null) return "''";
            string escaped = value.Replace("\\", "\\\\").Replace("'", "\\'");
            return "'" + escaped + "'";
        }
    }
}
