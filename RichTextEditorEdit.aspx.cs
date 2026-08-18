using System;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 10: Admin Panel &gt; Rich Text Editor &gt; create/edit page.
    /// Requirement 3: Document Title + Rich Text Content, both mandatory.
    /// Requirement 5: Save / Update / Reset / Preview actions.
    /// Requirement 9/10: server-side validation + HTML sanitization before persisting.
    /// </summary>
    public partial class RichTextEditorEdit : Page
    {
        private string CurrentAdminName
        {
            get { return Session["AdminName"] as string; }
        }

        private int DocumentId
        {
            get { return string.IsNullOrEmpty(hdnDocumentId.Value) ? 0 : Convert.ToInt32(hdnDocumentId.Value); }
        }

        private bool IsEditMode
        {
            get { return DocumentId > 0; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
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
                int requestedId;
                int.TryParse(Request.QueryString["id"], out requestedId);

                if (requestedId > 0)
                {
                    LoadDocument(requestedId);
                }
                else
                {
                    hdnDocumentId.Value = "0";
                    litPageTitle.Text = "New Document";
                    litBreadcrumb.Text = "New Document";
                    litFormHeading.Text = "Create Document";
                }
            }
        }

        private void LoadDocument(int documentId)
        {
            try
            {
                DataRow doc = RichTextDocumentHelper.GetDocumentById(documentId);
                if (doc == null)
                {
                    ShowAlert("The requested document could not be found. It may have been deleted.", "warning");
                    litPageTitle.Text = "New Document";
                    litBreadcrumb.Text = "New Document";
                    litFormHeading.Text = "Create Document";
                    hdnDocumentId.Value = "0";
                    return;
                }

                hdnDocumentId.Value = doc["DocumentID"].ToString();
                txtTitle.Text = doc["Title"].ToString();
                txtContent.Text = doc["ContentHtml"].ToString();
                ddlStatus.SelectedValue = doc["Status"].ToString();

                litPageTitle.Text = "Edit Document";
                litBreadcrumb.Text = "Edit Document";
                litFormHeading.Text = "Edit Document \u2014 " + doc["Title"];
            }
            catch (Exception ex)
            {
                AppLogger.Error("RichTextEditorEdit.LoadDocument", "Failed to load document " + documentId, ex);
                ShowAlert("Unable to load this document right now. Please try again shortly.", "danger");
            }
        }

        protected void cvContent_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string plainText = RichTextSanitizer.StripTags(txtContent.Text);
            args.IsValid = !string.IsNullOrWhiteSpace(plainText);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                string title = txtTitle.Text.Trim();
                // Requirement 6/9/10: sanitize on the way in — this is the authoritative pass;
                // never trust the client (browser extension, direct POST, tampered request, etc.).
                string sanitizedContent = RichTextSanitizer.Sanitize(txtContent.Text);
                string status = ddlStatus.SelectedValue;

                if (IsEditMode)
                {
                    if (!RichTextDocumentHelper.DocumentExists(DocumentId))
                    {
                        ShowAlert("This document no longer exists — it may have been deleted by another admin.", "warning");
                        return;
                    }

                    RichTextDocumentHelper.UpdateDocument(DocumentId, title, sanitizedContent, status, CurrentAdminName);
                    AppLogger.Info("RichTextEditorEdit", "Document " + DocumentId + " updated by " + CurrentAdminName);
                    Response.Redirect("RichTextEditor.aspx?msg=updated");
                }
                else
                {
                    int newId = RichTextDocumentHelper.InsertDocument(title, sanitizedContent, status, CurrentAdminName);
                    AppLogger.Info("RichTextEditorEdit", "Document " + newId + " created by " + CurrentAdminName);
                    Response.Redirect("RichTextEditor.aspx?msg=created");
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // Raised internally by Response.Redirect() above on success — must propagate
                // so the redirect actually completes; the CLR re-raises it automatically
                // after this catch block, so nothing further to do here.
                throw;
            }
            catch (Exception ex)
            {
                AppLogger.Error("RichTextEditorEdit.btnSave_Click", "Failed to save document.", ex);
                ShowAlert("Could not save the document due to a server error. Please try again.", "danger");
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            if (IsEditMode)
            {
                // Reset reloads the last saved version from the database, discarding edits.
                LoadDocument(DocumentId);
                ShowAlert("Changes reverted to the last saved version.", "info");
            }
            else
            {
                txtTitle.Text = string.Empty;
                txtContent.Text = string.Empty;
                ddlStatus.SelectedValue = "Published";
                ShowAlert("Form cleared.", "info");
            }
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
