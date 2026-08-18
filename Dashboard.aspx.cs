using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    public partial class Dashboard : Page
    {
        private const string SESSION_STUDENT_ID = "StudentID";
        private const string SESSION_STUDENT_NAME = "StudentName";
        private const string PLACEHOLDER_PHOTO = "https://via.placeholder.com/120x120?text=Photo";

        private int CurrentStudentId
        {
            get { return Convert.ToInt32(Session[SESSION_STUDENT_ID]); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Task 8, Requirement 3/7: protected page — never cached, so a browser "Back" after
            // logout can't resurrect it, and every request re-checks the session.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            // Prevent direct access to the dashboard without logging in.
            if (Session[SESSION_STUDENT_ID] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadCountries();
                LoadProfile();
                LoadMyFees();
            }
        }

        #region ---- Task 12: My Fees ----

        /// <summary>Shows the same six figures (Total Payable, Amount Paid, Outstanding,
        /// Late Fee, Scholarship/Discount, Net Payable) and Payment Status the Admin sees on
        /// Student Dues, plus a per-fee-head breakdown and payment history — read-only, this
        /// student's own data only.</summary>
        private void LoadMyFees()
        {
            try
            {
                RegistrationFeeHelper.FeeSummary summary = RegistrationFeeHelper.GetStudentFeeSummary(CurrentStudentId);

                if (summary.PaymentStatus == "No Dues Generated")
                {
                    pnlFeeSummary.Visible = false;
                    pnlNoFees.Visible = true;
                    litNoFeesMessage.Text = "No fee details are available yet. Once the Admin configures and generates " +
                        "your registration fee, it will appear here.";
                    return;
                }

                pnlNoFees.Visible = false;
                pnlFeeSummary.Visible = true;

                litTotalPayable.Text = summary.TotalPayable.ToString("N2");
                litAmountPaid.Text = summary.AmountPaid.ToString("N2");
                litOutstanding.Text = summary.OutstandingAmount.ToString("N2");
                litLateFee.Text = (summary.LateFeeOutstanding + summary.LateFeeCharged).ToString("N2");
                litDiscount.Text = summary.DiscountAmount.ToString("N2");
                litNetPayable.Text = summary.NetPayable.ToString("N2");

                string statusCssKey = summary.PaymentStatus.Replace(" ", "");
                litPaymentStatus.Text = string.Format("<span class='badge-status status-{0}'>{1}</span>",
                    statusCssKey, Server.HtmlEncode(summary.PaymentStatus));

                gvMyFees.DataSource = RegistrationFeeHelper.GetStudentFeeDemands(CurrentStudentId);
                gvMyFees.DataBind();

                gvMyPayments.DataSource = RegistrationFeeHelper.GetTransactionsForStudent(CurrentStudentId);
                gvMyPayments.DataBind();
            }
            catch (Exception ex)
            {
                // Non-fatal: the rest of the dashboard (profile) must still work even if the
                // fee module has an issue (e.g. Task 12 schema not yet migrated on this DB).
                System.Diagnostics.Trace.TraceError("Dashboard.LoadMyFees: " + ex.Message);
                pnlFeeSummary.Visible = false;
                pnlNoFees.Visible = true;
                litNoFeesMessage.Text = "We couldn't load your fee details right now. Please try again in a moment.";
            }
        }

        #endregion

        #region ---- Load / Display Profile ----

        private void LoadProfile()
        {
            DataTable dt;
            try
            {
                dt = DBHelper.ExecuteQuery(@"
                    SELECT
                        s.StudentID, s.FullName, s.Email, s.Mobile, s.Address, s.Gender, s.DOB, s.PhotoPath,
                        s.CountryID, s.StateID, s.DistrictID, s.RegistrationDate,
                        c.CountryName, st.StateName, d.DistrictName
                    FROM Students s
                    INNER JOIN Countries c ON s.CountryID  = c.CountryID
                    INNER JOIN States st   ON s.StateID    = st.StateID
                    INNER JOIN Districts d ON s.DistrictID = d.DistrictID
                    WHERE s.StudentID = @StudentID",
                    new SqlParameter("@StudentID", CurrentStudentId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Dashboard.LoadProfile: database error - " + ex.Message);
                ShowError("We couldn't load your profile right now. Please refresh the page in a moment.");
                return;
            }

            if (dt.Rows.Count == 0)
            {
                // Account no longer exists — force re-login.
                Session.Clear();
                Session.Abandon();
                Response.Redirect("Login.aspx");
                return;
            }

            DataRow row = dt.Rows[0];

            // ---- Read-only overview ----
            litStudentId.Text = row["StudentID"].ToString();
            litFullName.Text = Server.HtmlEncode(row["FullName"].ToString());
            litEmail.Text = Server.HtmlEncode(row["Email"].ToString());
            litMobile.Text = Server.HtmlEncode(row["Mobile"].ToString());
            litGender.Text = row["Gender"] == DBNull.Value ? "-" : row["Gender"].ToString();
            litDOB.Text = row["DOB"] == DBNull.Value ? "-" : Convert.ToDateTime(row["DOB"]).ToString("dd MMM yyyy");
            litCountry.Text = Server.HtmlEncode(row["CountryName"].ToString());
            litState.Text = Server.HtmlEncode(row["StateName"].ToString());
            litDistrict.Text = Server.HtmlEncode(row["DistrictName"].ToString());
            litAddress.Text = row["Address"] == DBNull.Value ? "-" : Server.HtmlEncode(row["Address"].ToString());
            litRegisteredOn.Text = Convert.ToDateTime(row["RegistrationDate"]).ToString("dd MMM yyyy, hh:mm tt");

            DateTime? prevLogin = Session["PrevLastLogin"] as DateTime?;
            litLastLogin.Text = prevLogin.HasValue
                ? prevLogin.Value.ToString("dd MMM yyyy, hh:mm tt")
                : "This is your first login";

            string photoUrl = row["PhotoPath"] == DBNull.Value || string.IsNullOrEmpty(row["PhotoPath"].ToString())
                ? PLACEHOLDER_PHOTO
                : ResolveUrl("~/" + row["PhotoPath"]);
            imgProfilePhoto.Src = photoUrl;
            photoPreviewEdit.Src = photoUrl;

            // ---- Editable form, pre-filled ----
            txtEditStudentId.Text = row["StudentID"].ToString();
            txtEditEmail.Text = row["Email"].ToString();
            txtEditFullName.Text = row["FullName"].ToString();
            txtEditMobile.Text = row["Mobile"].ToString();
            hdnEditFullMobile.Value = row["Mobile"].ToString();
            txtEditAddress.Text = row["Address"] == DBNull.Value ? "" : row["Address"].ToString();

            ddlEditCountry.SelectedValue = row["CountryID"].ToString();
            LoadStates(Convert.ToInt32(row["CountryID"]));
            ddlEditState.SelectedValue = row["StateID"].ToString();
            LoadDistricts(Convert.ToInt32(row["StateID"]));
            ddlEditDistrict.SelectedValue = row["DistrictID"].ToString();
        }

        #endregion

        #region ---- Cascading Dropdowns (Country -> State -> District) ----

        private void LoadCountries()
        {
            DataTable dt = DBHelper.ExecuteQuery("SELECT CountryID, CountryName FROM Countries ORDER BY CountryName");
            ddlEditCountry.DataSource = dt;
            ddlEditCountry.DataTextField = "CountryName";
            ddlEditCountry.DataValueField = "CountryID";
            ddlEditCountry.DataBind();
        }

        private void LoadStates(int countryId)
        {
            ddlEditState.Items.Clear();

            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT StateID, StateName FROM States WHERE CountryID = @CountryID ORDER BY StateName",
                new SqlParameter("@CountryID", countryId));

            foreach (DataRow row in dt.Rows)
            {
                ddlEditState.Items.Add(new ListItem(row["StateName"].ToString(), row["StateID"].ToString()));
            }
        }

        private void LoadDistricts(int stateId)
        {
            ddlEditDistrict.Items.Clear();

            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT DistrictID, DistrictName FROM Districts WHERE StateID = @StateID ORDER BY DistrictName",
                new SqlParameter("@StateID", stateId));

            foreach (DataRow row in dt.Rows)
            {
                ddlEditDistrict.Items.Add(new ListItem(row["DistrictName"].ToString(), row["DistrictID"].ToString()));
            }
        }

        protected void ddlEditCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            int countryId;
            int.TryParse(ddlEditCountry.SelectedValue, out countryId);
            LoadStates(countryId);
            LoadDistricts(-1);
        }

        protected void ddlEditState_SelectedIndexChanged(object sender, EventArgs e)
        {
            int stateId;
            int.TryParse(ddlEditState.SelectedValue, out stateId);
            LoadDistricts(stateId);
        }

        #endregion

        #region ---- Update Profile ----

        protected void btnUpdateProfile_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            if (ddlEditCountry.SelectedValue == "" || ddlEditState.SelectedValue == "" || ddlEditDistrict.SelectedValue == "")
            {
                ShowError("Please select Country, State and District.");
                return;
            }

            string mobile = string.IsNullOrEmpty(hdnEditFullMobile.Value) ? txtEditMobile.Text.Trim() : hdnEditFullMobile.Value;

            string photoPath = null;
            if (fuEditPhoto.HasFile)
            {
                string validationError;
                photoPath = SaveUploadedPhoto(fuEditPhoto, out validationError);
                if (photoPath == null)
                {
                    lblPhotoError.Text = validationError;
                    return;
                }
            }

            try
            {
                string updateSql = photoPath == null
                    ? @"UPDATE Students SET
                            FullName = @FullName, Mobile = @Mobile, Address = @Address,
                            CountryID = @CountryID, StateID = @StateID, DistrictID = @DistrictID
                        WHERE StudentID = @StudentID"
                    : @"UPDATE Students SET
                            FullName = @FullName, Mobile = @Mobile, Address = @Address,
                            CountryID = @CountryID, StateID = @StateID, DistrictID = @DistrictID,
                            PhotoPath = @PhotoPath
                        WHERE StudentID = @StudentID";

                var parameters = new System.Collections.Generic.List<SqlParameter>
                {
                    new SqlParameter("@FullName", txtEditFullName.Text.Trim()),
                    new SqlParameter("@Mobile", mobile),
                    new SqlParameter("@Address", (object)txtEditAddress.Text.Trim() ?? DBNull.Value),
                    new SqlParameter("@CountryID", int.Parse(ddlEditCountry.SelectedValue)),
                    new SqlParameter("@StateID", int.Parse(ddlEditState.SelectedValue)),
                    new SqlParameter("@DistrictID", int.Parse(ddlEditDistrict.SelectedValue)),
                    new SqlParameter("@StudentID", CurrentStudentId)
                };
                if (photoPath != null)
                {
                    parameters.Add(new SqlParameter("@PhotoPath", photoPath));
                }

                DBHelper.ExecuteNonQuery(updateSql, parameters.ToArray());

                Session[SESSION_STUDENT_NAME] = txtEditFullName.Text.Trim();

                ShowSuccess("Your profile was updated successfully.");
                LoadProfile();
            }
            catch (Exception ex)
            {
                ShowError("Profile update failed: " + ex.Message);
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

        #region ---- Logout ----

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Login.aspx");
        }

        #endregion

        #region ---- Helpers ----

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

        #endregion
    }
}
