using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace StudentRegistrationSystem
{
    public partial class ChangePassword : Page
    {
        private const string SESSION_STUDENT_ID = "StudentID";

        private int CurrentStudentId
        {
            get { return Convert.ToInt32(Session[SESSION_STUDENT_ID]); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            // Prevent direct access without logging in — same gate as Dashboard.aspx.
            if (Session[SESSION_STUDENT_ID] == null)
            {
                Response.Redirect("Login.aspx");
            }
        }

        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT Mobile FROM Students WHERE StudentID = @StudentID",
                new SqlParameter("@StudentID", CurrentStudentId));

            if (dt.Rows.Count == 0)
            {
                Session.Clear();
                Session.Abandon();
                Response.Redirect("Login.aspx");
                return;
            }

            string storedMobile = dt.Rows[0]["Mobile"].ToString();
            string storedDigits = Regex.Replace(storedMobile, "[^0-9]", "");
            string storedLast10 = storedDigits.Length >= 10 ? storedDigits.Substring(storedDigits.Length - 10) : storedDigits;

            string currentEntered = Regex.Replace(txtCurrentPassword.Text.Trim(), "[^0-9]", "");
            if (currentEntered.Length != 10 || currentEntered != storedLast10)
            {
                ShowError("Current password is incorrect.");
                return;
            }

            string newPassword = txtNewPassword.Text.Trim();
            if (newPassword == storedLast10)
            {
                ShowError("New password must be different from your current password.");
                return;
            }

            // Preserve whatever country-code prefix the number already had; only replace the last 10 digits.
            string prefix = storedDigits.Length > 10 ? storedDigits.Substring(0, storedDigits.Length - 10) : "";
            string dialPrefix = storedMobile.TrimStart().StartsWith("+") ? "+" : "";
            string newMobile = dialPrefix + prefix + newPassword;

            DBHelper.ExecuteNonQuery(
                "UPDATE Students SET Mobile = @Mobile WHERE StudentID = @StudentID",
                new SqlParameter("@Mobile", newMobile),
                new SqlParameter("@StudentID", CurrentStudentId));

            txtCurrentPassword.Text = "";
            txtNewPassword.Text = "";
            txtConfirmPassword.Text = "";

            ShowSuccess("Your password (mobile number) was updated successfully. Use the new number to log in next time.");
        }

        private void ShowSuccess(string message)
        {
            pnlSuccessMsg.Controls.Clear();
            pnlSuccessMsg.Controls.Add(new LiteralControl(message));
            pnlSuccessMsg.CssClass = "alert alert-success";
        }

        private void ShowError(string message)
        {
            pnlErrorMsg.Controls.Clear();
            pnlErrorMsg.Controls.Add(new LiteralControl(message));
            pnlErrorMsg.CssClass = "alert alert-danger";
        }

        private void HideMessages()
        {
            pnlSuccessMsg.CssClass = "alert alert-success d-none";
            pnlErrorMsg.CssClass = "alert alert-danger d-none";
        }
    }
}
