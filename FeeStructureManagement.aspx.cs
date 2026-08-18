using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 12: Admin Panel &gt; Registration Fees &gt; list of configured fee structures.</summary>
    public partial class FeeStructureManagement : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private string SearchTerm
        {
            get { return (ViewState["FS_Search"] as string) ?? string.Empty; }
            set { ViewState["FS_Search"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack)
            {
                txtSearch.Text = string.Empty;
                BindGrid();

                switch (Request.QueryString["msg"])
                {
                    case "created": ShowAlert("Fee structure created successfully.", "success"); break;
                    case "updated": ShowAlert("Fee structure updated successfully.", "success"); break;
                }
            }
        }

        private void BindGrid()
        {
            try
            {
                DataTable dt = RegistrationFeeHelper.GetFeeStructures(SearchTerm, null);
                gvStructures.DataSource = dt;
                gvStructures.DataBind();
                litResultCount.Text = dt.Rows.Count == 0
                    ? "No fee structures found."
                    : string.Format("{0} fee structure{1} found.", dt.Rows.Count, dt.Rows.Count == 1 ? "" : "s");
            }
            catch (Exception ex)
            {
                AppLogger.Error("FeeStructureManagement.BindGrid", "Failed to load fee structures.", ex);
                ShowAlert("Unable to load fee structures right now. Please try again shortly.", "danger");
            }
        }

        protected void gvStructures_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            DataRowView row = (DataRowView)e.Row.DataItem;
            bool isActive = Convert.ToBoolean(row["IsActive"]);
            Literal litBadge = (Literal)e.Row.FindControl("litStatusBadge");
            if (litBadge != null)
            {
                litBadge.Text = string.Format("<span class='badge-status {0}'>{1}</span>",
                    isActive ? "badge-active" : "badge-inactive", isActive ? "Active" : "Inactive");
            }
        }

        protected void gvStructures_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            try
            {
                if (e.CommandName == "ToggleActive")
                {
                    DataRow row = RegistrationFeeHelper.GetFeeStructureById(id);
                    if (row == null) { ShowAlert("Fee structure not found.", "warning"); }
                    else
                    {
                        bool newStatus = !Convert.ToBoolean(row["IsActive"]);
                        RegistrationFeeHelper.SetFeeStructureActive(id, newStatus, CurrentAdminName);
                        ShowAlert("Fee structure is now " + (newStatus ? "active." : "inactive."), "success");
                    }
                }
                else if (e.CommandName == "DeleteStructure")
                {
                    RegistrationFeeHelper.DeleteFeeStructure(id);
                    ShowAlert("Fee structure deleted.", "success");
                }
            }
            catch (InvalidOperationException ex)
            {
                ShowAlert(ex.Message, "warning");
            }
            catch (Exception ex)
            {
                AppLogger.Error("FeeStructureManagement.gvStructures_RowCommand", "Command " + e.CommandName + " failed for id " + id, ex);
                ShowAlert("The action could not be completed due to a server error.", "danger");
            }

            BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { SearchTerm = txtSearch.Text.Trim(); BindGrid(); }
        protected void btnClear_Click(object sender, EventArgs e) { txtSearch.Text = string.Empty; SearchTerm = string.Empty; BindGrid(); }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("AdminLogin.aspx");
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(Server.HtmlEncode(message)));
        }
    }
}
