using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 12: Admin Panel &gt; Registration Fees &gt; create/edit one fee structure,
    /// including its installment schedule and late fee rule.</summary>
    public partial class FeeStructureEdit : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private int? FeeStructureId
        {
            get
            {
                int id;
                return int.TryParse(hdnFeeStructureId.Value, out id) && id > 0 ? (int?)id : null;
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

                int id;
                if (int.TryParse(Request.QueryString["id"], out id))
                {
                    hdnFeeStructureId.Value = id.ToString();
                    LoadForEdit(id);
                }
                else
                {
                    hdnFeeStructureId.Value = "";
                    txtDueDate.Text = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd");
                    BuildInstallmentPreview();
                }

                UpdateInstallmentPanelVisibility();
            }
        }

        private void LoadLookups()
        {
            BindDropDown(ddlAcademicYear, RegistrationFeeHelper.GetAcademicYears(), "AcademicYearID", "YearLabel");
            BindDropDown(ddlInstitute, RegistrationFeeHelper.GetInstitutes(), "InstituteID", "InstituteName");
            BindDropDown(ddlStudentCategory, RegistrationFeeHelper.GetStudentCategories(), "StudentCategoryID", "CategoryName");
            BindDropDown(ddlFeeType, RegistrationFeeHelper.GetFeeTypes(), "FeeTypeID", "FeeTypeName");

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

        private void LoadForEdit(int id)
        {
            DataRow row = RegistrationFeeHelper.GetFeeStructureById(id);
            if (row == null) { Response.Redirect("FeeStructureManagement.aspx"); return; }

            litPageTitle.Text = "Edit Fee Structure";

            ddlAcademicYear.SelectedValue = row["AcademicYearID"].ToString();
            ddlInstitute.SelectedValue = row["InstituteID"].ToString();
            LoadCoursesForSelectedInstitute();
            ddlCourse.SelectedValue = row["CourseID"].ToString();
            ddlYearSemester.SelectedValue = row["YearSemester"].ToString();
            ddlStudentCategory.SelectedValue = row["StudentCategoryID"].ToString();
            ddlFeeType.SelectedValue = row["FeeTypeID"].ToString();

            txtFeeAmount.Text = Convert.ToDecimal(row["FeeAmount"]).ToString("0.00");
            txtDueDate.Text = Convert.ToDateTime(row["DueDate"]).ToString("yyyy-MM-dd");
            chkActive.Checked = Convert.ToBoolean(row["IsActive"]);

            chkInstallmentsAllowed.Checked = Convert.ToBoolean(row["InstallmentsAllowed"]);
            ddlNumberOfInstallments.SelectedValue = row["NumberOfInstallments"].ToString();

            ddlLateFeeType.SelectedValue = row["LateFeeType"].ToString();
            txtLateFeeValue.Text = Convert.ToDecimal(row["LateFeeValue"]).ToString("0.00");
            txtGraceDays.Text = row["LateFeeGraceDays"].ToString();
            txtLateFeeMax.Text = row["LateFeeMaxAmount"] == DBNull.Value ? "" : Convert.ToDecimal(row["LateFeeMaxAmount"]).ToString("0.00");

            DataTable schedule = RegistrationFeeHelper.GetInstallmentSchedule(id);
            rptInstallments.DataSource = schedule;
            rptInstallments.DataBind();
        }

        protected void chkInstallmentsAllowed_CheckedChanged(object sender, EventArgs e)
        {
            UpdateInstallmentPanelVisibility();
            BuildInstallmentPreview();
        }

        protected void ddlNumberOfInstallments_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildInstallmentPreview();
        }

        private void UpdateInstallmentPanelVisibility()
        {
            pnlInstallments.Visible = chkInstallmentsAllowed.Checked;
            ddlNumberOfInstallments.Enabled = chkInstallmentsAllowed.Checked;
        }

        /// <summary>Rebuilds the editable installment-schedule preview using an even split of
        /// the current Fee Amount / Due Date / Number of Installments. Admin can hand-tune each
        /// row afterwards; final values are re-read (not re-generated) on Save.</summary>
        private void BuildInstallmentPreview()
        {
            int count = chkInstallmentsAllowed.Checked ? int.Parse(ddlNumberOfInstallments.SelectedValue) : 1;
            DateTime baseDue;
            if (!DateTime.TryParse(txtDueDate.Text, out baseDue)) baseDue = DateTime.Today.AddMonths(1);

            DataTable dt = new DataTable();
            dt.Columns.Add("InstallmentNo", typeof(int));
            dt.Columns.Add("DueDate", typeof(DateTime));
            dt.Columns.Add("AmountPercent", typeof(decimal));

            decimal evenShare = Math.Floor((100m / count) * 100m) / 100m;
            decimal running = 0m;
            for (int i = 1; i <= count; i++)
            {
                decimal percent = (i == count) ? (100m - running) : evenShare;
                running += percent;
                DateTime due = count == 1 ? baseDue : baseDue.AddMonths(i - 1);
                dt.Rows.Add(i, due, percent);
            }

            rptInstallments.DataSource = dt;
            rptInstallments.DataBind();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                decimal feeAmount = decimal.Parse(txtFeeAmount.Text, CultureInfo.InvariantCulture);
                DateTime dueDate = DateTime.Parse(txtDueDate.Text, CultureInfo.InvariantCulture);

                RegistrationFeeHelper.FeeStructureInput input = new RegistrationFeeHelper.FeeStructureInput
                {
                    FeeStructureID = FeeStructureId,
                    AcademicYearID = int.Parse(ddlAcademicYear.SelectedValue),
                    InstituteID = int.Parse(ddlInstitute.SelectedValue),
                    CourseID = int.Parse(ddlCourse.SelectedValue),
                    YearSemester = ddlYearSemester.SelectedValue,
                    StudentCategoryID = int.Parse(ddlStudentCategory.SelectedValue),
                    FeeTypeID = int.Parse(ddlFeeType.SelectedValue),
                    FeeAmount = feeAmount,
                    DueDate = dueDate,
                    InstallmentsAllowed = chkInstallmentsAllowed.Checked,
                    NumberOfInstallments = chkInstallmentsAllowed.Checked ? int.Parse(ddlNumberOfInstallments.SelectedValue) : 1,
                    LateFeeType = ddlLateFeeType.SelectedValue,
                    LateFeeValue = decimal.Parse(txtLateFeeValue.Text, CultureInfo.InvariantCulture),
                    LateFeeGraceDays = int.Parse(txtGraceDays.Text),
                    LateFeeMaxAmount = string.IsNullOrWhiteSpace(txtLateFeeMax.Text) ? (decimal?)null : decimal.Parse(txtLateFeeMax.Text, CultureInfo.InvariantCulture),
                    IsActive = chkActive.Checked,
                    ActorName = CurrentAdminName,
                    Installments = ReadInstallmentRowsFromRepeater()
                };

                int id = RegistrationFeeHelper.SaveFeeStructure(input);
                AppLogger.Info("FeeStructureEdit", "Fee structure " + id + " saved by " + CurrentAdminName);

                Response.Redirect("FeeStructureManagement.aspx?msg=" + (FeeStructureId.HasValue ? "updated" : "created"));
            }
            catch (ArgumentException ex)
            {
                ShowAlert(ex.Message, "danger");
            }
            catch (Exception ex)
            {
                AppLogger.Error("FeeStructureEdit.btnSave_Click", "Failed to save fee structure.", ex);
                ShowAlert("Could not save the fee structure due to a server error. Please try again.", "danger");
            }
        }

        private List<Tuple<int, DateTime, decimal>> ReadInstallmentRowsFromRepeater()
        {
            List<Tuple<int, DateTime, decimal>> rows = new List<Tuple<int, DateTime, decimal>>();
            foreach (RepeaterItem item in rptInstallments.Items)
            {
                HiddenField hdnNo = (HiddenField)item.FindControl("hdnNo");
                TextBox txtDate = (TextBox)item.FindControl("txtInstDueDate");
                TextBox txtPercent = (TextBox)item.FindControl("txtInstPercent");
                if (hdnNo == null || txtDate == null || txtPercent == null) continue;

                int no = int.Parse(hdnNo.Value);
                DateTime due = DateTime.Parse(txtDate.Text, CultureInfo.InvariantCulture);
                decimal percent = decimal.Parse(txtPercent.Text, CultureInfo.InvariantCulture);
                rows.Add(Tuple.Create(no, due, percent));
            }
            return rows;
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(Server.HtmlEncode(message)));
        }
    }
}
