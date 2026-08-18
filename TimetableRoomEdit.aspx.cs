using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15: Admin Panel &gt; Timetable &gt; Rooms/Labs &gt; availability grid.</summary>
    public partial class TimetableRoomEdit : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private int RoomId
        {
            get { int id; return int.TryParse(Request.QueryString["id"], out id) ? id : -1; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (RoomId <= 0) { Response.Redirect("TimetableRoomManagement.aspx"); return; }
            DataRow room = TimetableSetupHelper.GetRoomById(RoomId);
            if (room == null) { Response.Redirect("TimetableRoomManagement.aspx"); return; }
            litRoomName.Text = Server.HtmlEncode(string.Format("{0} ({1})", room["RoomNumber"], room["RoomType"]));

            if (!IsPostBack) BindAvailabilityGrid();
        }

        private void BindAvailabilityGrid()
        {
            DataTable days = TimetableSetupHelper.GetWorkingDays();
            DataTable periods = TimetableSetupHelper.GetTeachingPeriods();
            DataTable existing = TimetableSetupHelper.GetRoomAvailability(RoomId);

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
                    sb.AppendFormat("<td class='text-center'><input type='checkbox' name='avail_{0}_{1}' {2} /></td>",
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

            TimetableSetupHelper.SetRoomAvailability(RoomId, checkedCells == totalCells ? new List<Tuple<int, int>>() : slots);
            ShowAlert("Availability saved.", "success");
            BindAvailabilityGrid();
        }

        protected void btnClearAvailability_Click(object sender, EventArgs e)
        {
            TimetableSetupHelper.SetRoomAvailability(RoomId, new List<Tuple<int, int>>());
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
