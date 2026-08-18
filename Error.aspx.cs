using System;
using System.Web.UI;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 8, Requirement 5: friendly landing page for customErrors' defaultRedirect in
    /// Web.config, so an unhandled exception anywhere in the app never shows a raw stack trace
    /// to a real user.
    /// </summary>
    public partial class ErrorPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
