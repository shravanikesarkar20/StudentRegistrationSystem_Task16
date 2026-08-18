using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 10: Admin Panel &gt; Rich Text Editor &gt; document listing page.
    /// Requirement 7: search, sort, and pagination over saved documents.
    /// Requirement 8: view/preview a document exactly as it will render.
    /// </summary>
    public partial class RichTextEditor : Page
    {
        private const int PageSize = 10;

        private string CurrentAdminName
        {
            get { return Session["AdminName"] as string; }
        }

        private string SearchTerm
        {
            get { return (ViewState["RTE_Search"] as string) ?? string.Empty; }
            set { ViewState["RTE_Search"] = value; }
        }

        private string SortColumn
        {
            get { return (ViewState["RTE_SortCol"] as string) ?? "ModifiedDate"; }
            set { ViewState["RTE_SortCol"] = value; }
        }

        private string SortDirection
        {
            get { return (ViewState["RTE_SortDir"] as string) ?? "DESC"; }
            set { ViewState["RTE_SortDir"] = value; }
        }

        private int PageIndex
        {
            get { return ViewState["RTE_PageIndex"] == null ? 0 : (int)ViewState["RTE_PageIndex"]; }
            set { ViewState["RTE_PageIndex"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Requirement 1/protected page: only authenticated administrators may reach this page.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

            if (CurrentAdminName == null)
            {
                Response.Redirect("AdminLogin.aspx");
                return;
            }

            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack)
            {
                txtSearch.Text = string.Empty;
                BindGrid();

                switch (Request.QueryString["msg"])
                {
                    case "created":
                        ShowAlert("Document created successfully.", "success");
                        break;
                    case "updated":
                        ShowAlert("Document updated successfully.", "success");
                        break;
                }
            }
        }

        private void BindGrid()
        {
            try
            {
                int totalCount;
                DataTable dt = RichTextDocumentHelper.GetDocuments(
                    SearchTerm, SortColumn, SortDirection, PageIndex, PageSize, out totalCount);

                gvDocuments.DataSource = dt;
                gvDocuments.DataBind();

                int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);
                if (PageIndex >= totalPages) PageIndex = Math.Max(0, totalPages - 1);

                litResultCount.Text = totalCount == 0
                    ? "No documents found."
                    : string.Format("{0} document{1} found.", totalCount, totalCount == 1 ? "" : "s");

                litPageInfo.Text = string.Format("Page {0} of {1}", PageIndex + 1, totalPages);

                btnFirst.Enabled = btnPrev.Enabled = PageIndex > 0;
                btnNext.Enabled = btnLast.Enabled = PageIndex < totalPages - 1;
            }
            catch (Exception ex)
            {
                AppLogger.Error("RichTextEditor.BindGrid", "Failed to load document list.", ex);
                ShowAlert("Unable to load documents right now. Please try again shortly.", "danger");
            }
        }

        protected void gvDocuments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            DataRowView row = (DataRowView)e.Row.DataItem;
            string status = row["Status"].ToString();

            Literal litBadge = (Literal)e.Row.FindControl("litStatusBadge");
            if (litBadge != null)
            {
                string cssClass = status == "Draft" ? "badge-draft" : "badge-published";
                litBadge.Text = string.Format("<span class='badge-status {0}'>{1}</span>", cssClass, Server.HtmlEncode(status));
            }
        }

        protected void gvDocuments_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (SortColumn == e.SortExpression)
            {
                SortDirection = (SortDirection == "ASC") ? "DESC" : "ASC";
            }
            else
            {
                SortColumn = e.SortExpression;
                SortDirection = "ASC";
            }
            PageIndex = 0;
            BindGrid();
        }

        protected void gvDocuments_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int documentId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out documentId)) return;

            if (e.CommandName == "DeleteDoc")
            {
                try
                {
                    int rows = RichTextDocumentHelper.DeleteDocument(documentId);
                    if (rows > 0)
                    {
                        AppLogger.Info("RichTextEditor", "Document " + documentId + " deleted by " + CurrentAdminName);
                        ShowAlert("Document deleted successfully.", "success");
                    }
                    else
                    {
                        ShowAlert("Document was not found — it may have already been deleted.", "warning");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("RichTextEditor", "Delete failed for document " + documentId, ex);
                    ShowAlert("Could not delete the document due to a server error.", "danger");
                }
                BindGrid();
            }
            else if (e.CommandName == "ViewDoc")
            {
                try
                {
                    DataRow doc = RichTextDocumentHelper.GetDocumentById(documentId);
                    if (doc == null)
                    {
                        ShowAlert("Document was not found.", "warning");
                        return;
                    }

                    litViewTitle.Text = Server.HtmlEncode(doc["Title"].ToString());
                    // Re-sanitize on the way out too, in case content was ever written by another path.
                    litViewContent.Text = RichTextSanitizer.Sanitize(doc["ContentHtml"].ToString());
                    hdnOpenViewModal.Value = "1";
                }
                catch (Exception ex)
                {
                    AppLogger.Error("RichTextEditor", "View failed for document " + documentId, ex);
                    ShowAlert("Could not load the document preview.", "danger");
                }
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchTerm = txtSearch.Text.Trim();
            PageIndex = 0;
            BindGrid();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            SearchTerm = string.Empty;
            PageIndex = 0;
            BindGrid();
        }

        protected void btnFirst_Click(object sender, EventArgs e) { PageIndex = 0; BindGrid(); }
        protected void btnPrev_Click(object sender, EventArgs e) { PageIndex = Math.Max(0, PageIndex - 1); BindGrid(); }
        protected void btnNext_Click(object sender, EventArgs e) { PageIndex = PageIndex + 1; BindGrid(); }
        protected void btnLast_Click(object sender, EventArgs e)
        {
            int totalCount;
            RichTextDocumentHelper.GetDocuments(SearchTerm, SortColumn, SortDirection, 0, PageSize, out totalCount);
            int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);
            PageIndex = Math.Max(0, totalPages - 1);
            BindGrid();
        }

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
