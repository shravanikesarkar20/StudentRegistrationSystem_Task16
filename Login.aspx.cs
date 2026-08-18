using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    public partial class Login : Page
    {
        private const string SESSION_STUDENT_ID = "StudentID";
        private const string SESSION_STUDENT_NAME = "StudentName";
        private const string SESSION_CAPTCHA = "CaptchaCode";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Task 8, Requirement 7: never let the browser cache a page that could reflect
            // a logged-in state or stale form values.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            if (!IsPostBack)
            {
                HideMessages();

                // Already logged in? Skip straight to the dashboard.
                if (Session[SESSION_STUDENT_ID] != null)
                {
                    Response.Redirect("Dashboard.aspx");
                }
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            // ---- 1. CAPTCHA check ----
            string enteredCaptcha = txtCaptchaInput.Text.Trim();
            string sessionCaptcha = Session[SESSION_CAPTCHA] as string;

            if (string.IsNullOrEmpty(sessionCaptcha) ||
                !string.Equals(enteredCaptcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Incorrect CAPTCHA code. Please try again.");
                return;
            }

            // CAPTCHA is single-use regardless of outcome.
            Session[SESSION_CAPTCHA] = null;

            // ---- 2. Look up the student by email (Requirement 5/7: never let a DB hiccup crash the page) ----
            string email = txtEmail.Text.Trim();
            DataTable dt;

            try
            {
                dt = DBHelper.ExecuteQuery(
                    "SELECT StudentID, FullName, Mobile, LastLoginDate, ApprovalStatus, AccountStatus, RejectionRemark FROM Students WHERE Email = @Email",
                    new SqlParameter("@Email", email));
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Trace.TraceError("Login: database error during authentication - " + sqlEx.Message);
                ShowError("We couldn't reach the database right now. Please try again in a moment.");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Login: unexpected error during authentication - " + ex.Message);
                ShowError("Something went wrong while signing you in. Please try again.");
                return;
            }

            if (dt.Rows.Count == 0)
            {
                ShowError("Invalid email address or password.");
                return;
            }

            // ---- 3. Validate password against the registered mobile number ----
            DataRow row = dt.Rows[0];
            string enteredDigits = Regex.Replace(txtPassword.Text.Trim(), "[^0-9]", "");
            string storedDigits = Regex.Replace(row["Mobile"].ToString(), "[^0-9]", "");
            string storedLast10 = storedDigits.Length >= 10
                ? storedDigits.Substring(storedDigits.Length - 10)
                : storedDigits;

            if (enteredDigits.Length != 10 || enteredDigits != storedLast10)
            {
                ShowError("Invalid email address or password.");
                return;
            }

            // ---- 3b. Task 7: Approval workflow + account status gate ----
            string approvalStatus = row["ApprovalStatus"].ToString();
            string accountStatus = row["AccountStatus"].ToString();

            if (string.Equals(approvalStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Your registration is still pending approval by the Admin. You will be notified by email once it is reviewed.");
                return;
            }

            if (string.Equals(approvalStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                string remark = row["RejectionRemark"] == DBNull.Value ? "" : row["RejectionRemark"].ToString();
                ShowError("Your registration application was rejected by the Admin" +
                    (string.IsNullOrEmpty(remark) ? "." : (": " + remark)));
                return;
            }

            if (string.Equals(accountStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Your account has been deactivated by the Admin. Please contact the administration office for assistance.");
                return;
            }

            // ---- 4. Success: start the session, then record this login ----
            int studentId = Convert.ToInt32(row["StudentID"]);

            Session[SESSION_STUDENT_ID] = studentId;
            Session[SESSION_STUDENT_NAME] = row["FullName"].ToString();
            // Show the PREVIOUS login time on the dashboard, captured before we overwrite it below.
            Session["PrevLastLogin"] = row["LastLoginDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["LastLoginDate"]);

            try
            {
                DBHelper.ExecuteNonQuery(
                    "UPDATE Students SET LastLoginDate = GETDATE() WHERE StudentID = @StudentID",
                    new SqlParameter("@StudentID", studentId));
            }
            catch (Exception ex)
            {
                // Non-fatal: the login itself already succeeded, so we still let the student in.
                System.Diagnostics.Trace.TraceError("Login: failed to update LastLoginDate - " + ex.Message);
            }

            // ---- 5. Role-based navigation (Requirement 4) ----
            Response.Redirect("Dashboard.aspx");
        }

        private void ShowError(string message)
        {
            pnlErrorMsg.Controls.Clear();
            pnlErrorMsg.Controls.Add(new LiteralControl(message));
            pnlErrorMsg.CssClass = "alert alert-danger";
        }

        private void HideMessages()
        {
            pnlErrorMsg.CssClass = "alert alert-danger d-none";
        }
    }
}
