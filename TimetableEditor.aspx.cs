using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15, Sections G/H: Admin Panel &gt; Timetable &gt; manual move + swap, both
    /// validated through TimetableHelper (which delegates to TimetableConflictDetector) before
    /// anything is written — "It should not simply allow the change."</summary>
    public partial class TimetableEditor : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private int AcademicYearId
        {
            get { int id; return int.TryParse(Request.QueryString["ay"], out id) ? id : -1; }
        }

        private int DivisionId
        {
            get { int id; return int.TryParse(Request.QueryString["div"], out id) ? id : -1; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (AcademicYearId <= 0 || DivisionId <= 0) { Response.Redirect("TimetableGenerate.aspx"); return; }

            DataRow division = TimetableSetupHelper.GetDivision(DivisionId);
            if (division == null) { Response.Redirect("TimetableGenerate.aspx"); return; }
            litDivisionName.Text = Server.HtmlEncode(string.Format("{0} &middot; Division {1}", division["YearSemester"], division["DivisionName"]));

            if (!IsPostBack)
            {
                LoadLookups();
                BindGrid();
                BindEntryDropdowns();
            }
        }

        private void LoadLookups()
        {
            DataTable days = TimetableSetupHelper.GetWorkingDays();
            ddlEditDay.DataSource = days; ddlEditDay.DataTextField = "DayName"; ddlEditDay.DataValueField = "DayID"; ddlEditDay.DataBind();

            DataTable periods = TimetableSetupHelper.GetTeachingPeriods();
            ddlEditPeriod.Items.Clear();
            foreach (DataRow p in periods.Rows)
                ddlEditPeriod.Items.Add(new ListItem(string.Format("{0} ({1}-{2})", p["Label"], p["StartTime"], p["EndTime"]), p["PeriodID"].ToString()));

            DataTable faculty = TimetableSetupHelper.GetFaculty(true);
            ddlEditFaculty.DataSource = faculty; ddlEditFaculty.DataTextField = "FacultyName"; ddlEditFaculty.DataValueField = "FacultyID"; ddlEditFaculty.DataBind();

            DataTable rooms = TimetableSetupHelper.GetRooms(true);
            ddlEditRoom.Items.Clear();
            foreach (DataRow r in rooms.Rows)
                ddlEditRoom.Items.Add(new ListItem(string.Format("{0} ({1}, cap {2})", r["RoomNumber"], r["RoomType"], r["Capacity"]), r["RoomID"].ToString()));
        }

        private void BindGrid()
        {
            DataTable entries = TimetableHelper.GetDivisionTimetable(AcademicYearId, DivisionId);
            litGrid.Text = TimetableHelper.RenderGridHtml(entries, true);
        }

        private void BindEntryDropdowns()
        {
            DataTable entries = TimetableHelper.GetDivisionTimetable(AcademicYearId, DivisionId);
            ddlEditEntry.Items.Clear();
            ddlSwapA.Items.Clear();
            ddlSwapB.Items.Clear();
            foreach (DataRow row in entries.Rows)
            {
                string text = string.Format("{0} P{1} - {2} ({3})", row["DayName"], row["PeriodNumber"], row["SubjectName"], row["FacultyName"]);
                ddlEditEntry.Items.Add(new ListItem(text, row["EntryID"].ToString()));
                ddlSwapA.Items.Add(new ListItem(text, row["EntryID"].ToString()));
                ddlSwapB.Items.Add(new ListItem(text, row["EntryID"].ToString()));
            }
        }

        protected void btnMove_Click(object sender, EventArgs e)
        {
            int entryId, dayId, periodId, facultyId, roomId;
            if (!int.TryParse(ddlEditEntry.SelectedValue, out entryId) || !int.TryParse(ddlEditDay.SelectedValue, out dayId)
                || !int.TryParse(ddlEditPeriod.SelectedValue, out periodId) || !int.TryParse(ddlEditFaculty.SelectedValue, out facultyId)
                || !int.TryParse(ddlEditRoom.SelectedValue, out roomId))
            {
                litMoveResult.Text = "<div class='alert alert-warning py-2 small mt-2 mb-0'>Select an entry and all target fields.</div>";
                return;
            }

            var conflicts = TimetableHelper.TryMoveEntry(entryId, dayId, periodId, facultyId, roomId);
            if (conflicts.Count == 0)
            {
                litMoveResult.Text = "<div class='alert alert-success py-2 small mt-2 mb-0'>Moved successfully — no conflicts.</div>";
                BindGrid();
                BindEntryDropdowns();
            }
            else
            {
                var sb = new System.Text.StringBuilder("<div class='alert alert-danger py-2 small mt-2 mb-0'><strong>Not moved — conflicts found:</strong><ul class='mb-0'>");
                foreach (string reason in conflicts) sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(reason));
                sb.Append("</ul></div>");
                litMoveResult.Text = sb.ToString();
            }
        }

        protected void btnSwap_Click(object sender, EventArgs e)
        {
            int entryA, entryB;
            if (!int.TryParse(ddlSwapA.SelectedValue, out entryA) || !int.TryParse(ddlSwapB.SelectedValue, out entryB))
            {
                litSwapResult.Text = "<div class='alert alert-warning py-2 small mt-2 mb-0'>Select two entries to swap.</div>";
                return;
            }
            if (entryA == entryB)
            {
                litSwapResult.Text = "<div class='alert alert-warning py-2 small mt-2 mb-0'>Select two different entries.</div>";
                return;
            }

            var conflicts = TimetableHelper.TrySwapEntries(entryA, entryB);
            if (conflicts.Count == 0)
            {
                litSwapResult.Text = "<div class='alert alert-success py-2 small mt-2 mb-0'>Swapped successfully — no conflicts.</div>";
                BindGrid();
                BindEntryDropdowns();
            }
            else
            {
                var sb = new System.Text.StringBuilder("<div class='alert alert-danger py-2 small mt-2 mb-0'><strong>Swap rejected — conflicts found:</strong><ul class='mb-0'>");
                foreach (string reason in conflicts) sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(reason));
                sb.Append("</ul></div>");
                litSwapResult.Text = sb.ToString();
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Response.Redirect("AdminLogin.aspx");
        }
    }
}
