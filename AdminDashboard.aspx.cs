using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    public partial class AdminDashboard : Page
    {
        private const string SESSION_ADMIN_NAME = "AdminName";
        private const string SESSION_ADMIN_USERNAME = "AdminUsername";
        private const string SESSION_TAB = "AdminTab";
        private const string SESSION_SEARCH = "AdminSearchTerm";

        private string CurrentAdminName
        {
            get { return Session[SESSION_ADMIN_NAME] as string; }
        }

        private string CurrentAdminUsername
        {
            get { return Session[SESSION_ADMIN_USERNAME] as string; }
        }

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
            // Task 8, Requirement 3/7: protected page — never cached, so a browser "Back" after
            // logout can't resurrect it, and every request re-checks the session.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            // Task 7/8, Requirement 3: only authenticated administrators may reach this page.
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
                LoadStats();
                BindGrid();
            }

            HighlightActiveTab();
        }

        #region ---- Dashboard Stats (Requirement 3) ----

        private void LoadStats()
        {
            DataTable dt;
            try
            {
                dt = DBHelper.ExecuteQuery(@"
                    SELECT
                        COUNT(*) AS TotalRegistered,
                        SUM(CASE WHEN AccountStatus = 'Active'   THEN 1 ELSE 0 END) AS TotalActive,
                        SUM(CASE WHEN AccountStatus = 'Inactive' THEN 1 ELSE 0 END) AS TotalInactive,
                        SUM(CASE WHEN ApprovalStatus = 'Pending'  THEN 1 ELSE 0 END) AS TotalPending,
                        SUM(CASE WHEN ApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS TotalApproved,
                        SUM(CASE WHEN ApprovalStatus = 'Rejected' THEN 1 ELSE 0 END) AS TotalRejected
                    FROM Students");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("AdminDashboard.LoadStats: database error - " + ex.Message);
                ShowAlert("We couldn't load the dashboard statistics right now. Please refresh the page in a moment.", "danger");
                return;
            }

            if (dt.Rows.Count == 0) return;
            DataRow row = dt.Rows[0];

            litTotalRegistered.Text = row["TotalRegistered"].ToString();
            litActive.Text = row["TotalActive"] == DBNull.Value ? "0" : row["TotalActive"].ToString();
            litInactive.Text = row["TotalInactive"] == DBNull.Value ? "0" : row["TotalInactive"].ToString();
            litPending.Text = row["TotalPending"] == DBNull.Value ? "0" : row["TotalPending"].ToString();
            litApproved.Text = row["TotalApproved"] == DBNull.Value ? "0" : row["TotalApproved"].ToString();
            litRejected.Text = row["TotalRejected"] == DBNull.Value ? "0" : row["TotalRejected"].ToString();
        }

        #endregion

        #region ---- Tabs + Search ----

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

        #endregion

        #region ---- Grid Binding (Requirement 4/7) ----

        private void BindGrid()
        {
            string sql = @"
                SELECT StudentID, FullName, Email, Mobile, RegistrationDate,
                       ApprovalStatus, AccountStatus, RejectionRemark
                FROM Students
                WHERE (@Tab = 'All' OR ApprovalStatus = @Tab)
                  AND (@Search = '' OR FullName LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%'
                       OR CAST(StudentID AS NVARCHAR(20)) LIKE '%' + @Search + '%')
                ORDER BY RegistrationDate DESC";

            try
            {
                DataTable dt = DBHelper.ExecuteQuery(sql,
                    new SqlParameter("@Tab", CurrentTab),
                    new SqlParameter("@Search", CurrentSearch));

                gvCandidates.DataSource = dt;
                gvCandidates.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("AdminDashboard.BindGrid: database error - " + ex.Message);
                ShowAlert("We couldn't load the candidate list right now. Please refresh the page in a moment.", "danger");
                gvCandidates.DataSource = null;
                gvCandidates.DataBind();
            }

            HighlightActiveTab();
        }

        protected void gvCandidates_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            DataRowView rowView = (DataRowView)e.Row.DataItem;
            string approval = rowView["ApprovalStatus"].ToString();
            string account = rowView["AccountStatus"].ToString();
            string remark = rowView["RejectionRemark"] == DBNull.Value ? "" : rowView["RejectionRemark"].ToString();

            // ---- Status badges ----
            Literal litApprovalBadge = (Literal)e.Row.FindControl("litApprovalBadge");
            Literal litAccountBadge = (Literal)e.Row.FindControl("litAccountBadge");

            litApprovalBadge.Text = "<span class=\"badge-status badge-" + approval.ToLower() + "\">" + approval + "</span>";
            litAccountBadge.Text = "<span class=\"badge-status badge-" + account.ToLower() + "\">" + account + "</span>";

            // ---- Contextual action buttons ----
            LinkButton btnApprove = (LinkButton)e.Row.FindControl("btnApprove");
            LinkButton btnReject = (LinkButton)e.Row.FindControl("btnReject");
            LinkButton btnActivate = (LinkButton)e.Row.FindControl("btnActivate");
            LinkButton btnDeactivate = (LinkButton)e.Row.FindControl("btnDeactivate");
            LinkButton btnReset = (LinkButton)e.Row.FindControl("btnReset");
            Panel pnlRemark = (Panel)e.Row.FindControl("pnlRemark");
            Literal litRemark = (Literal)e.Row.FindControl("litRemark");

            bool isPending = approval == "Pending";
            bool isApproved = approval == "Approved";
            bool isRejected = approval == "Rejected";

            btnApprove.Visible = isPending;
            btnReject.Visible = isPending;
            btnReset.Visible = isRejected;

            // Activate/Deactivate only make sense once an application has been approved.
            btnActivate.Visible = isApproved && account == "Inactive";
            btnDeactivate.Visible = isApproved && account == "Active";

            if (isRejected && !string.IsNullOrEmpty(remark))
            {
                pnlRemark.Visible = true;
                litRemark.Text = Server.HtmlEncode(remark);
            }
        }

        #endregion

        #region ---- Row Actions: Approve / Activate / Deactivate / Reset (Requirements 5-8) ----

        protected void gvCandidates_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "OpenReject") return; // handled entirely client-side (opens modal)

            int studentId;
            if (!int.TryParse(e.CommandArgument.ToString(), out studentId)) return;

            switch (e.CommandName)
            {
                case "Approve":
                    ApproveStudent(studentId);
                    break;
                case "Activate":
                    SetAccountStatus(studentId, "Active");
                    break;
                case "Deactivate":
                    SetAccountStatus(studentId, "Inactive");
                    break;
                case "Reset":
                    ResetApplication(studentId);
                    break;
            }

            LoadStats();
            BindGrid();
        }

        private void ApproveStudent(int studentId)
        {
            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT FullName, Email FROM Students WHERE StudentID = @StudentID",
                new SqlParameter("@StudentID", studentId));
            if (dt.Rows.Count == 0) return;

            DBHelper.ExecuteNonQuery(@"
                UPDATE Students
                SET ApprovalStatus = 'Approved',
                    ApprovedBy = @ApprovedBy,
                    ApprovedDate = GETDATE(),
                    RejectedBy = NULL,
                    RejectedDate = NULL,
                    RejectionRemark = NULL,
                    LastModifiedDate = GETDATE()
                WHERE StudentID = @StudentID",
                new SqlParameter("@ApprovedBy", CurrentAdminUsername),
                new SqlParameter("@StudentID", studentId));

            TrySendApprovalEmail(studentId, dt.Rows[0]["Email"].ToString(), dt.Rows[0]["FullName"].ToString());

            ShowAlert("Application for " + dt.Rows[0]["FullName"] + " (ID " + studentId + ") approved. Login access enabled.", "success");
        }

        protected void btnConfirmReject_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int studentId;
            if (!int.TryParse(hdnRejectStudentId.Value, out studentId))
            {
                ShowAlert("Could not identify the application to reject. Please try again.", "danger");
                return;
            }

            string remark = txtRejectRemark.Text.Trim();
            if (string.IsNullOrEmpty(remark))
            {
                ShowAlert("A rejection remark is required.", "danger");
                return;
            }

            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT FullName, Email FROM Students WHERE StudentID = @StudentID",
                new SqlParameter("@StudentID", studentId));
            if (dt.Rows.Count == 0)
            {
                ShowAlert("Student not found.", "danger");
                return;
            }

            DBHelper.ExecuteNonQuery(@"
                UPDATE Students
                SET ApprovalStatus = 'Rejected',
                    RejectionRemark = @RejectionRemark,
                    RejectedBy = @RejectedBy,
                    RejectedDate = GETDATE(),
                    ApprovedBy = NULL,
                    ApprovedDate = NULL,
                    LastModifiedDate = GETDATE()
                WHERE StudentID = @StudentID",
                new SqlParameter("@RejectionRemark", remark),
                new SqlParameter("@RejectedBy", CurrentAdminUsername),
                new SqlParameter("@StudentID", studentId));

            TrySendRejectionEmail(studentId, dt.Rows[0]["Email"].ToString(), dt.Rows[0]["FullName"].ToString(), remark);

            txtRejectRemark.Text = string.Empty;
            hdnRejectStudentId.Value = string.Empty;

            ShowAlert("Application for " + dt.Rows[0]["FullName"] + " (ID " + studentId + ") rejected and student notified.", "warning");

            LoadStats();
            BindGrid();
        }

        private void SetAccountStatus(int studentId, string newStatus)
        {
            DBHelper.ExecuteNonQuery(
                "UPDATE Students SET AccountStatus = @Status, LastModifiedDate = GETDATE() WHERE StudentID = @StudentID",
                new SqlParameter("@Status", newStatus),
                new SqlParameter("@StudentID", studentId));

            ShowAlert("Student ID " + studentId + " marked as " + newStatus + ".", "info");
        }

        private void ResetApplication(int studentId)
        {
            DBHelper.ExecuteNonQuery(@"
                UPDATE Students
                SET ApprovalStatus = 'Pending',
                    RejectionRemark = NULL,
                    RejectedBy = NULL,
                    RejectedDate = NULL,
                    ApprovedBy = NULL,
                    ApprovedDate = NULL,
                    LastModifiedDate = GETDATE()
                WHERE StudentID = @StudentID",
                new SqlParameter("@StudentID", studentId));

            ShowAlert("Application for Student ID " + studentId + " reset to Pending for re-review.", "info");
        }

        #endregion

        #region ---- Email (best-effort: SMTP failures never block an admin action) ----

        private void TrySendApprovalEmail(int studentId, string email, string fullName)
        {
            try
            {
                EmailHelper.SendApprovalEmail(email, fullName, studentId);
            }
            catch (Exception ex)
            {
                // Task 9, Requirement 8: the approval itself already succeeded in the database,
                // so a mail failure must not roll it back — but it must be logged so the Admin
                // team can follow up and re-notify the student manually if needed.
                AppLogger.Error("Approval", "Failed to send Approval email for StudentID=" + studentId + " to " + email, ex);
            }
        }

        private void TrySendRejectionEmail(int studentId, string email, string fullName, string remark)
        {
            try
            {
                EmailHelper.SendRejectionEmail(email, fullName, studentId, remark);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Rejection", "Failed to send Rejection email for StudentID=" + studentId + " to " + email, ex);
            }
        }

        #endregion

        #region ---- Misc ----

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

        #endregion
    }
}
