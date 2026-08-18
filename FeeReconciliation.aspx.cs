using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 12: Admin Panel &gt; Fee Reconciliation &mdash; manually mark transactions
    /// Reconciled/Disputed, or upload a bank/gateway statement CSV to auto-match by reference.</summary>
    public partial class FeeReconciliation : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private string StatusFilter
        {
            get { return (ViewState["FR_Status"] as string) ?? string.Empty; }
            set { ViewState["FR_Status"] = value; }
        }

        private string SearchTerm
        {
            get { return (ViewState["FR_Search"] as string) ?? string.Empty; }
            set { ViewState["FR_Search"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            try
            {
                DataTable dt = RegistrationFeeHelper.GetTransactionsForReconciliation(StatusFilter, SearchTerm);
                gvTransactions.DataSource = dt;
                gvTransactions.DataBind();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FeeReconciliation.BindGrid", "Failed to load transactions.", ex);
                ShowAlert("Unable to load transactions right now.", "danger");
            }
        }

        protected void gvTransactions_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Status badge is rendered directly via the inline <%# %> expression in markup.
        }

        protected void gvTransactions_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int transactionId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out transactionId)) return;

            try
            {
                if (e.CommandName == "Reconcile")
                {
                    RegistrationFeeHelper.ReconcileTransaction(transactionId, "Reconciled", CurrentAdminName);
                    ShowAlert("Transaction marked Reconciled.", "success");
                }
                else if (e.CommandName == "Dispute")
                {
                    RegistrationFeeHelper.ReconcileTransaction(transactionId, "Disputed", CurrentAdminName);
                    ShowAlert("Transaction marked Disputed.", "warning");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FeeReconciliation.gvTransactions_RowCommand", "Command " + e.CommandName + " failed for id " + transactionId, ex);
                ShowAlert("The action could not be completed due to a server error.", "danger");
            }

            BindGrid();
        }

        protected void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            StatusFilter = ddlStatusFilter.SelectedValue;
            BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchTerm = txtSearch.Text.Trim();
            BindGrid();
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (!fuStatement.HasFile)
            {
                ShowAlert("Choose a CSV file to upload first.", "warning");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSourceLabel.Text))
            {
                ShowAlert("Enter a source label (e.g. the bank/gateway name and period) for this batch.", "warning");
                return;
            }

            string extension = Path.GetExtension(fuStatement.FileName).ToLowerInvariant();
            if (extension != ".csv")
            {
                ShowAlert("Only .csv files are accepted for statement matching.", "warning");
                return;
            }

            try
            {
                List<Tuple<string, decimal, DateTime?>> rows = ParseCsv(fuStatement.PostedFile.InputStream);
                if (rows.Count == 0)
                {
                    ShowAlert("The uploaded file has no valid rows to match.", "warning");
                    return;
                }

                RegistrationFeeHelper.ReconciliationBatchResult result = RegistrationFeeHelper.MatchBankStatement(
                    txtSourceLabel.Text.Trim(), fuStatement.FileName, rows, CurrentAdminName);

                pnlBatchResult.Visible = true;
                litBatchSummary.Text = string.Format("{0} record(s) processed &mdash; {1} matched &amp; reconciled, {2} unmatched or mismatched.",
                    result.TotalRecords, result.MatchedRecords, result.UnmatchedRecords);

                AppLogger.Info("FeeReconciliation", string.Format("Batch {0} uploaded by {1}: {2} matched / {3} unmatched.",
                    result.BatchID, CurrentAdminName, result.MatchedRecords, result.UnmatchedRecords));

                ShowAlert("Statement processed successfully.", "success");
            }
            catch (Exception ex)
            {
                AppLogger.Error("FeeReconciliation.btnUpload_Click", "Failed to process statement upload.", ex);
                ShowAlert("Could not process the statement due to a server error. Check the file format and try again.", "danger");
            }

            BindGrid();
        }

        /// <summary>Parses a simple headerless CSV: Reference,Amount,Date(optional). Blank or
        /// malformed rows are skipped rather than aborting the whole batch.</summary>
        private static List<Tuple<string, decimal, DateTime?>> ParseCsv(Stream stream)
        {
            List<Tuple<string, decimal, DateTime?>> rows = new List<Tuple<string, decimal, DateTime?>>();
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    string reference = parts[0].Trim().Trim('"');
                    decimal amount;
                    if (string.IsNullOrEmpty(reference) || !decimal.TryParse(parts[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                        continue;

                    DateTime? date = null;
                    if (parts.Length >= 3)
                    {
                        DateTime parsed;
                        if (DateTime.TryParse(parts[2].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                            date = parsed;
                    }

                    rows.Add(Tuple.Create(reference, amount, date));
                }
            }
            return rows;
        }

        private void ShowAlert(string message, string type)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(message));
        }
    }
}
