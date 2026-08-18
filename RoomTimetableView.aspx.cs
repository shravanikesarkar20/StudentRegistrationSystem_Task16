using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15, Section I(c): Admin Panel &gt; Timetable &gt; per-room utilization,
    /// helping identify unused rooms and scheduling conflicts.</summary>
    public partial class RoomTimetableView : Page
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
                ddlRoom.DataSource = TimetableSetupHelper.GetRooms(true);
                ddlRoom.DataTextField = "RoomNumber"; ddlRoom.DataValueField = "RoomID";
                ddlRoom.DataBind();
                BindUsage();
            }
        }

        protected void ddlRoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindUsage();
        }

        private void BindUsage()
        {
            int roomId;
            if (!int.TryParse(ddlRoom.SelectedValue, out roomId)) return;

            DataTable usage = TimetableHelper.GetRoomUtilization(roomId);
            if (usage.Rows.Count == 0)
            {
                pnlGrid.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            gvUsage.DataSource = usage;
            gvUsage.DataBind();
            pnlGrid.Visible = true;
            pnlEmpty.Visible = false;
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Response.Redirect("AdminLogin.aspx");
        }
    }
}
