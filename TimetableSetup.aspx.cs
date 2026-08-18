using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15: Admin Panel &gt; Timetable &gt; Academic Setup (working schedule, divisions, subjects).</summary>
    public partial class TimetableSetup : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        public string ActiveTab
        {
            get
            {
                string tab = Request.QueryString["tab"];
                return (tab == "divisions" || tab == "subjects") ? tab : "schedule";
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            pnlSchedule.Visible = ActiveTab == "schedule";
            pnlDivisions.Visible = ActiveTab == "divisions";
            pnlSubjects.Visible = ActiveTab == "subjects";

            if (!IsPostBack)
            {
                LoadLookups();
                BindAll();
            }
        }

        private void LoadLookups()
        {
            ddlDivAcademicYear.DataSource = DBHelper.ExecuteQuery("SELECT AcademicYearID, YearLabel FROM dbo.AcademicYears WHERE IsActive=1 ORDER BY YearLabel DESC");
            ddlDivAcademicYear.DataTextField = "YearLabel"; ddlDivAcademicYear.DataValueField = "AcademicYearID"; ddlDivAcademicYear.DataBind();

            DataTable courses = DBHelper.ExecuteQuery("SELECT CourseID, CourseName FROM dbo.Courses WHERE IsActive=1 ORDER BY CourseName");
            ddlDivCourse.DataSource = courses; ddlDivCourse.DataTextField = "CourseName"; ddlDivCourse.DataValueField = "CourseID"; ddlDivCourse.DataBind();
            ddlSubCourse.DataSource = courses.Copy(); ddlSubCourse.DataTextField = "CourseName"; ddlSubCourse.DataValueField = "CourseID"; ddlSubCourse.DataBind();
        }

        private void BindAll()
        {
            try
            {
                gvDays.DataSource = TimetableSetupHelper.GetWorkingDays(); gvDays.DataBind();
                gvPeriods.DataSource = TimetableSetupHelper.GetPeriods(); gvPeriods.DataBind();
                txtMaxPerDay.Text = TimetableSetupHelper.GetMaxClassesPerDay().ToString();
                gvDivisions.DataSource = TimetableSetupHelper.GetDivisions(); gvDivisions.DataBind();
                gvSubjects.DataSource = TimetableSetupHelper.GetSubjects(); gvSubjects.DataBind();
            }
            catch (Exception ex)
            {
                AppLogger.Error("TimetableSetup.BindAll", "Failed to load academic setup data.", ex);
                ShowAlert("Unable to load setup data right now. Please try again shortly.", "danger");
            }
        }

        // ---------------------------------------------------------------- Working Schedule

        protected void gvDays_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;
            DataTable dt = TimetableSetupHelper.GetWorkingDays();
            DataRow row = null;
            foreach (DataRow r in dt.Rows) { if (Convert.ToInt32(r["DayID"]) == id) { row = r; break; } }
            if (row != null) TimetableSetupHelper.SetWorkingDay(id, !Convert.ToBoolean(row["IsWorkingDay"]));
            gvDays.DataSource = TimetableSetupHelper.GetWorkingDays(); gvDays.DataBind();
        }

        protected void gvPeriods_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            DataRowView row = (DataRowView)e.Row.DataItem;
            bool isBreak = Convert.ToBoolean(row["IsBreak"]);
            Literal lit = (Literal)e.Row.FindControl("litType");
            if (lit != null) lit.Text = isBreak
                ? "<span class='badge-status badge-inactive'>Break</span>"
                : "<span class='badge-status badge-active'>Teaching</span>";
        }

        protected void gvPeriods_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;
            if (e.CommandName == "Delete") TimetableSetupHelper.DeletePeriod(id);
            gvPeriods.DataSource = TimetableSetupHelper.GetPeriods(); gvPeriods.DataBind();
        }

        protected void btnAddPeriod_Click(object sender, EventArgs e)
        {
            int num;
            if (!int.TryParse(txtPeriodNum.Text, out num) || string.IsNullOrWhiteSpace(txtPeriodLabel.Text)
                || string.IsNullOrWhiteSpace(txtPeriodStart.Text) || string.IsNullOrWhiteSpace(txtPeriodEnd.Text))
            {
                ShowAlert("Please fill in period #, label, start and end time.", "warning");
                return;
            }
            TimetableSetupHelper.AddPeriod(num, txtPeriodLabel.Text.Trim(), txtPeriodStart.Text.Trim(), txtPeriodEnd.Text.Trim(), chkPeriodBreak.Checked);
            txtPeriodNum.Text = txtPeriodLabel.Text = txtPeriodStart.Text = txtPeriodEnd.Text = "";
            chkPeriodBreak.Checked = false;
            ShowAlert("Period added.", "success");
            gvPeriods.DataSource = TimetableSetupHelper.GetPeriods(); gvPeriods.DataBind();
        }

        protected void btnSaveMax_Click(object sender, EventArgs e)
        {
            int val;
            if (!int.TryParse(txtMaxPerDay.Text, out val) || val <= 0) { ShowAlert("Enter a valid positive number.", "warning"); return; }
            TimetableSetupHelper.SetMaxClassesPerDay(val, CurrentAdminName);
            ShowAlert("Generation limit saved.", "success");
        }

        // ---------------------------------------------------------------- Divisions

        protected void gvDivisions_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            DataRowView row = (DataRowView)e.Row.DataItem;
            bool active = Convert.ToBoolean(row["IsActive"]);
            Literal lit = (Literal)e.Row.FindControl("litStatus");
            if (lit != null) lit.Text = string.Format("<span class='badge-status {0}'>{1}</span>", active ? "badge-active" : "badge-inactive", active ? "Active" : "Inactive");
        }

        protected void gvDivisions_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;
            DataRow row = TimetableSetupHelper.GetDivision(id);
            if (row != null)
            {
                TimetableSetupHelper.SaveDivision(id, Convert.ToInt32(row["AcademicYearID"]), Convert.ToInt32(row["CourseID"]),
                    row["YearSemester"].ToString(), row["DivisionName"].ToString(), Convert.ToInt32(row["StudentStrength"]),
                    !Convert.ToBoolean(row["IsActive"]));
            }
            gvDivisions.DataSource = TimetableSetupHelper.GetDivisions(); gvDivisions.DataBind();
        }

        protected void btnAddDivision_Click(object sender, EventArgs e)
        {
            int strength;
            if (string.IsNullOrWhiteSpace(txtDivYearSem.Text) || string.IsNullOrWhiteSpace(txtDivName.Text) || !int.TryParse(txtDivStrength.Text, out strength))
            {
                ShowAlert("Please fill in Year/Sem, Division name, and a valid strength.", "warning");
                return;
            }
            TimetableSetupHelper.SaveDivision(0, Convert.ToInt32(ddlDivAcademicYear.SelectedValue), Convert.ToInt32(ddlDivCourse.SelectedValue),
                txtDivYearSem.Text.Trim(), txtDivName.Text.Trim(), strength, true);
            txtDivYearSem.Text = txtDivName.Text = txtDivStrength.Text = "";
            ShowAlert("Division added.", "success");
            gvDivisions.DataSource = TimetableSetupHelper.GetDivisions(); gvDivisions.DataBind();
        }

        // ---------------------------------------------------------------- Subjects

        protected void gvSubjects_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            DataRowView row = (DataRowView)e.Row.DataItem;
            bool active = Convert.ToBoolean(row["IsActive"]);
            Literal lit = (Literal)e.Row.FindControl("litStatus");
            if (lit != null) lit.Text = string.Format("<span class='badge-status {0}'>{1}</span>", active ? "badge-active" : "badge-inactive", active ? "Active" : "Inactive");
        }

        protected void gvSubjects_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;
            DataRow row = TimetableSetupHelper.GetSubject(id);
            if (row != null)
            {
                TimetableSetupHelper.SaveSubject(id, row["SubjectCode"].ToString(), row["SubjectName"].ToString(),
                    Convert.ToInt32(row["CourseID"]), row["YearSemester"].ToString(), row["SubjectType"].ToString(),
                    Convert.ToInt32(row["WeeklyHours"]), !Convert.ToBoolean(row["IsActive"]));
            }
            gvSubjects.DataSource = TimetableSetupHelper.GetSubjects(); gvSubjects.DataBind();
        }

        protected void btnAddSubject_Click(object sender, EventArgs e)
        {
            int weeklyHours;
            if (string.IsNullOrWhiteSpace(txtSubCode.Text) || string.IsNullOrWhiteSpace(txtSubName.Text)
                || string.IsNullOrWhiteSpace(txtSubYearSem.Text) || !int.TryParse(txtSubWeeklyHours.Text, out weeklyHours) || weeklyHours <= 0)
            {
                ShowAlert("Please fill in code, name, Year/Sem, and a valid weekly hours value.", "warning");
                return;
            }
            TimetableSetupHelper.SaveSubject(0, txtSubCode.Text.Trim(), txtSubName.Text.Trim(), Convert.ToInt32(ddlSubCourse.SelectedValue),
                txtSubYearSem.Text.Trim(), ddlSubType.SelectedValue, weeklyHours, true);
            txtSubCode.Text = txtSubName.Text = txtSubYearSem.Text = "";
            txtSubWeeklyHours.Text = "1";
            ShowAlert("Subject added.", "success");
            gvSubjects.DataSource = TimetableSetupHelper.GetSubjects(); gvSubjects.DataBind();
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(Server.HtmlEncode(message)));
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Response.Redirect("AdminLogin.aspx");
        }
    }
}
