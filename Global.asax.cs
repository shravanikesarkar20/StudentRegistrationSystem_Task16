using System;
using System.Web;

namespace StudentRegistrationSystem
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Runs once when the application first starts.
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Runs when a new user session begins (fresh OTP/session state per visitor).
        }

        /// <summary>
        /// Task 8, Requirement 5: last-resort safety net. Every login/dashboard/change-password
        /// path already has its own try/catch around database calls, but this backstop makes sure
        /// that ANY unhandled exception anywhere in the app is logged and turned into the friendly
        /// Error.aspx page instead of crashing with a raw ASP.NET error/stack trace.
        /// </summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex != null)
            {
                System.Diagnostics.Trace.TraceError("Unhandled exception: " + ex);
                AppLogger.Error("Unhandled", "Unhandled exception at " + Request.Path, ex);
            }
            // customErrors in Web.config takes over the redirect to Error.aspx for remote
            // requests; we don't clear the error here so local/debug requests still see it.
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}
