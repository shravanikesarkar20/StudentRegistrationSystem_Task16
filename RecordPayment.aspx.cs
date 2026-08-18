using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>Task 12: Admin Panel &gt; Record Payment &mdash; captures one payment (online or
    /// offline) for a student and allocates it across their open fee heads/installments.</summary>
    public partial class RecordPayment : Page
    {
        private string CurrentAdminName { get { return Session["AdminName"] as string; } }

        private int? StudentId
        {
            get
            {
                int id;
                return int.TryParse(Request.QueryString["studentId"], out id) && id > 0 ? (int?)id : null;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (CurrentAdminName == null) { Response.Redirect("AdminLogin.aspx"); return; }
            litAdminName.Text = Server.HtmlEncode(CurrentAdminName);

            if (!StudentId.HasValue)
            {
                pnlNoStudent.Visible = true;
                pnlForm.Visible = false;
                return;
            }

            if (!IsPostBack)
            {
                var student = RegistrationFeeHelper.GetStudentById(StudentId.Value);
                if (student == null) { pnlNoStudent.Visible = true; pnlForm.Visible = false; return; }

                litStudentName.Text = Server.HtmlEncode(student["FullName"] + " (ID " + StudentId.Value + ")");
                LoadTotals();
                TogglePaymentModeFields();
            }
        }

        private void LoadTotals()
        {
            RegistrationFeeHelper.FeeSummary summary = RegistrationFeeHelper.GetStudentFeeSummary(StudentId.Value);
            litOutstanding.Text = summary.OutstandingAmount.ToString("N2");
            litNetPayable.Text = summary.NetPayable.ToString("N2");
        }

        protected void ddlPaymentMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            TogglePaymentModeFields();
        }

        private void TogglePaymentModeFields()
        {
            bool isOnline = ddlPaymentMode.SelectedValue == "Online" || ddlPaymentMode.SelectedValue == "UPI";
            pnlOnlineFields.Visible = isOnline;
            pnlOfflineFields.Visible = !isOnline;
        }

        protected void btnRecord_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !StudentId.HasValue) return;

            try
            {
                decimal amount = decimal.Parse(txtAmount.Text, CultureInfo.InvariantCulture);
                bool isOnline = ddlPaymentMode.SelectedValue == "Online" || ddlPaymentMode.SelectedValue == "UPI";

                RegistrationFeeHelper.PaymentResult result = RegistrationFeeHelper.RecordPayment(
                    StudentId.Value, amount, ddlPaymentMode.SelectedValue,
                    isOnline ? txtGatewayName.Text.Trim() : null,
                    isOnline ? txtGatewayTxnId.Text.Trim() : null,
                    !isOnline ? txtBankRef.Text.Trim() : null,
                    !isOnline ? txtChequeNo.Text.Trim() : null,
                    txtRemarks.Text.Trim(), CurrentAdminName);

                AppLogger.Info("RecordPayment", string.Format(
                    "Payment {0} ({1}) recorded for StudentID {2} by {3}. Allocated {4}, unallocated {5}.",
                    result.TransactionRef, amount, StudentId.Value, CurrentAdminName, result.AmountAllocated, result.AmountUnallocated));

                litReceiptNo.Text = result.TransactionRef;
                pnlAllocationResult.Visible = true;
                gvAllocations.DataSource = RegistrationFeeHelper.GetTransactionAllocations(result.TransactionID);
                gvAllocations.DataBind();

                string msg = string.Format("Payment recorded successfully. Receipt: {0}. Allocated &#8377;{1:N2}.",
                    result.TransactionRef, result.AmountAllocated);
                if (result.AmountUnallocated > 0)
                    msg += string.Format(" &#8377;{0:N2} could not be allocated (no outstanding dues) — recorded as advance credit for follow-up.", result.AmountUnallocated);
                ShowAlert(msg, "success", allowHtml: true);

                LoadTotals();
                txtAmount.Text = string.Empty;
            }
            catch (ArgumentException ex)
            {
                ShowAlert(ex.Message, "danger");
            }
            catch (Exception ex)
            {
                AppLogger.Error("RecordPayment.btnRecord_Click", "Failed to record payment.", ex);
                ShowAlert("Could not record the payment due to a server error. No money has been recorded — please retry.", "danger");
            }
        }

        private void ShowAlert(string message, string type, bool allowHtml = false)
        {
            pnlAlert.CssClass = "alert py-2 small alert-" + type;
            pnlAlert.Controls.Clear();
            pnlAlert.Controls.Add(new LiteralControl(allowHtml ? message : Server.HtmlEncode(message)));
        }
    }
}
