using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 15: Admin Panel &gt; Timetable &gt; Rooms/Labs list.</summary>
    public partial class TimetableRoomManagement : Page
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
            try { gvRooms.DataSource = TimetableSetupHelper.GetRooms(); gvRooms.DataBind(); }
            catch (Exception ex)
            {
                AppLogger.Error("TimetableRoomManagement.BindGrid", "Failed to load rooms.", ex);
                ShowAlert("Unable to load rooms right now. Please try again shortly.", "danger");
            }
        }

        protected void gvRooms_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            DataRowView row = (DataRowView)e.Row.DataItem;
            bool active = Convert.ToBoolean(row["IsActive"]);
            Literal lit = (Literal)e.Row.FindControl("litStatus");
            if (lit != null) lit.Text = string.Format("<span class='badge-status {0}'>{1}</span>", active ? "badge-active" : "badge-inactive", active ? "Active" : "Inactive");
        }

        protected void gvRooms_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;
            DataRow row = TimetableSetupHelper.GetRoomById(id);
            if (row != null)
            {
                TimetableSetupHelper.SaveRoom(id, row["RoomNumber"].ToString(), row["RoomType"].ToString(),
                    Convert.ToInt32(row["Capacity"]), row["Building"] == DBNull.Value ? null : row["Building"].ToString(),
                    !Convert.ToBoolean(row["IsActive"]));
            }
            BindGrid();
        }

        protected void btnAddRoom_Click(object sender, EventArgs e)
        {
            int capacity;
            if (string.IsNullOrWhiteSpace(txtRoomNumber.Text) || !int.TryParse(txtCapacity.Text, out capacity) || capacity <= 0)
            {
                ShowAlert("Please enter a room number and a valid capacity.", "warning");
                return;
            }
            TimetableSetupHelper.SaveRoom(0, txtRoomNumber.Text.Trim(), ddlRoomType.SelectedValue, capacity, txtBuilding.Text.Trim(), true);
            txtRoomNumber.Text = txtCapacity.Text = txtBuilding.Text = "";
            ShowAlert("Room added.", "success");
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
