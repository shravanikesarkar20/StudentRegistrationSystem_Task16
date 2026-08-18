using System;
using System.Data.SqlClient;
using System.Web.UI;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Public, no-login institute self-registration form. Submissions land as Pending and
    /// only become selectable in Register.aspx's Institute dropdown once an admin approves
    /// them via InstituteManagement.aspx.
    /// </summary>
    public partial class InstituteRegister : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                HideMessages();
            }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            string instituteName = txtInstituteName.Text.Trim();

            if (InstituteRegistrationHelper.IsInstituteNameTaken(instituteName))
            {
                ShowError("An institute with this name has already been registered.");
                return;
            }

            int capacity;
            if (!int.TryParse(txtCapacity.Text.Trim(), out capacity) || capacity <= 0)
            {
                ShowError("Please enter a valid student capacity.");
                return;
            }

            try
            {
                InstituteRegistrationHelper.RegisterInstitute(
                    instituteName,
                    capacity,
                    txtAddress.Text.Trim(),
                    txtCity.Text.Trim(),
                    txtContactEmail.Text.Trim(),
                    txtContactPhone.Text.Trim(),
                    txtWebsite.Text.Trim(),
                    txtCourses.Text.Trim());
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Trace.TraceError("InstituteRegister: database error - " + sqlEx.Message);
                ShowError("We couldn't reach the database right now. Please try again in a moment.");
                return;
            }

            pnlForm.Visible = false;
            pnlSuccess.Visible = true;
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
