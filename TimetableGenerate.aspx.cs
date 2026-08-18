using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15, Section E/F: Admin Panel &gt; Timetable &gt; auto-generate + preview + unresolved conflicts.</summary>
    public partial class TimetableGenerate : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack)
            {
                ddlAcademicYear.DataSource = DBHelper.ExecuteQuery("SELECT AcademicYearID, YearLabel FROM dbo.AcademicYears WHERE IsActive=1 ORDER BY YearLabel DESC");
                ddlAcademicYear.DataTextField = "YearLabel"; ddlAcademicYear.DataValueField = "AcademicYearID"; ddlAcademicYear.DataBind();
                BindDivisions();
            }
        }

        private void BindDivisions()
        {
            int academicYearId;
            if (!int.TryParse(ddlAcademicYear.SelectedValue, out academicYearId)) return;

            DataTable divisions = TimetableSetupHelper.GetDivisions(true);
            ddlDivision.Items.Clear();
            foreach (DataRow row in divisions.Rows)
            {
                if (Convert.ToInt32(row["AcademicYearID"]) != academicYearId) continue;
                string text = string.Format("{0} - {1} - Div {2} ({3} students)", row["CourseName"], row["YearSemester"], row["DivisionName"], row["StudentStrength"]);
                ddlDivision.Items.Add(new ListItem(text, row["DivisionID"].ToString()));
            }
        }

        protected void ddlAcademicYear_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindDivisions();
            pnlGrid.Visible = false;
            pnlUnresolved.Visible = false;
        }

        protected void btnGenerate_Click(object sender, EventArgs e)
        {
            int academicYearId, divisionId;
            if (!int.TryParse(ddlAcademicYear.SelectedValue, out academicYearId) || !int.TryParse(ddlDivision.SelectedValue, out divisionId))
            {
                ShowAlert("Select an academic year and division first.", "warning");
                return;
            }

            try
            {
                var result = TimetableGenerationEngine.GenerateForDivision(academicYearId, divisionId, CurrentAdminName);
                ShowAlert(string.Format("Generation complete: {0} period(s) placed, {1} unresolved.", result.PlacedCount, result.UnresolvedMessages.Count),
                    result.UnresolvedMessages.Count == 0 ? "success" : "warning");
                RenderResults(academicYearId, divisionId);
            }
            catch (Exception ex)
            {
                AppLogger.Error("TimetableGenerate.btnGenerate_Click", "Generation failed.", ex);
                ShowAlert("Generation failed. Please check the academic setup (subjects, faculty assignments, rooms) and try again.", "danger");
            }
        }

        protected void btnView_Click(object sender, EventArgs e)
        {
            int academicYearId, divisionId;
            if (!int.TryParse(ddlAcademicYear.SelectedValue, out academicYearId) || !int.TryParse(ddlDivision.SelectedValue, out divisionId))
            {
                ShowAlert("Select an academic year and division first.", "warning");
                return;
            }
            RenderResults(academicYearId, divisionId);
        }

        private void RenderResults(int academicYearId, int divisionId)
        {
            DataTable entries = TimetableHelper.GetDivisionTimetable(academicYearId, divisionId);
            litGrid.Text = TimetableHelper.RenderGridHtml(entries, false);
            pnlGrid.Visible = true;
            hlEdit.NavigateUrl = string.Format("TimetableEditor.aspx?ay={0}&div={1}", academicYearId, divisionId);

            DataTable unresolved = TimetableHelper.GetUnresolved(academicYearId, divisionId);
            if (unresolved.Rows.Count > 0)
            {
                var sb = new StringBuilder("<ul class='mb-0'>");
                foreach (DataRow row in unresolved.Rows)
                    sb.AppendFormat("<li><strong>{0}</strong>: {1}</li>", HttpUtility.HtmlEncode(row["SubjectName"].ToString()), HttpUtility.HtmlEncode(row["Reason"].ToString()));
                sb.Append("</ul>");
                litUnresolved.Text = sb.ToString();
                pnlUnresolved.Visible = true;
            }
            else
            {
                pnlUnresolved.Visible = false;
            }
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
