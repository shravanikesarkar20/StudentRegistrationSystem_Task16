using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15: Admin Panel &gt; Timetable &gt; Faculty &gt; subject/class assignment + availability grid.</summary>
    public partial class TimetableFacultyEdit : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private int FacultyId
        {
            get { int id; return int.TryParse(Request.QueryString["id"], out id) ? id : -1; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (FacultyId <= 0) { Response.Redirect("TimetableFacultyManagement.aspx"); return; }

            DataRow faculty = TimetableSetupHelper.GetFacultyById(FacultyId);
            if (faculty == null) { Response.Redirect("TimetableFacultyManagement.aspx"); return; }
            litFacultyName.Text = Server.HtmlEncode(faculty["FacultyName"].ToString());

            if (!IsPostBack)
            {
                ddlDivision.DataSource = TimetableSetupHelper.GetDivisions(true);
                ddlDivision.DataTextField = "DivisionName"; // overwritten below with composite text
                ddlDivision.DataValueField = "DivisionID";
                BindDivisionDropdown();
                BindSubjectDropdown();
                BindAssignments();
                BindAvailabilityGrid();
            }
        }

        private void BindDivisionDropdown()
        {
            DataTable divisions = TimetableSetupHelper.GetDivisions(true);
            ddlDivision.Items.Clear();
            foreach (DataRow row in divisions.Rows)
            {
                string text = string.Format("{0} - {1} - Div {2}", row["CourseName"], row["YearSemester"], row["DivisionName"]);
                ddlDivision.Items.Add(new ListItem(text, row["DivisionID"].ToString()));
            }
        }

        protected void ddlDivision_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindSubjectDropdown();
        }

        private void BindSubjectDropdown()
        {
            ddlSubject.Items.Clear();
            int divisionId;
            if (!int.TryParse(ddlDivision.SelectedValue, out divisionId)) return;
            DataTable subjects = TimetableSetupHelper.GetSubjectsForDivision(divisionId);
            foreach (DataRow row in subjects.Rows)
            {
                string text = string.Format("{0} - {1} ({2})", row["SubjectCode"], row["SubjectName"], row["SubjectType"]);
                ddlSubject.Items.Add(new ListItem(text, row["SubjectID"].ToString()));
            }
        }

        private void BindAssignments()
        {
            gvAssignments.DataSource = TimetableSetupHelper.GetFacultyAssignments(FacultyId);
            gvAssignments.DataBind();
        }

        protected void gvAssignments_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (e.CommandName == "Remove" && int.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                TimetableSetupHelper.RemoveFacultyAssignment(id);
                BindAssignments();
            }
        }

        protected void btnAssign_Click(object sender, EventArgs e)
        {
            int divisionId, subjectId;
            if (!int.TryParse(ddlDivision.SelectedValue, out divisionId) || !int.TryParse(ddlSubject.SelectedValue, out subjectId))
            {
                ShowAlert("Select a division and subject first.", "warning");
                return;
            }
            TimetableSetupHelper.AddFacultyAssignment(FacultyId, subjectId, divisionId);
            ShowAlert("Assignment added.", "success");
            BindAssignments();
        }

        // ---------------------------------------------------------------- Availability grid
        // Rendered as plain HTML checkboxes (name="avail_{DayID}_{PeriodID}") inside the
        // server <form>, so a single Save postback reads every checkbox from Request.Form
        // without the overhead of per-cell server controls/postbacks.

        private void BindAvailabilityGrid()
        {
            DataTable days = TimetableSetupHelper.GetWorkingDays();
            DataTable periods = TimetableSetupHelper.GetTeachingPeriods();
            DataTable existing = TimetableSetupHelper.GetFacultyAvailability(FacultyId);

            var availableSet = new HashSet<string>();
            foreach (DataRow row in existing.Rows) availableSet.Add(row["DayID"] + "_" + row["PeriodID"]);
            bool hasExplicitRows = existing.Rows.Count > 0;

            var sb = new StringBuilder();
            sb.Append("<div class='table-responsive'><table class='table table-bordered mb-0'><thead><tr><th>Day \\ Period</th>");
            foreach (DataRow p in periods.Rows) sb.AppendFormat("<th>{0}<br/><small>{1}-{2}</small></th>", p["Label"], p["StartTime"], p["EndTime"]);
            sb.Append("</tr></thead><tbody>");

            foreach (DataRow day in days.Rows)
            {
                int dayId = Convert.ToInt32(day["DayID"]);
                sb.AppendFormat("<tr><td class='fw-semibold'>{0}</td>", day["DayName"]);
                foreach (DataRow p in periods.Rows)
                {
                    int periodId = Convert.ToInt32(p["PeriodID"]);
                    bool isChecked = hasExplicitRows ? availableSet.Contains(dayId + "_" + periodId) : true;
                    sb.AppendFormat(
                        "<td class='text-center'><input type='checkbox' name='avail_{0}_{1}' {2} /></td>",
                        dayId, periodId, isChecked ? "checked='checked'" : "");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table></div>");
            litAvailabilityGrid.Text = sb.ToString();
        }

        protected void btnSaveAvailability_Click(object sender, EventArgs e)
        {
            DataTable days = TimetableSetupHelper.GetWorkingDays();
            DataTable periods = TimetableSetupHelper.GetTeachingPeriods();

            var slots = new List<Tuple<int, int>>();
            int totalCells = 0, checkedCells = 0;
            foreach (DataRow day in days.Rows)
            {
                int dayId = Convert.ToInt32(day["DayID"]);
                foreach (DataRow p in periods.Rows)
                {
                    int periodId = Convert.ToInt32(p["PeriodID"]);
                    totalCells++;
                    if (!string.IsNullOrEmpty(Request.Form["avail_" + dayId + "_" + periodId]))
                    {
                        slots.Add(Tuple.Create(dayId, periodId));
                        checkedCells++;
                    }
                }
            }

            // Every cell checked is equivalent to the default-open model — store nothing so the
            // faculty stays "available every period" even as new periods are added later.
            TimetableSetupHelper.SetFacultyAvailability(FacultyId, checkedCells == totalCells ? new List<Tuple<int, int>>() : slots);
            ShowAlert("Availability saved.", "success");
            BindAvailabilityGrid();
        }

        protected void btnClearAvailability_Click(object sender, EventArgs e)
        {
            TimetableSetupHelper.SetFacultyAvailability(FacultyId, new List<Tuple<int, int>>());
            ShowAlert("Availability reset to default (available every period).", "success");
            BindAvailabilityGrid();
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
