using System;
using System.Data.SqlClient;
using System.Web;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 8: Admin Login, re-implemented to authenticate over HTTP GET as required by the
    /// assignment brief. There is no server-side &lt;form runat="server"&gt; on this page at all —
    /// the login "form" is a plain HTML &lt;form method="get"&gt;, so the browser issues a normal
    /// GET request with username/password/captcha in the query string, and everything is
    /// validated here in Page_Load (no ASP.NET postback machinery involved).
    ///
    /// SECURITY NOTE (documented for the assignment write-up):
    /// Submitting credentials via GET is inherently weaker than POST — the values end up in the
    /// browser's address bar, its history, and potentially in server / proxy access logs, none of
    /// which happens with POST. We only implement it this way because Task 8 explicitly calls for
    /// GET-based admin authentication. To limit the exposure as much as possible while still
    /// meeting that requirement, this page:
    ///   1) Never caches the response (Cache-Control: no-store), so the credential-bearing URL is
    ///      not kept in the browser's disk/back-forward cache.
    ///   2) Immediately issues a Response.Redirect to AdminDashboard.aspx on success, so the
    ///      address bar no longer shows the query string after login.
    ///   3) Never logs the raw query string; only a generic failure reason is traced.
    /// For a production deployment we would recommend switching this back to POST + HTTPS.
    /// </summary>
    public partial class AdminLogin : System.Web.UI.Page
    {
        private const string SESSION_ADMIN_NAME = "AdminName";
        private const string SESSION_ADMIN_USERNAME = "AdminUsername";
        private const string SESSION_CAPTCHA = "CaptchaCode";

        /// <summary>Bound into the markup via &lt;%= %&gt; to show a login error, if any.</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>Re-populated into the username box after a failed attempt (never the password).</summary>
        public string PostedUsername { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Requirement 7 (Security): the credential-bearing GET URL must never be cached.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);

            // Already logged in? Skip straight to the dashboard.
            if (Session[SESSION_ADMIN_NAME] != null)
            {
                Response.Redirect("AdminDashboard.aspx");
                return;
            }

            // Requirement 1: authenticate using the HTTP GET method.
            // "login=1" distinguishes an actual submitted attempt from the initial page load.
            if (Request.HttpMethod == "GET" && Request.QueryString["login"] == "1")
            {
                ProcessLogin();
            }
        }

        private void ProcessLogin()
        {
            string username = (Request.QueryString["username"] ?? string.Empty).Trim();
            string password = Request.QueryString["password"] ?? string.Empty;
            string captchaInput = (Request.QueryString["captcha"] ?? string.Empty).Trim();

            // Re-show whatever username was typed; deliberately never echo the password back.
            PostedUsername = username;

            // ---- 1. Server-side presence validation (Requirement 5) ----
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Please enter both username and password.";
                return;
            }

            if (string.IsNullOrEmpty(captchaInput))
            {
                ErrorMessage = "Please enter the CAPTCHA code.";
                return;
            }

            // ---- 2. CAPTCHA check (single-use, same mechanism as the student login) ----
            string sessionCaptcha = Session[SESSION_CAPTCHA] as string;
            Session[SESSION_CAPTCHA] = null; // burn it whether this attempt succeeds or fails

            if (string.IsNullOrEmpty(sessionCaptcha) ||
                !string.Equals(captchaInput, sessionCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Incorrect CAPTCHA code. Please try again.";
                return;
            }

            // ---- 3. Validate admin credentials (Requirement 5 + 7: graceful DB-failure handling) ----
            string fullName;
            try
            {
                fullName = AdminAuth.ValidateAdmin(username, password);
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Trace.TraceError("AdminLogin: database error during authentication - " + sqlEx.Message);
                ErrorMessage = "We couldn't reach the database right now. Please try again in a moment.";
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("AdminLogin: unexpected error during authentication - " + ex.Message);
                ErrorMessage = "Something went wrong while signing you in. Please try again.";
                return;
            }

            if (fullName == null)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            // ---- 4. Success: start the admin session (Requirement 3) ----
            Session[SESSION_ADMIN_NAME] = fullName;
            Session[SESSION_ADMIN_USERNAME] = username;

            // ---- 5. Role-based navigation (Requirement 4) ----
            Response.Redirect("AdminDashboard.aspx");
        }
    }
}
