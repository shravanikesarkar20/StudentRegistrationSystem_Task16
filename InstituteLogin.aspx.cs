using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 16: Institute Login for the Centralised Institute Dashboard.
    /// Standard POST + server-controls login, following the same shape as Login.aspx
    /// (student) - CAPTCHA, RequiredFieldValidators, session-based auth.
    /// </summary>
    public partial class InstituteLogin : Page
    {
        private const string SESSION_INST_ID = "InstId";
        private const string SESSION_INST_NAME = "InstName";
        private const string SESSION_INST_STATUS = "InstStatus";
        private const string SESSION_CAPTCHA = "CaptchaCode";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Requirement (Security): never let the browser cache a page that could reflect
            // a logged-in state or stale form values.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            if (!IsPostBack)
            {
                HideMessages();

                // Already logged in? Skip straight to the dashboard.
                if (Session[SESSION_INST_ID] != null)
                {
                    Response.Redirect("InstituteDashboard.aspx");
                }
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            // ---- 1. CAPTCHA check (single-use, same mechanism as the student/admin logins) ----
            string enteredCaptcha = txtCaptchaInput.Text.Trim();
            string sessionCaptcha = Session[SESSION_CAPTCHA] as string;
            Session[SESSION_CAPTCHA] = null; // burn it whether this attempt succeeds or fails

            if (string.IsNullOrEmpty(sessionCaptcha) ||
                !string.Equals(enteredCaptcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Incorrect CAPTCHA code. Please try again.");
                return;
            }

            // ---- 2. Validate credentials against dbo.instreg (Requirement: validation from instreg table) ----
            string instId = txtInstId.Text.Trim();
            string password = txtPassword.Text;
            DataRow row;

            try
            {
                row = InstituteAuth.ValidateInstitute(instId, password);
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Trace.TraceError("InstituteLogin: database error during authentication - " + sqlEx.Message);
                ShowError("We couldn't reach the database right now. Please try again in a moment.");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("InstituteLogin: unexpected error during authentication - " + ex.Message);
                ShowError("Something went wrong while signing you in. Please try again.");
                return;
            }

            if (row == null)
            {
                ShowError("Invalid Institute Id or Password.");
                return;
            }

            // ---- 3. Institute status gate. A Suspended/Inactive institute can still be told why,
            //         rather than being shown the same generic "invalid credentials" message. ----
            string status = row["status"].ToString();
            if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Your institute's account status is currently \"" + status + "\". Please contact Twinkle IT Solutions for assistance.");
                return;
            }

            // ---- 4. Success: start the institute session (proper session management) ----
            Session[SESSION_INST_ID] = row["instid"].ToString();
            Session[SESSION_INST_NAME] = row["instname"].ToString();
            Session[SESSION_INST_STATUS] = status;

            Response.Redirect("InstituteDashboard.aspx");
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
