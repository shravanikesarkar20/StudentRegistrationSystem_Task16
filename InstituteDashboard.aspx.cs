using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 16: Centralised Institute Dashboard.
    /// Shows the logged-in institute's profile + its active modules, and lets the
    /// institute click any active module to drill into module-specific, database-driven
    /// details (Student Management / Fees Management / Timetable Management, or any other
    /// module an admin later adds to dbo.modules).
    /// </summary>
    public partial class InstituteDashboard : Page
    {
        private const string SESSION_INST_ID = "InstId";
        private const string SESSION_INST_NAME = "InstName";
        private const string SESSION_INST_STATUS = "InstStatus";

        /// <summary>Which module's details are currently shown below the module grid.</summary>
        private string SelectedModule
        {
            get { return ViewState["SelectedModule"] as string; }
            set { ViewState["SelectedModule"] = value; }
        }

        private string InstId
        {
            get { return Session[SESSION_INST_ID] as string; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Proper session validation: no session, no dashboard.
            if (string.IsNullOrEmpty(InstId))
            {
                Response.Redirect("InstituteLogin.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadProfile();
                LoadModules();
                pnlModuleDetail.Visible = false;
                pnlNoSelection.Visible = true;
            }
        }

        private void LoadProfile()
        {
            DataRow profile = InstituteDashboardHelper.GetInstituteProfile(InstId);
            if (profile == null)
            {
                // The instreg row disappeared mid-session (e.g. deleted by an admin) - log out cleanly.
                Session.Clear();
                Response.Redirect("InstituteLogin.aspx");
                return;
            }

            litInstName.Text = Server.HtmlEncode(profile["instname"].ToString());
            litInstId.Text = Server.HtmlEncode(profile["instid"].ToString());
            litInstName2.Text = Server.HtmlEncode(profile["instname"].ToString());
            litInstId2.Text = Server.HtmlEncode(profile["instid"].ToString());

            string status = profile["status"].ToString();
            lblStatusBadge.Text = Server.HtmlEncode(status);
            lblStatusBadge.CssClass = "badge-status " + (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
                ? "badge-active" : "badge-inactive");

            // Keep the session copy in sync in case the admin changed it after login.
            Session[SESSION_INST_NAME] = profile["instname"].ToString();
            Session[SESSION_INST_STATUS] = status;
        }

        private void LoadModules()
        {
            DataTable modules = InstituteDashboardHelper.GetActiveModules(InstId);
            rptModules.DataSource = modules;
            rptModules.DataBind();

            int count = InstituteDashboardHelper.GetActiveModuleCount(InstId);
            litModuleCount.Text = count.ToString();

            pnlEmptyModules.Visible = modules.Rows.Count == 0;
        }

        protected void rptModules_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "SelectModule") return;

            string moduleName = e.CommandArgument.ToString();
            SelectModule(moduleName);
        }

        /// <summary>
        /// Loads and renders the detail view for one module, after re-confirming server-side
        /// that the module is an active module belonging to THIS institute (mandatory
        /// isolation requirement - never trust the posted CommandArgument on its own).
        /// </summary>
        private void SelectModule(string moduleName)
        {
            if (!InstituteDashboardHelper.IsActiveModuleForInstitute(InstId, moduleName))
            {
                // Either it never belonged to this institute, or it was deactivated after the
                // page was rendered - either way, refuse silently and just refresh the module list.
                pnlModuleDetail.Visible = false;
                pnlNoSelection.Visible = true;
                LoadModules();
                return;
            }

            SelectedModule = moduleName;

            string instName = Session[SESSION_INST_NAME] as string;
            InstituteDashboardHelper.ModuleDetail detail =
                InstituteDashboardHelper.GetModuleDetail(InstId, instName, moduleName);

            litModuleTitle.Text = Server.HtmlEncode(detail.ModuleName);

            rptStats.DataSource = detail.Stats;
            rptStats.DataBind();

            if (detail.RecentRecords != null && detail.RecentRecords.Rows.Count > 0)
            {
                litRecentSectionTitle.Text = Server.HtmlEncode(detail.RecentSectionTitle ?? "Recent Records");
                gvRecent.DataSource = detail.RecentRecords;
                gvRecent.DataBind();
                pnlRecentEmpty.Visible = false;
                gvRecent.Visible = true;
            }
            else
            {
                gvRecent.Visible = false;
                pnlRecentEmpty.Visible = true;
                pnlRecentEmpty.Controls.Clear();
                string emptyMessage = string.IsNullOrEmpty(detail.Note)
                    ? "No records found yet for this module."
                    : detail.Note;
                pnlRecentEmpty.Controls.Add(new LiteralControl(
                    "<i class=\"bi bi-inbox\"></i>" + Server.HtmlEncode(emptyMessage)));
            }

            pnlModuleNote.Visible = !string.IsNullOrEmpty(detail.Note) && detail.RecentRecords != null && detail.RecentRecords.Rows.Count > 0;
            litModuleNote.Text = Server.HtmlEncode(detail.Note ?? string.Empty);

            pnlNoSelection.Visible = false;
            pnlModuleDetail.Visible = true;

            // Reload the module cards too, so the "selected" highlight class follows the click.
            LoadModules();
        }

        /// <summary>Bound from markup to add a "selected" highlight to the clicked module's card.</summary>
        protected string GetModuleCardClass(object moduleNameObj)
        {
            string moduleName = moduleNameObj == null ? string.Empty : moduleNameObj.ToString();
            return string.Equals(moduleName, SelectedModule, StringComparison.OrdinalIgnoreCase)
                ? "module-card module-card-selected"
                : "module-card";
        }

        /// <summary>Bound from markup to pick a Bootstrap Icon per module name (full freedom on the icon set).</summary>
        protected string GetModuleIcon(object moduleNameObj)
        {
            string moduleName = moduleNameObj == null ? string.Empty : moduleNameObj.ToString();
            switch (moduleName.Trim())
            {
                case "Student Management": return "bi-people-fill";
                case "Fees Management": return "bi-cash-coin";
                case "Timetable Management": return "bi-calendar3";
                default: return "bi-grid-1x2-fill";
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("InstituteLogin.aspx");
        }
    }
}
