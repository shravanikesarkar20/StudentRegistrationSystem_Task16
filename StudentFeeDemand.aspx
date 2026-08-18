<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentFeeDemand.aspx.cs" Inherits="StudentRegistrationSystem.StudentFeeDemand" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Dues | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .card { border-radius: 16px; }
        .summary-tile { background: #fff; border: 1px solid #e6ece9; border-radius: 12px; padding: 14px 16px; height: 100%; }
        .summary-tile .label { font-size: .74rem; text-transform: uppercase; letter-spacing: .5px; color: #7c8b88; font-weight: 700; }
        .summary-tile .value { font-size: 1.3rem; font-weight: 700; color: #1f2d2b; }
        .summary-tile.outstanding .value { color: #dc3545; }
        .summary-tile.paid .value { color: #16a34a; }
        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; }
        .status-Paid { background: rgba(22,163,74,.15); color: #16a34a; }
        .status-PartiallyPaid { background: rgba(255,193,7,.2); color: #b8860b; }
        .status-Pending { background: rgba(13,110,253,.15); color: #0d6efd; }
        .status-Overdue { background: rgba(220,53,69,.15); color: #dc3545; }
        .status-Waived { background: rgba(108,117,125,.16); color: #6c757d; }
        .table td { vertical-align: middle; font-size: .9rem; }
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
                    <a href="StudentFeeDemand.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-receipt me-1"></i>Student Dues</a>
                    <a href="FeeReconciliation.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-arrow-left-right me-1"></i>Reconciliation</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h4 class="mb-0"><i class="bi bi-search me-2"></i>Find Student</h4></div>
                <div class="card-body p-3 p-md-4">
                    <div class="d-flex gap-2 flex-wrap">
                        <asp:TextBox ID="txtStudentSearch" runat="server" CssClass="form-control" placeholder="Search by name, email, mobile, or Student ID" Width="320px" />
                        <asp:Button ID="btnFindStudent" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnFindStudent_Click" />
                    </div>
                    <asp:GridView ID="gvStudentResults" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mt-3"
                        GridLines="None" OnRowCommand="gvStudentResults_RowCommand" EmptyDataText="No matching students. Try a different search.">
                        <Columns>
                            <asp:TemplateField HeaderText="ID"><ItemTemplate><%# Eval("StudentID") %></ItemTemplate></asp:TemplateField>
                            <asp:TemplateField HeaderText="Name"><ItemTemplate><%# Eval("FullName") %></ItemTemplate></asp:TemplateField>
                            <asp:TemplateField HeaderText="Email"><ItemTemplate><%# Eval("Email") %></ItemTemplate></asp:TemplateField>
                            <asp:TemplateField HeaderText="Mobile"><ItemTemplate><%# Eval("Mobile") %></ItemTemplate></asp:TemplateField>
                            <asp:TemplateField HeaderText="">
                                <ItemTemplate>
                                    <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-primary" CommandName="Select" CommandArgument='<%# Eval("StudentID") %>'>Select</asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <asp:Panel ID="pnlStudent" runat="server" Visible="false">
                <asp:HiddenField ID="hdnStudentId" runat="server" />

                <div class="card shadow-sm border-0 mb-4">
                    <div class="card-header card-header-gradient py-3 d-flex justify-content-between align-items-center">
                        <h4 class="mb-0"><i class="bi bi-person-badge me-2"></i><asp:Literal ID="litStudentHeader" runat="server" /></h4>
                        <a href="#" id="lnkChangeStudent" class="btn btn-light btn-sm" onclick="document.getElementById('<%= hdnClear.ClientID %>').value='1'; document.forms[0].submit(); return false;">Change Student</a>
                        <asp:HiddenField ID="hdnClear" runat="server" />
                    </div>
                    <div class="card-body p-3 p-md-4">

                        <h6 class="fw-bold text-primary mb-3">Academic Profile</h6>
                        <div class="row g-3 mb-3">
                            <div class="col-md-2">
                                <label class="form-label small fw-semibold">Academic Year</label>
                                <asp:DropDownList ID="ddlAcademicYear" runat="server" CssClass="form-select form-select-sm" />
                            </div>
                            <div class="col-md-3">
                                <label class="form-label small fw-semibold">Institute</label>
                                <asp:DropDownList ID="ddlInstitute" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlInstitute_SelectedIndexChanged" />
                            </div>
                            <div class="col-md-3">
                                <label class="form-label small fw-semibold">Course</label>
                                <asp:DropDownList ID="ddlCourse" runat="server" CssClass="form-select form-select-sm" />
                            </div>
                            <div class="col-md-2">
                                <label class="form-label small fw-semibold">Year/Sem</label>
                                <asp:DropDownList ID="ddlYearSemester" runat="server" CssClass="form-select form-select-sm" />
                            </div>
                            <div class="col-md-2">
                                <label class="form-label small fw-semibold">Category</label>
                                <asp:DropDownList ID="ddlStudentCategory" runat="server" CssClass="form-select form-select-sm" />
                            </div>
                        </div>
                        <div class="d-flex gap-2 mb-4">
                            <asp:Button ID="btnSaveProfile" runat="server" Text="Save Academic Profile" CssClass="btn btn-outline-primary btn-sm" OnClick="btnSaveProfile_Click" />
                            <asp:Button ID="btnGenerateDemand" runat="server" Text="Generate / Refresh Fee Demand" CssClass="btn btn-primary btn-sm" OnClick="btnGenerateDemand_Click" />
                            <a id="lnkRecordPayment" runat="server" class="btn btn-success btn-sm"><i class="bi bi-cash-stack me-1"></i>Record Payment</a>
                        </div>

                        <asp:Panel ID="pnlSummary" runat="server" Visible="false">
                            <h6 class="fw-bold text-primary mb-3">Fee Summary</h6>
                            <div class="row g-3 mb-4">
                                <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Total Payable</div><div class="value">&#8377;<asp:Literal ID="litTotalPayable" runat="server" /></div></div></div>
                                <div class="col-6 col-md-2"><div class="summary-tile paid"><div class="label">Amount Paid</div><div class="value">&#8377;<asp:Literal ID="litAmountPaid" runat="server" /></div></div></div>
                                <div class="col-6 col-md-2"><div class="summary-tile outstanding"><div class="label">Outstanding</div><div class="value">&#8377;<asp:Literal ID="litOutstanding" runat="server" /></div></div></div>
                                <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Late Fee</div><div class="value">&#8377;<asp:Literal ID="litLateFee" runat="server" /></div></div></div>
                                <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Discount</div><div class="value">&#8377;<asp:Literal ID="litDiscount" runat="server" /></div></div></div>
                                <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Net Payable</div><div class="value">&#8377;<asp:Literal ID="litNetPayable" runat="server" /></div></div></div>
                            </div>
                            <p><span class="label text-muted small">Payment Status:</span> <asp:Literal ID="litPaymentStatus" runat="server" /></p>

                            <h6 class="fw-bold text-primary mb-3 mt-4">Fee Heads</h6>
                            <div class="table-responsive">
                                <asp:GridView ID="gvFeeHeads" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                                    GridLines="None" OnRowCommand="gvFeeHeads_RowCommand" EmptyDataText="No fee demand generated yet.">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Fee Type"><ItemTemplate><%# Eval("FeeTypeName") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Gross" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("GrossAmount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Discount" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("DiscountAmount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Paid" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("AmountPaid", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Late Fee (live)" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("LiveLateFeeOutstanding", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Due Date"><ItemTemplate><%# Eval("DueDate", "{0:dd-MMM-yyyy}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Status">
                                            <ItemTemplate><span class='badge-status status-<%# Eval("Status") %>'><%# Eval("Status") %></span></ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Discount / Scholarship">
                                            <ItemTemplate>
                                                <div class="d-flex gap-1">
                                                    <asp:TextBox ID="txtDiscountAmt" runat="server" CssClass="form-control form-control-sm" Width="90px" TextMode="Number" step="0.01" />
                                                    <asp:TextBox ID="txtDiscountReason" runat="server" CssClass="form-control form-control-sm" Width="120px" placeholder="Reason" />
                                                    <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-primary" CommandName="ApplyDiscount" CommandArgument='<%# Eval("FeeDemandID") %>'>Apply</asp:LinkButton>
                                                </div>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </div>

                            <h6 class="fw-bold text-primary mb-3 mt-4">Payment History</h6>
                            <div class="table-responsive">
                                <asp:GridView ID="gvTransactions" runat="server" AutoGenerateColumns="false" CssClass="table table-sm table-hover align-middle"
                                    GridLines="None" EmptyDataText="No payments recorded yet.">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Receipt #"><ItemTemplate><%# Eval("TransactionRef") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Mode"><ItemTemplate><%# Eval("PaymentMode") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Amount" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("Amount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Date"><ItemTemplate><%# Eval("PaymentDate", "{0:dd-MMM-yyyy hh:mm tt}") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Status"><ItemTemplate><%# Eval("Status") %></ItemTemplate></asp:TemplateField>
                                        <asp:TemplateField HeaderText="Reconciliation"><ItemTemplate><%# Eval("ReconciliationStatus") %></ItemTemplate></asp:TemplateField>
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
