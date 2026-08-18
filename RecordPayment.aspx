<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RecordPayment.aspx.cs" Inherits="StudentRegistrationSystem.RecordPayment" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Record Payment | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .card { border-radius: 16px; }
        .form-label { font-weight: 600; font-size: .88rem; color: #34474a; }
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
                    <a href="FeeReconciliation.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-arrow-left-right me-1"></i>Reconciliation</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <asp:Panel ID="pnlNoStudent" runat="server" Visible="false">
                <div class="alert alert-warning">No student selected. <a href="StudentFeeDemand.aspx">Go to Student Dues</a> and select a student first.</div>
            </asp:Panel>

            <asp:Panel ID="pnlForm" runat="server">
                <div class="card shadow-sm border-0">
                    <div class="card-header card-header-gradient py-3">
                        <h4 class="mb-0"><i class="bi bi-cash-stack me-2"></i>Record Payment &mdash; <asp:Literal ID="litStudentName" runat="server" /></h4>
                    </div>
                    <div class="card-body p-3 p-md-4">

                        <div class="row g-3 mb-4">
                            <div class="col-6 col-md-3">
                                <div class="p-3 border rounded-3 bg-white"><div class="text-muted small">Outstanding</div><div class="fw-bold">&#8377;<asp:Literal ID="litOutstanding" runat="server" /></div></div>
                            </div>
                            <div class="col-6 col-md-3">
                                <div class="p-3 border rounded-3 bg-white"><div class="text-muted small">Net Payable</div><div class="fw-bold">&#8377;<asp:Literal ID="litNetPayable" runat="server" /></div></div>
                            </div>
                        </div>

                        <div class="row g-3 mb-3">
                            <div class="col-md-3">
                                <label class="form-label">Amount (&#8377;)</label>
                                <asp:TextBox ID="txtAmount" runat="server" CssClass="form-control" TextMode="Number" step="0.01" />
                                <asp:RangeValidator ID="rvAmount" runat="server" ControlToValidate="txtAmount" Type="Currency"
                                    MinimumValue="0.01" MaximumValue="99999999" ErrorMessage="Enter a valid amount greater than 0." CssClass="text-danger small" Display="Dynamic" />
                            </div>
                            <div class="col-md-3">
                                <label class="form-label">Payment Mode</label>
                                <asp:DropDownList ID="ddlPaymentMode" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlPaymentMode_SelectedIndexChanged">
                                    <asp:ListItem Text="Online (Gateway)" Value="Online" />
                                    <asp:ListItem Text="UPI" Value="UPI" />
                                    <asp:ListItem Text="Cash" Value="Cash" />
                                    <asp:ListItem Text="Cheque" Value="Cheque" />
                                    <asp:ListItem Text="Demand Draft" Value="DD" />
                                    <asp:ListItem Text="Bank Transfer" Value="BankTransfer" />
                                </asp:DropDownList>
                            </div>
                            <asp:Panel ID="pnlOnlineFields" runat="server" CssClass="col-md-6 d-flex gap-2">
                                <div class="flex-fill">
                                    <label class="form-label">Gateway Name</label>
                                    <asp:TextBox ID="txtGatewayName" runat="server" CssClass="form-control" placeholder="e.g. Razorpay, PayU" />
                                </div>
                                <div class="flex-fill">
                                    <label class="form-label">Gateway Transaction ID</label>
                                    <asp:TextBox ID="txtGatewayTxnId" runat="server" CssClass="form-control" />
                                </div>
                            </asp:Panel>
                            <asp:Panel ID="pnlOfflineFields" runat="server" CssClass="col-md-6 d-flex gap-2">
                                <div class="flex-fill">
                                    <label class="form-label">Bank Reference No.</label>
                                    <asp:TextBox ID="txtBankRef" runat="server" CssClass="form-control" />
                                </div>
                                <div class="flex-fill">
                                    <label class="form-label">Cheque / DD No.</label>
                                    <asp:TextBox ID="txtChequeNo" runat="server" CssClass="form-control" />
                                </div>
                            </asp:Panel>
                        </div>

                        <div class="row g-3 mb-4">
                            <div class="col-md-8">
                                <label class="form-label">Remarks</label>
                                <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control" />
                            </div>
                        </div>

                        <div class="d-flex gap-2">
                            <asp:Button ID="btnRecord" runat="server" Text="Record Payment" CssClass="btn btn-success" OnClick="btnRecord_Click" />
                            <a href="StudentFeeDemand.aspx" class="btn btn-outline-secondary">Back to Student Dues</a>
                        </div>

                        <asp:Panel ID="pnlAllocationResult" runat="server" Visible="false" CssClass="mt-4">
                            <h6 class="fw-bold text-primary">Allocation Breakdown &mdash; <asp:Literal ID="litReceiptNo" runat="server" /></h6>
                            <div class="table-responsive">
                                <asp:GridView ID="gvAllocations" runat="server" AutoGenerateColumns="false" CssClass="table table-sm table-hover" GridLines="None">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Fee Type"><ItemTemplate><%# Eval("FeeTypeName") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Installment"><ItemTemplate><%# Eval("InstallmentNo") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Type"><ItemTemplate><%# Eval("AllocationType") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Amount" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("AllocatedAmount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </asp:Panel>
                    </div>
                </div>
            </asp:Panel>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    </form>
</body>
</html>
