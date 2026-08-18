using System;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 12: Admin Panel &gt; Student Dues &mdash; search a student, maintain their
    /// academic profile, auto-generate their fee demand, and review/adjust it.</summary>
    public partial class StudentFeeDemand : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private int? SelectedStudentId
        {
            get
            {
                int id;
                return int.TryParse(hdnStudentId.Value, out id) && id > 0 ? (int?)id : null;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack)
            {
                LoadLookups();

                int studentId;
                if (int.TryParse(Request.QueryString["studentId"], out studentId))
                {
                    SelectStudent(studentId);
                }
            }
            else if (hdnClear.Value == "1")
            {
                hdnStudentId.Value = "";
                hdnClear.Value = "";
                pnlStudent.Visible = false;
            }
        }

        private void LoadLookups()
        {
            BindDropDown(ddlAcademicYear, RegistrationFeeHelper.GetAcademicYears(), "AcademicYearID", "YearLabel");
            BindDropDown(ddlInstitute, RegistrationFeeHelper.GetInstitutes(), "InstituteID", "InstituteName");
            BindDropDown(ddlStudentCategory, RegistrationFeeHelper.GetStudentCategories(), "StudentCategoryID", "CategoryName");

            ddlYearSemester.Items.Clear();
            foreach (string ys in RegistrationFeeHelper.YearSemesterOptions)
                ddlYearSemester.Items.Add(new ListItem(ys, ys));

            LoadCoursesForSelectedInstitute();
        }

        private void LoadCoursesForSelectedInstitute()
        {
            int instituteId;
            int.TryParse(ddlInstitute.SelectedValue, out instituteId);
            BindDropDown(ddlCourse, RegistrationFeeHelper.GetCourses(instituteId > 0 ? instituteId : (int?)null), "CourseID", "CourseName");
        }

        private static void BindDropDown(DropDownList ddl, DataTable dt, string valueField, string textField)
        {
            ddl.DataSource = dt;
            ddl.DataValueField = valueField;
            ddl.DataTextField = textField;
            ddl.DataBind();
        }

        protected void ddlInstitute_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCoursesForSelectedInstitute();
        }

        protected void btnFindStudent_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = RegistrationFeeHelper.SearchStudents(txtStudentSearch.Text.Trim());
                gvStudentResults.DataSource = dt;
                gvStudentResults.DataBind();
            }
            catch (Exception ex)
            {
                AppLogger.Error("StudentFeeDemand.btnFindStudent_Click", "Student search failed.", ex);
                ShowAlert("Search failed due to a server error.", "danger");
            }
        }

        protected void gvStudentResults_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Select") return;
            int studentId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out studentId)) return;
            SelectStudent(studentId);
        }

        private void SelectStudent(int studentId)
        {
            DataRow student = RegistrationFeeHelper.GetStudentById(studentId);
            if (student == null) { ShowAlert("Student not found.", "warning"); return; }

            hdnStudentId.Value = studentId.ToString();
            pnlStudent.Visible = true;
            litStudentHeader.Text = Server.HtmlEncode(student["FullName"] + " (ID " + studentId + ")");
            lnkRecordPayment.HRef = "RecordPayment.aspx?studentId=" + studentId;

            LoadLookups();

            DataRow profile = RegistrationFeeHelper.GetStudentAcademicProfile(studentId);
            if (profile != null)
            {
                ddlAcademicYear.SelectedValue = profile["AcademicYearID"].ToString();
                ddlInstitute.SelectedValue = profile["InstituteID"].ToString();
                LoadCoursesForSelectedInstitute();
                ddlCourse.SelectedValue = profile["CourseID"].ToString();
                ddlYearSemester.SelectedValue = profile["YearSemester"].ToString();
                ddlStudentCategory.SelectedValue = profile["StudentCategoryID"].ToString();
            }

            LoadSummary(studentId);
        }

        protected void btnSaveProfile_Click(object sender, EventArgs e)
        {
            int? studentId = SelectedStudentId;
            if (!studentId.HasValue) return;

            try
            {
                RegistrationFeeHelper.SaveStudentAcademicProfile(studentId.Value,
                    int.Parse(ddlAcademicYear.SelectedValue), int.Parse(ddlInstitute.SelectedValue),
                    int.Parse(ddlCourse.SelectedValue), ddlYearSemester.SelectedValue,
                    int.Parse(ddlStudentCategory.SelectedValue), CurrentAdminName);

                ShowAlert("Academic profile saved.", "success");
                LoadSummary(studentId.Value);
            }
            catch (Exception ex)
            {
                AppLogger.Error("StudentFeeDemand.btnSaveProfile_Click", "Failed to save academic profile.", ex);
                ShowAlert("Could not save the academic profile due to a server error.", "danger");
            }
        }

        protected void btnGenerateDemand_Click(object sender, EventArgs e)
        {
            int? studentId = SelectedStudentId;
            if (!studentId.HasValue) return;

            try
            {
                int created = RegistrationFeeHelper.GenerateFeeDemandsForStudent(studentId.Value, CurrentAdminName);
                ShowAlert(created > 0
                    ? string.Format("{0} new fee head{1} generated for this student.", created, created == 1 ? "" : "s")
                    : "No new fee heads to generate — the student's fee demand is already up to date with the configured structures.", "success");
            }
            catch (InvalidOperationException ex)
            {
                ShowAlert(ex.Message, "warning");
            }
            catch (Exception ex)
            {
                AppLogger.Error("StudentFeeDemand.btnGenerateDemand_Click", "Failed to generate fee demand.", ex);
                ShowAlert("Could not generate the fee demand due to a server error.", "danger");
            }

            LoadSummary(studentId.Value);
        }

        private void LoadSummary(int studentId)
        {
            try
            {
                RegistrationFeeHelper.FeeSummary summary = RegistrationFeeHelper.GetStudentFeeSummary(studentId);
                pnlSummary.Visible = true;

                litTotalPayable.Text = summary.TotalPayable.ToString("N2");
                litAmountPaid.Text = summary.AmountPaid.ToString("N2");
                litOutstanding.Text = summary.OutstandingAmount.ToString("N2");
                litLateFee.Text = summary.LateFeeOutstanding.ToString("N2");
                litDiscount.Text = summary.DiscountAmount.ToString("N2");
                litNetPayable.Text = summary.NetPayable.ToString("N2");
                string statusCssKey = summary.PaymentStatus == "No Dues Generated" ? "Pending" : summary.PaymentStatus;
                litPaymentStatus.Text = string.Format("<span class='badge-status status-{0}'>{1}</span>",
                    statusCssKey, Server.HtmlEncode(summary.PaymentStatus));

                gvFeeHeads.DataSource = RegistrationFeeHelper.GetStudentFeeDemands(studentId);
                gvFeeHeads.DataBind();

                gvTransactions.DataSource = RegistrationFeeHelper.GetTransactionsForStudent(studentId);
                gvTransactions.DataBind();
            }
            catch (Exception ex)
            {
                AppLogger.Error("StudentFeeDemand.LoadSummary", "Failed to load fee summary.", ex);
                ShowAlert("Could not load the fee summary due to a server error.", "danger");
            }
        }

        protected void gvFeeHeads_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ApplyDiscount") return;
            int? studentId = SelectedStudentId;
            if (!studentId.HasValue) return;

            int feeDemandId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out feeDemandId)) return;

            GridViewRow row = ((Control)e.CommandSource).NamingContainer as GridViewRow;
            if (row == null) return;

            TextBox txtAmt = (TextBox)row.FindControl("txtDiscountAmt");
            TextBox txtReason = (TextBox)row.FindControl("txtDiscountReason");

            try
            {
                decimal amount;
                if (!decimal.TryParse(txtAmt.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                {
                    ShowAlert("Enter a valid discount amount.", "warning");
                }
                else
                {
                    RegistrationFeeHelper.ApplyDiscount(feeDemandId, amount, txtReason.Text.Trim(), CurrentAdminName);
                    ShowAlert("Discount applied.", "success");
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                ShowAlert(ex.Message, "warning");
            }
            catch (Exception ex)
            {
                AppLogger.Error("StudentFeeDemand.gvFeeHeads_RowCommand", "Failed to apply discount.", ex);
                ShowAlert("Could not apply the discount due to a server error.", "danger");
            }

            LoadSummary(studentId.Value);
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(Server.HtmlEncode(message)));
        }
    }
}
