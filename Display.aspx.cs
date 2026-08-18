using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    public partial class Display : Page
    {
        private const string BASE_QUERY = @"
            SELECT
                s.StudentID,
                s.FullName,
                s.Email,
                s.Mobile,
                s.Gender,
                s.PhotoPath,
                s.IsEmailVerified,
                s.RegistrationDate,
                (d.DistrictName + ', ' + st.StateName + ', ' + c.CountryName) AS Location
            FROM Students s
            INNER JOIN Districts d ON s.DistrictID = d.DistrictID
            INNER JOIN States st   ON s.StateID    = st.StateID
            INNER JOIN Countries c ON s.CountryID  = c.CountryID";

        // Whitelist of columns that may be used in ORDER BY — never build ORDER BY from raw user input.
        private static readonly System.Collections.Generic.Dictionary<string, string> SORTABLE_COLUMNS =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "FullName", "s.FullName" },
            { "RegistrationDate", "s.RegistrationDate" },
            { "Email", "s.Email" }
        };

        private string SortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "RegistrationDate"; }
            set { ViewState["SortExpression"] = value; }
        }

        private string SortDirection
        {
            get { return ViewState["SortDirection"] as string ?? "DESC"; }
            set { ViewState["SortDirection"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            string name = txtSearchName.Text.Trim();
            string email = txtSearchEmail.Text.Trim();
            string mobile = txtSearchMobile.Text.Trim();
            string gender = ddlFilterGender.SelectedValue;

            var whereClauses = new System.Collections.Generic.List<string>();
            var parameters = new System.Collections.Generic.List<SqlParameter>();

            if (!string.IsNullOrEmpty(name))
            {
                whereClauses.Add("s.FullName LIKE @Name");
                parameters.Add(new SqlParameter("@Name", "%" + name + "%"));
            }
            if (!string.IsNullOrEmpty(email))
            {
                whereClauses.Add("s.Email LIKE @Email");
                parameters.Add(new SqlParameter("@Email", "%" + email + "%"));
            }
            if (!string.IsNullOrEmpty(mobile))
            {
                whereClauses.Add("s.Mobile LIKE @Mobile");
                parameters.Add(new SqlParameter("@Mobile", "%" + mobile + "%"));
            }
            if (!string.IsNullOrEmpty(gender))
            {
                whereClauses.Add("s.Gender = @Gender");
                parameters.Add(new SqlParameter("@Gender", gender));
            }

            string sql = BASE_QUERY;
            if (whereClauses.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", whereClauses);
            }

            string sortColumn = SORTABLE_COLUMNS.ContainsKey(SortExpression) ? SORTABLE_COLUMNS[SortExpression] : "s.RegistrationDate";
            string direction = string.Equals(SortDirection, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            sql += " ORDER BY " + sortColumn + " " + direction;

            DataTable dt = DBHelper.ExecuteQuery(sql, parameters.ToArray());

            gvStudents.DataSource = dt;
            gvStudents.DataBind();

            lblRecordCount.Text = dt.Rows.Count + " student record(s) found.";
        }

        protected void gvStudents_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (!SORTABLE_COLUMNS.ContainsKey(e.SortExpression)) return;

            if (string.Equals(SortExpression, e.SortExpression, StringComparison.OrdinalIgnoreCase))
            {
                // Same column clicked again — toggle direction.
                SortDirection = string.Equals(SortDirection, "ASC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            }
            else
            {
                SortExpression = e.SortExpression;
                SortDirection = "ASC";
            }

            BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindGrid();
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            txtSearchName.Text = "";
            txtSearchEmail.Text = "";
            txtSearchMobile.Text = "";
            ddlFilterGender.SelectedIndex = 0;
            SortExpression = "RegistrationDate";
            SortDirection = "DESC";
            BindGrid();
        }

        #region ---- Export to Excel ----

        protected void btnExport_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=StudentRecords_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.Charset = "";
            Response.ContentType = "application/vnd.ms-excel";

            // GridView must not be embedded in an UpdatePanel for this simple export approach.
            gvStudents.AllowPaging = false;
            BindGrid();

            // Hide the "Photo" / "Verified" template columns visually is optional;
            // here we export all visible columns as-is inside a clean HTML table.
            using (StringWriter sw = new StringWriter())
            using (HtmlTextWriter hw = new HtmlTextWriter(sw))
            {
                gvStudents.HeaderRow.BackColor = System.Drawing.Color.FromName("#0d6efd");
                foreach (TableCell cell in gvStudents.HeaderRow.Cells)
                {
                    cell.CssClass = "excel-header";
                }

                gvStudents.RenderControl(hw);

                Response.Write("<html><head><meta charset='utf-8'>");
                Response.Write("<style>.excel-header{background:#0d6efd;color:#fff;font-weight:bold;} table{border-collapse:collapse;} td,th{border:1px solid #ccc;padding:6px;}</style>");
                Response.Write("</head><body>");
                Response.Write(sw.ToString());
                Response.Write("</body></html>");
            }

            Response.End();
        }

        /// <summary>
        /// Required override so GridView.RenderControl works outside of a Page.Render context
        /// (standard workaround for exporting server controls to Excel/CSV in Web Forms).
        /// </summary>
        public override void VerifyRenderingInServerForm(Control control)
        {
            // Intentionally left blank to bypass the ASP.NET server-form verification
            // that normally blocks RenderControl() calls made outside Page.Render().
        }

        #endregion
    }
}
