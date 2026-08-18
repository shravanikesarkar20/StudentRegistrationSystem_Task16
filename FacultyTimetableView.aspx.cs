using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15, Section I(a): public page — a faculty member selects their own name
    /// and views their weekly schedule across all divisions they teach.</summary>
    public partial class FacultyTimetableView : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ddlFaculty.DataSource = TimetableSetupHelper.GetFaculty(true);
                ddlFaculty.DataTextField = "FacultyName"; ddlFaculty.DataValueField = "FacultyID";
                ddlFaculty.DataBind();
            }
        }

        protected void btnView_Click(object sender, EventArgs e)
        {
            int facultyId;
            if (!int.TryParse(ddlFaculty.SelectedValue, out facultyId)) return;

            DataTable schedule = TimetableHelper.GetFacultyTimetable(facultyId);
            if (schedule.Rows.Count == 0)
            {
                pnlGrid.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            litHeading.Text = Server.HtmlEncode(ddlFaculty.SelectedItem.Text);
            gvSchedule.DataSource = schedule;
            gvSchedule.DataBind();
            pnlGrid.Visible = true;
            pnlEmpty.Visible = false;
        }
    }
}
