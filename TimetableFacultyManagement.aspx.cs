using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15: Admin Panel &gt; Timetable &gt; Faculty list.</summary>
    public partial class TimetableFacultyManagement : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack) BindGrid();
        }

        private void BindGrid()
        {
            try { gvFaculty.DataSource = TimetableSetupHelper.GetFaculty(); gvFaculty.DataBind(); }
            catch (Exception ex)
            {
                AppLogger.Error("TimetableFacultyManagement.BindGrid", "Failed to load faculty.", ex);
                ShowAlert("Unable to load faculty right now. Please try again shortly.", "danger");
            }
        }

        protected void gvFaculty_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            DataRowView row = (DataRowView)e.Row.DataItem;
            bool active = Convert.ToBoolean(row["IsActive"]);
            Literal lit = (Literal)e.Row.FindControl("litStatus");
            if (lit != null) lit.Text = string.Format("<span class='badge-status {0}'>{1}</span>", active ? "badge-active" : "badge-inactive", active ? "Active" : "Inactive");
        }

        protected void gvFaculty_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;
            DataRow row = TimetableSetupHelper.GetFacultyById(id);
            if (row != null)
            {
                TimetableSetupHelper.SaveFaculty(id, row["FacultyName"].ToString(),
                    row["Email"] == DBNull.Value ? null : row["Email"].ToString(),
                    row["Department"] == DBNull.Value ? null : row["Department"].ToString(),
                    !Convert.ToBoolean(row["IsActive"]));
            }
            BindGrid();
        }

        protected void btnAddFaculty_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { ShowAlert("Please enter a faculty name.", "warning"); return; }
            TimetableSetupHelper.SaveFaculty(0, txtName.Text.Trim(), txtEmail.Text.Trim(), txtDept.Text.Trim(), true);
            txtName.Text = txtEmail.Text = txtDept.Text = "";
            ShowAlert("Faculty added.", "success");
            BindGrid();
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
