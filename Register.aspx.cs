using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    public partial class Register : Page
    {
        private const string SESSION_OTP = "Reg_OTP";
        private const string SESSION_OTP_TIME = "Reg_OTP_Time";
        private const string SESSION_OTP_EMAIL = "Reg_OTP_Email";
        private const string SESSION_EMAIL_VERIFIED = "Reg_EmailVerified";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadStudentIdPreview();
                LoadCountries();
                LoadStates(-1);
                LoadDistricts(-1);
                LoadAcademicDropdowns();
                ResetOtpState();
                LoadAdvertisementModal();
            }
        }

        #region ---- Task 11: Advertisement Modal ----

        /// <summary>
        /// Requirement 1/3 (revised): show the advertisement banner inline at the top of the
        /// Student Registration form — but only when the global switch (Admin Panel >
        /// Advertisements) is on AND at least one advertisement is currently active. If no
        /// advertisement is active, or the feature is disabled, the banner is never rendered.
        /// Replaces the earlier popup-modal presentation.
        /// </summary>
        private void LoadAdvertisementModal()
        {
            try
            {
                if (!AdvertisementHelper.IsModalGloballyEnabled())
                {
                    return;
                }

                DataTable ads = AdvertisementHelper.GetActiveAdvertisementsForDisplay();
                if (ads.Rows.Count == 0)
                {
                    return;
                }

                rptAds.DataSource = ads;
                rptAds.DataBind();

                rptAdIndicators.DataSource = ads;
                rptAdIndicators.DataBind();

                bool hasMultiple = ads.Rows.Count > 1;
                pnlAdIndicators.Visible = hasMultiple;
                pnlAdControls.Visible = hasMultiple;

                pnlAdBanner.Visible = true;
            }
            catch (Exception ex)
            {
                // Never let an advertisement-loading failure break the registration page itself.
                AppLogger.Error("Register.LoadAdvertisementModal", "Failed to load advertisements.", ex);
                pnlAdBanner.Visible = false;
            }
        }

        protected void rptAds_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            DataRowView row = (DataRowView)e.Item.DataItem;
            string imagePath = row["ImagePath"] == DBNull.Value ? null : row["ImagePath"].ToString();

            System.Web.UI.WebControls.Image imgAd = (System.Web.UI.WebControls.Image)e.Item.FindControl("imgAd");
            if (imgAd == null) return;

            if (!string.IsNullOrEmpty(imagePath))
            {
                imgAd.ImageUrl = ResolveUrl("~/" + imagePath.TrimStart('~', '/'));
                imgAd.Visible = true;
            }
            else
            {
                imgAd.Visible = false;
            }
        }

        #endregion

        #region ---- Task 6: Duplicate Email Prevention ----

        /// <summary>
        /// Called from client-side JS (fetch to "Register.aspx/CheckEmailExists") to warn the
        /// student before they even request an OTP. The authoritative check still happens
        /// server-side in btnSendOTP_Click / btnRegister_Click below.
        /// </summary>
        [WebMethod]
        public static bool CheckEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            object result = DBHelper.ExecuteScalar(
                "SELECT COUNT(1) FROM Students WHERE Email = @Email",
                new SqlParameter("@Email", email.Trim()));

            return Convert.ToInt32(result) > 0;
        }

        private static bool EmailAlreadyRegistered(string email)
        {
            object result = DBHelper.ExecuteScalar(
                "SELECT COUNT(1) FROM Students WHERE Email = @Email",
                new SqlParameter("@Email", email.Trim()));
            return Convert.ToInt32(result) > 0;
        }

        #endregion

        #region ---- Student ID Preview ----

        private void LoadStudentIdPreview()
        {
            try
            {
                object result = DBHelper.ExecuteScalar(
                    "SELECT IDENT_CURRENT('Students') + IDENT_INCR('Students')");
                txtStudentIdPreview.Text = (result != null && result != DBNull.Value)
                    ? Convert.ToInt32(result).ToString()
                    : "1001";
            }
            catch
            {
                txtStudentIdPreview.Text = "Auto Generated";
            }
        }

        #endregion

        #region ---- Cascading Dropdowns (Country -> State -> District) ----

        private void LoadCountries()
        {
            DataTable dt = DBHelper.ExecuteQuery("SELECT CountryID, CountryName FROM Countries ORDER BY CountryName");
            ddlCountry.DataSource = dt;
            ddlCountry.DataTextField = "CountryName";
            ddlCountry.DataValueField = "CountryID";
            ddlCountry.DataBind();
            ddlCountry.Items.Insert(0, new ListItem("Select Country", ""));
        }

        private void LoadStates(int countryId)
        {
            ddlState.Items.Clear();
            ddlState.Items.Add(new ListItem("Select State", ""));

            if (countryId > 0)
            {
                DataTable dt = DBHelper.ExecuteQuery(
                    "SELECT StateID, StateName FROM States WHERE CountryID = @CountryID ORDER BY StateName",
                    new SqlParameter("@CountryID", countryId));

                foreach (DataRow row in dt.Rows)
                {
                    ddlState.Items.Add(new ListItem(row["StateName"].ToString(), row["StateID"].ToString()));
                }
            }
        }

        private void LoadDistricts(int stateId)
        {
            ddlDistrict.Items.Clear();
            ddlDistrict.Items.Add(new ListItem("Select District", ""));

            if (stateId > 0)
            {
                DataTable dt = DBHelper.ExecuteQuery(
                    "SELECT DistrictID, DistrictName FROM Districts WHERE StateID = @StateID ORDER BY DistrictName",
                    new SqlParameter("@StateID", stateId));

                foreach (DataRow row in dt.Rows)
                {
                    ddlDistrict.Items.Add(new ListItem(row["DistrictName"].ToString(), row["DistrictID"].ToString()));
                }
            }
        }

        protected void ddlCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            int countryId;
            int.TryParse(ddlCountry.SelectedValue, out countryId);
            LoadStates(countryId);
            LoadDistricts(-1);
        }

        protected void ddlState_SelectedIndexChanged(object sender, EventArgs e)
        {
            int stateId;
            int.TryParse(ddlState.SelectedValue, out stateId);
            LoadDistricts(stateId);
        }

        #endregion

        #region ---- Task 12: Course / Academic Details (drives fee calculation) ----

        private void LoadAcademicDropdowns()
        {
            ddlInstitute.DataSource = RegistrationFeeHelper.GetInstitutes();
            ddlInstitute.DataTextField = "InstituteName";
            ddlInstitute.DataValueField = "InstituteID";
            ddlInstitute.DataBind();
            ddlInstitute.Items.Insert(0, new ListItem("Select Institute", ""));
            ddlInstitute.SelectedIndex = 0;

            LoadCourses(-1);

            ddlYearSemester.Items.Clear();
            ddlYearSemester.Items.Add(new ListItem("Select Year / Semester", ""));
            foreach (string option in RegistrationFeeHelper.YearSemesterOptions)
            {
                ddlYearSemester.Items.Add(new ListItem(option, option));
            }

            ddlAcademicYear.DataSource = RegistrationFeeHelper.GetAcademicYears();
            ddlAcademicYear.DataTextField = "YearLabel";
            ddlAcademicYear.DataValueField = "AcademicYearID";
            ddlAcademicYear.DataBind();
            ddlAcademicYear.Items.Insert(0, new ListItem("Select Academic Year", ""));
            ddlAcademicYear.SelectedIndex = 0;

            ddlStudentCategory.DataSource = RegistrationFeeHelper.GetStudentCategories();
            ddlStudentCategory.DataTextField = "CategoryName";
            ddlStudentCategory.DataValueField = "StudentCategoryID";
            ddlStudentCategory.DataBind();
            ddlStudentCategory.Items.Insert(0, new ListItem("Select Category", ""));
            ddlStudentCategory.SelectedIndex = 0;
        }

        private void LoadCourses(int instituteId)
        {
            ddlCourse.Items.Clear();
            ddlCourse.Items.Add(new ListItem("Select Course", ""));

            if (instituteId > 0)
            {
                DataTable dt = RegistrationFeeHelper.GetCourses(instituteId);
                foreach (DataRow row in dt.Rows)
                {
                    ddlCourse.Items.Add(new ListItem(row["CourseName"].ToString(), row["CourseID"].ToString()));
                }
            }
        }

        protected void ddlInstitute_SelectedIndexChanged(object sender, EventArgs e)
        {
            int instituteId;
            int.TryParse(ddlInstitute.SelectedValue, out instituteId);
            LoadCourses(instituteId);
        }

        #endregion

        #region ---- OTP Verification Flow ----

        private void ResetOtpState()
        {
            Session[SESSION_OTP] = null;
            Session[SESSION_OTP_TIME] = null;
            Session[SESSION_OTP_EMAIL] = null;
            Session[SESSION_EMAIL_VERIFIED] = false;
        }

        protected void btnSendOTP_Click(object sender, EventArgs e)
        {
            HideMessages();
            string email = txtEmail.Text.Trim();
            string fullName = txtFullName.Text.Trim();

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                ShowError("Please enter a valid email address before requesting an OTP.");
                return;
            }
            if (string.IsNullOrEmpty(fullName))
            {
                ShowError("Please enter the student's full name before requesting an OTP.");
                return;
            }

            if (EmailAlreadyRegistered(email))
            {
                ShowError("A student is already registered with this Email Address.");
                return;
            }

            try
            {
                string otp = EmailHelper.GenerateOTP();

                Session[SESSION_OTP] = otp;
                Session[SESSION_OTP_TIME] = DateTime.Now;
                Session[SESSION_OTP_EMAIL] = email;
                Session[SESSION_EMAIL_VERIFIED] = false;

                EmailHelper.SendStudentOTP(email, fullName, otp);

                txtOTP.Enabled = true;
                btnVerifyOTP.Enabled = true;
                btnResendOTP.Enabled = true;
                lblVerifiedStatus.Text = "Email Not Verified";
                lblVerifiedStatus.CssClass = "badge bg-secondary";

                ShowSuccess("A 6-digit OTP has been sent to " + email + ". It is valid for " +
                            ConfigurationManager.AppSettings["OTPExpiryMinutes"] + " minutes.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Registration", "Failed to send OTP email to " + email, ex);
                ShowError("Failed to send OTP email. Please verify SMTP settings. Details: " + ex.Message);
            }
        }

        protected void btnResendOTP_Click(object sender, EventArgs e)
        {
            btnSendOTP_Click(sender, e);
        }

        protected void btnVerifyOTP_Click(object sender, EventArgs e)
        {
            HideMessages();

            string sessionOtp = Session[SESSION_OTP] as string;
            string sessionEmail = Session[SESSION_OTP_EMAIL] as string;
            object sessionTimeObj = Session[SESSION_OTP_TIME];

            if (string.IsNullOrEmpty(sessionOtp) || sessionTimeObj == null)
            {
                ShowError("No OTP request found. Please click 'Send OTP' first.");
                return;
            }

            DateTime generatedAt = (DateTime)sessionTimeObj;
            int expiryMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["OTPExpiryMinutes"]);

            if (DateTime.Now.Subtract(generatedAt).TotalMinutes > expiryMinutes)
            {
                ShowError("This OTP has expired. Please request a new one.");
                Session[SESSION_EMAIL_VERIFIED] = false;
                return;
            }

            if (!string.Equals(sessionEmail, txtEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Email address has changed since the OTP was sent. Please resend the OTP.");
                return;
            }

            if (txtOTP.Text.Trim() == sessionOtp)
            {
                Session[SESSION_EMAIL_VERIFIED] = true;
                lblVerifiedStatus.Text = "Email Verified";
                lblVerifiedStatus.CssClass = "badge bg-success";
                txtOTP.Enabled = false;
                btnVerifyOTP.Enabled = false;
                btnResendOTP.Enabled = false;
                txtEmail.Enabled = false;
                btnSendOTP.Enabled = false;
                ShowSuccess("Email verified successfully! You may now complete the registration.");
            }
            else
            {
                Session[SESSION_EMAIL_VERIFIED] = false;
                ShowError("Incorrect OTP. Please try again or click Resend.");
            }
        }

        private bool IsEmailVerified()
        {
            return Session[SESSION_EMAIL_VERIFIED] != null && (bool)Session[SESSION_EMAIL_VERIFIED];
        }

        #endregion

        #region ---- Save Registration ----

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            if (!IsEmailVerified())
            {
                ShowError("Please verify the student's email address using OTP before registering.");
                return;
            }

            if (ddlCountry.SelectedValue == "" || ddlState.SelectedValue == "" || ddlDistrict.SelectedValue == "")
            {
                ShowError("Please select Country, State and District.");
                return;
            }

            if (ddlInstitute.SelectedValue == "" || ddlCourse.SelectedValue == "" || ddlYearSemester.SelectedValue == "" ||
                ddlAcademicYear.SelectedValue == "" || ddlStudentCategory.SelectedValue == "")
            {
                ShowError("Please select Institute, Course, Year/Semester, Academic Year and Student Category.");
                return;
            }

            // Final server-side duplicate check — the authoritative gate before insert.
            if (EmailAlreadyRegistered(txtEmail.Text.Trim()))
            {
                ShowError("A student is already registered with this Email Address.");
                return;
            }

            string photoPath = null;
            if (fuPhoto.HasFile)
            {
                string validationError;
                photoPath = SaveUploadedPhoto(fuPhoto, out validationError);
                if (photoPath == null)
                {
                    lblPhotoError.Text = validationError;
                    return;
                }
            }

            try
            {
                string insertSql = @"INSERT INTO Students
                    (FullName, Email, Mobile, CountryID, StateID, DistrictID, Address, Gender, DOB, PhotoPath, IsEmailVerified, RegistrationDate)
                    VALUES
                    (@FullName, @Email, @Mobile, @CountryID, @StateID, @DistrictID, @Address, @Gender, @DOB, @PhotoPath, 1, GETDATE())";

                string mobile = string.IsNullOrEmpty(hdnFullMobile.Value) ? txtMobile.Text.Trim() : hdnFullMobile.Value;

                int newId = DBHelper.ExecuteInsertReturnId(insertSql,
                    new SqlParameter("@FullName", txtFullName.Text.Trim()),
                    new SqlParameter("@Email", txtEmail.Text.Trim()),
                    new SqlParameter("@Mobile", mobile),
                    new SqlParameter("@CountryID", int.Parse(ddlCountry.SelectedValue)),
                    new SqlParameter("@StateID", int.Parse(ddlState.SelectedValue)),
                    new SqlParameter("@DistrictID", int.Parse(ddlDistrict.SelectedValue)),
                    new SqlParameter("@Address", (object)txtAddress.Text.Trim() ?? DBNull.Value),
                    new SqlParameter("@Gender", ddlGender.SelectedValue),
                    new SqlParameter("@DOB", Convert.ToDateTime(txtDOB.Text)),
                    new SqlParameter("@PhotoPath", (object)photoPath ?? DBNull.Value));

                var studentDetails = BuildStudentDetails(newId, mobile);

                // Task 9, Requirement 2 & 3: notify both the student and the Admin. Each send
                // is independently best-effort (a failed email must never roll back a
                // successful registration) but every attempt is validated and logged.
                TrySendRegistrationConfirmation(studentDetails);
                TrySendAdminNotification(studentDetails);

                // Task 12: save the academic profile just collected, then immediately try to
                // generate the student's fee demand so it's already waiting on their dashboard
                // the first time they log in. Best-effort — a fee-side hiccup (or no fee
                // structures configured yet for this combination) must never roll back an
                // otherwise-successful registration.
                int feeHeadsGenerated = TrySaveAcademicProfileAndGenerateFees(newId);

                string successMessage = "Student registered successfully! Student ID: " + newId +
                    ". A confirmation email has been sent to " + txtEmail.Text.Trim() + ".";
                successMessage += feeHeadsGenerated > 0
                    ? " Your registration fee details are ready — log in to your dashboard to view them."
                    : " Your registration fee details will appear on your dashboard once they're configured by the Admin.";

                ShowSuccess(successMessage);
                ClearForm();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Unique index violation — another registration used this email a moment ago.
                ShowError("A student is already registered with this Email Address.");
            }
            catch (Exception ex)
            {
                ShowError("Registration failed: " + ex.Message);
            }
        }

        /// <summary>Task 9: single source of truth for the placeholder data shared by the
        /// Registration Confirmation and Admin Notification templates.</summary>
        private Dictionary<string, string> BuildStudentDetails(int studentId, string mobile)
        {
            return new Dictionary<string, string>
            {
                { "StudentID", studentId.ToString() },
                { "StudentName", txtFullName.Text.Trim() },
                { "Email", txtEmail.Text.Trim() },
                { "Mobile", mobile },
                { "Gender", ddlGender.SelectedValue },
                { "DOB", txtDOB.Text },
                { "Country", ddlCountry.SelectedItem.Text },
                { "State", ddlState.SelectedItem.Text },
                { "District", ddlDistrict.SelectedItem.Text },
                { "Address", txtAddress.Text.Trim() },
                { "RegistrationDate", DateTime.Now.ToString("dd MMM yyyy, hh:mm tt") }
            };
        }

        /// <summary>Task 9, Requirement 2: sends the student their Registration Confirmation
        /// email. Best-effort — logged on failure, never blocks a successful registration.</summary>
        private void TrySendRegistrationConfirmation(Dictionary<string, string> studentDetails)
        {
            try
            {
                EmailHelper.SendRegistrationConfirmation(studentDetails);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Registration", "Failed to send Registration Confirmation email for StudentID=" +
                    studentDetails["StudentID"], ex);
            }
        }

        /// <summary>Task 9, Requirement 3: sends the Admin their New Registration notification
        /// email. Best-effort — logged on failure, never blocks a successful registration.</summary>
        private void TrySendAdminNotification(Dictionary<string, string> studentDetails)
        {
            try
            {
                EmailHelper.SendAdminNotification(studentDetails);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Registration", "Failed to send Admin Notification email for StudentID=" +
                    studentDetails["StudentID"], ex);
            }
        }

        /// <summary>Task 12: persists the Institute/Course/Year-Semester/Academic Year/Category
        /// picked in the "Course &amp; Academic Details" section as the student's
        /// StudentAcademicProfile, then generates a fee demand for every active fee structure
        /// that already matches it. Returns the number of new fee heads generated (0 if none
        /// are configured yet for this combination, or if this step fails for any reason).</summary>
        private int TrySaveAcademicProfileAndGenerateFees(int studentId)
        {
            try
            {
                RegistrationFeeHelper.SaveStudentAcademicProfile(
                    studentId,
                    int.Parse(ddlAcademicYear.SelectedValue),
                    int.Parse(ddlInstitute.SelectedValue),
                    int.Parse(ddlCourse.SelectedValue),
                    ddlYearSemester.SelectedValue,
                    int.Parse(ddlStudentCategory.SelectedValue),
                    "Self-Registration");

                return RegistrationFeeHelper.GenerateFeeDemandsForStudent(studentId, "Self-Registration");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Registration", "Failed to save academic profile / generate fee demand for StudentID=" + studentId, ex);
                return 0;
            }
        }

        private string SaveUploadedPhoto(FileUpload upload, out string errorMessage)
        {
            errorMessage = null;
            string ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
            string[] allowed = { ".jpg", ".jpeg", ".png" };

            if (Array.IndexOf(allowed, ext) < 0)
            {
                errorMessage = "Only .jpg, .jpeg and .png files are allowed.";
                return null;
            }

            double maxMb = Convert.ToDouble(ConfigurationManager.AppSettings["MaxPhotoSizeMB"]);
            if (upload.PostedFile.ContentLength > maxMb * 1024 * 1024)
            {
                errorMessage = "Photo size must not exceed " + maxMb + " MB.";
                return null;
            }

            string uploadFolder = Server.MapPath(ConfigurationManager.AppSettings["StudentPhotoUploadPath"]);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString("N") + ext;
            string fullPath = Path.Combine(uploadFolder, uniqueFileName);
            upload.SaveAs(fullPath);

            return "Uploads/Students/" + uniqueFileName;
        }

        #endregion

        #region ---- Helpers ----

        private bool IsValidEmail(string email)
        {
            try { return new System.Net.Mail.MailAddress(email).Address == email; }
            catch { return false; }
        }

        private void ShowSuccess(string message)
        {
            pnlSuccessMsg.Controls.Clear();
            pnlSuccessMsg.Controls.Add(new LiteralControl(message));
            pnlSuccessMsg.CssClass = "alert alert-success";
            pnlErrorMsg.CssClass = "alert alert-danger d-none";
        }

        private void ShowError(string message)
        {
            pnlErrorMsg.Controls.Clear();
            pnlErrorMsg.Controls.Add(new LiteralControl(message));
            pnlErrorMsg.CssClass = "alert alert-danger";
            pnlSuccessMsg.CssClass = "alert alert-success d-none";
        }

        private void HideMessages()
        {
            pnlSuccessMsg.CssClass = "alert alert-success d-none";
            pnlErrorMsg.CssClass = "alert alert-danger d-none";
        }

        private void ClearForm()
        {
            txtFullName.Text = "";
            txtEmail.Text = "";
            txtEmail.Enabled = true;
            txtMobile.Text = "";
            hdnFullMobile.Value = "";
            txtAddress.Text = "";
            txtDOB.Text = "";
            ddlGender.SelectedIndex = 0;
            ddlCountry.SelectedIndex = 0;
            LoadStates(-1);
            LoadDistricts(-1);
            ddlInstitute.SelectedIndex = 0;
            LoadCourses(-1);
            ddlYearSemester.SelectedIndex = 0;
            ddlAcademicYear.SelectedIndex = 0;
            ddlStudentCategory.SelectedIndex = 0;
            txtOTP.Text = "";
            txtOTP.Enabled = false;
            btnVerifyOTP.Enabled = false;
            btnResendOTP.Enabled = false;
            btnSendOTP.Enabled = true;
            lblPhotoError.Text = "";
            lblVerifiedStatus.Text = "Email Not Verified";
            lblVerifiedStatus.CssClass = "badge bg-secondary";
            ResetOtpState();
            LoadStudentIdPreview();
        }

        #endregion
    }
}
