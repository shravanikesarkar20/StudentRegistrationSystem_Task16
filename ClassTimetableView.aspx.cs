using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15, Section I(b): public page — students pick their division and view its
    /// published timetable. No login required, mirroring Display.aspx's public access pattern.</summary>
    public partial class ClassTimetableView : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
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
                string text = string.Format("{0} - {1} - Div {2}", row["CourseName"], row["YearSemester"], row["DivisionName"]);
                ddlDivision.Items.Add(new ListItem(text, row["DivisionID"].ToString()));
            }
        }

        protected void ddlAcademicYear_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindDivisions();
            pnlGrid.Visible = false;
            pnlEmpty.Visible = false;
        }

        protected void btnView_Click(object sender, EventArgs e)
        {
            int academicYearId, divisionId;
            if (!int.TryParse(ddlAcademicYear.SelectedValue, out academicYearId) || !int.TryParse(ddlDivision.SelectedValue, out divisionId)) return;

            DataTable entries = TimetableHelper.GetDivisionTimetable(academicYearId, divisionId);
            if (entries.Rows.Count == 0)
            {
                pnlGrid.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            litHeading.Text = Server.HtmlEncode(ddlDivision.SelectedItem.Text);
            litGrid.Text = TimetableHelper.RenderGridHtml(entries, false);
            pnlGrid.Visible = true;
            pnlEmpty.Visible = false;
        }
    }
}
