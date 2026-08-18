<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FeeReconciliation.aspx.cs" Inherits="StudentRegistrationSystem.FeeReconciliation" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Fee Reconciliation | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .card { border-radius: 16px; }
        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; }
        .status-Reconciled   { background: rgba(22,163,74,.15); color: #16a34a; }
        .status-Unreconciled { background: rgba(255,193,7,.2); color: #b8860b; }
        .status-Disputed     { background: rgba(220,53,69,.15); color: #dc3545; }
        .table td { vertical-align: middle; font-size: .88rem; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-shield-lock-fill me-2"></i>Admin Panel</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="AdminDashboard.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a>
                    <a href="BannerManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-images me-1"></i>Home Banners</a>
                    <a href="FeeStructureManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-cash-coin me-1"></i>Registration Fees</a>
                    <a href="StudentFeeDemand.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-receipt me-1"></i>Student Dues</a>
                    <a href="FeeReconciliation.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-arrow-left-right me-1"></i>Reconciliation</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h4 class="mb-0"><i class="bi bi-file-earmark-spreadsheet me-2"></i>Upload Bank / Gateway Statement</h4></div>
                <div class="card-body p-3 p-md-4">
                    <p class="text-muted small">CSV with three columns, no header: <code>Reference,Amount,Date</code> (date optional, <code>yyyy-MM-dd</code>). Each row is matched to a transaction by its Gateway Transaction ID or Bank Reference Number; exact amount matches auto-reconcile, mismatched amounts are flagged for review.</p>
                    <div class="d-flex gap-2 align-items-end flex-wrap">
                        <div>
                            <label class="form-label small fw-semibold">Source Label</label>
                            <asp:TextBox ID="txtSourceLabel" runat="server" CssClass="form-control form-control-sm" placeholder="e.g. HDFC Bank - Aug 2026" Width="220px" />
                        </div>
                        <div>
                            <label class="form-label small fw-semibold">Statement File (.csv)</label>
                            <asp:FileUpload ID="fuStatement" runat="server" CssClass="form-control form-control-sm" />
                        </div>
                        <asp:Button ID="btnUpload" runat="server" Text="Upload &amp; Match" CssClass="btn btn-primary btn-sm" OnClick="btnUpload_Click" />
                    </div>

                    <asp:Panel ID="pnlBatchResult" runat="server" Visible="false" CssClass="mt-3">
                        <div class="alert alert-info py-2 small mb-0">
                            Batch processed: <asp:Literal ID="litBatchSummary" runat="server" />
                        </div>
                    </asp:Panel>
                </div>
            </div>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
                    <h4 class="mb-0"><i class="bi bi-arrow-left-right me-2"></i>Transactions</h4>
                    <div class="d-flex gap-2">
                        <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged">
                            <asp:ListItem Text="All Statuses" Value="" />
                            <asp:ListItem Text="Unreconciled" Value="Unreconciled" />
                            <asp:ListItem Text="Reconciled" Value="Reconciled" />
                            <asp:ListItem Text="Disputed" Value="Disputed" />
                        </asp:DropDownList>
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search receipt, student, ref..." />
                        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                    </div>
                </div>
                <div class="card-body p-3 p-md-4">
                    <div class="table-responsive">
                        <asp:GridView ID="gvTransactions" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvTransactions_RowCommand" OnRowDataBound="gvTransactions_RowDataBound"
                            EmptyDataText="No transactions found.">
                            <Columns>
                                <asp:TemplateField HeaderText="Receipt #"><ItemTemplate><%# Eval("TransactionRef") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Student"><ItemTemplate><%# Eval("FullName") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Mode"><ItemTemplate><%# Eval("PaymentMode") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Amount" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("Amount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Date"><ItemTemplate><%# Eval("PaymentDate", "{0:dd-MMM-yyyy}") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Gateway/Bank Ref">
                                    <ItemTemplate><%# Eval("GatewayTransactionID") ?? Eval("BankReferenceNumber") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate><span class='badge-status status-<%# Eval("ReconciliationStatus") %>'><%# Eval("ReconciliationStatus") %></span></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="d-flex gap-1 justify-content-center">
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-success" ToolTip="Mark Reconciled"
                                                CommandName="Reconcile" CommandArgument='<%# Eval("TransactionID") %>'><i class="bi bi-check-lg"></i></asp:LinkButton>
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-danger" ToolTip="Mark Disputed"
                                                CommandName="Dispute" CommandArgument='<%# Eval("TransactionID") %>'><i class="bi bi-exclamation-triangle"></i></asp:LinkButton>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    </form>
</body>
</html>
