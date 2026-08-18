<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FeeStructureEdit.aspx.cs" Inherits="StudentRegistrationSystem.FeeStructureEdit" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Configure Fee Structure | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .form-label { font-weight: 600; font-size: .88rem; color: #34474a; }
        .card { border-radius: 16px; }
        .installment-row { background: #f8fafb; border: 1px solid #e6ece9; border-radius: 10px; padding: .6rem .8rem; margin-bottom: .5rem; }
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
                    <a href="FeeStructureManagement.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-cash-coin me-1"></i>Registration Fees</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>
            <asp:HiddenField ID="hdnFeeStructureId" runat="server" />

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3">
                    <h4 class="mb-0"><i class="bi bi-sliders me-2"></i><asp:Literal ID="litPageTitle" runat="server" Text="New Fee Structure" /></h4>
                </div>
                <div class="card-body p-3 p-md-4">

                    <h6 class="fw-bold text-primary mb-3">Configuration Axes</h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-4">
                            <label class="form-label">Academic Year</label>
                            <asp:DropDownList ID="ddlAcademicYear" runat="server" CssClass="form-select" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Institute</label>
                            <asp:DropDownList ID="ddlInstitute" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlInstitute_SelectedIndexChanged" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Course / Program</label>
                            <asp:DropDownList ID="ddlCourse" runat="server" CssClass="form-select" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Year / Semester</label>
                            <asp:DropDownList ID="ddlYearSemester" runat="server" CssClass="form-select" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Applicable Student Category</label>
                            <asp:DropDownList ID="ddlStudentCategory" runat="server" CssClass="form-select" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Fee Type</label>
                            <asp:DropDownList ID="ddlFeeType" runat="server" CssClass="form-select" />
                        </div>
                    </div>

                    <h6 class="fw-bold text-primary mb-3">Amount &amp; Due Date</h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-4">
                            <label class="form-label">Fee Amount (&#8377;)</label>
                            <asp:TextBox ID="txtFeeAmount" runat="server" CssClass="form-control" TextMode="Number" step="0.01" />
                            <asp:RangeValidator ID="rvAmount" runat="server" ControlToValidate="txtFeeAmount" Type="Currency"
                                MinimumValue="0.01" MaximumValue="99999999" ErrorMessage="Enter a valid amount greater than 0." CssClass="text-danger small" Display="Dynamic" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Due Date</label>
                            <asp:TextBox ID="txtDueDate" runat="server" CssClass="form-control" TextMode="Date" />
                            <asp:RequiredFieldValidator ID="rfvDueDate" runat="server" ControlToValidate="txtDueDate" ErrorMessage="Due date is required." CssClass="text-danger small" Display="Dynamic" />
                        </div>
                        <div class="col-md-4 d-flex align-items-end">
                            <div class="form-check">
                                <asp:CheckBox ID="chkActive" runat="server" CssClass="form-check-input" Checked="true" />
                                <label class="form-check-label fw-semibold">Active</label>
                            </div>
                        </div>
                    </div>

                    <h6 class="fw-bold text-primary mb-3">Installment Details</h6>
                    <div class="row g-3 mb-2">
                        <div class="col-md-3">
                            <div class="form-check mt-4">
                                <asp:CheckBox ID="chkInstallmentsAllowed" runat="server" CssClass="form-check-input" AutoPostBack="true" OnCheckedChanged="chkInstallmentsAllowed_CheckedChanged" />
                                <label class="form-check-label fw-semibold">Allow Installments</label>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Number of Installments</label>
                            <asp:DropDownList ID="ddlNumberOfInstallments" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlNumberOfInstallments_SelectedIndexChanged">
                                <asp:ListItem Text="2" Value="2" /><asp:ListItem Text="3" Value="3" /><asp:ListItem Text="4" Value="4" />
                                <asp:ListItem Text="6" Value="6" /><asp:ListItem Text="12" Value="12" />
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-6 d-flex align-items-end">
                            <p class="text-muted small mb-2">Schedule below is auto-split evenly from the amount and due date above (first installment due on the date above, subsequent ones one month apart). Adjust each row's percentage/date as needed &mdash; percentages must total 100%.</p>
                        </div>
                    </div>

                    <asp:Panel ID="pnlInstallments" runat="server">
                        <asp:Repeater ID="rptInstallments" runat="server">
                            <ItemTemplate>
                                <div class="installment-row row g-2 align-items-center">
                                    <div class="col-md-2"><span class="fw-semibold">Installment <%# Eval("InstallmentNo") %></span>
                                        <asp:HiddenField ID="hdnNo" runat="server" Value='<%# Eval("InstallmentNo") %>' />
                                    </div>
                                    <div class="col-md-5">
                                        <label class="form-label mb-0 small">Due Date</label>
                                        <asp:TextBox ID="txtInstDueDate" runat="server" CssClass="form-control form-control-sm" TextMode="Date" Text='<%# Eval("DueDate", "{0:yyyy-MM-dd}") %>' />
                                    </div>
                                    <div class="col-md-5">
                                        <label class="form-label mb-0 small">Amount %</label>
                                        <asp:TextBox ID="txtInstPercent" runat="server" CssClass="form-control form-control-sm" TextMode="Number" step="0.01" Text='<%# Eval("AmountPercent") %>' />
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>

                    <h6 class="fw-bold text-primary mb-3 mt-4">Late Fee Rules</h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-3">
                            <label class="form-label">Late Fee Type</label>
                            <asp:DropDownList ID="ddlLateFeeType" runat="server" CssClass="form-select">
                                <asp:ListItem Text="Flat Amount" Value="Flat" />
                                <asp:ListItem Text="Per Day Overdue" Value="PerDay" />
                                <asp:ListItem Text="Percentage of Outstanding" Value="Percentage" />
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Late Fee Value</label>
                            <asp:TextBox ID="txtLateFeeValue" runat="server" CssClass="form-control" TextMode="Number" step="0.01" Text="0" />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Grace Period (days)</label>
                            <asp:TextBox ID="txtGraceDays" runat="server" CssClass="form-control" TextMode="Number" Text="0" />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Max Late Fee (optional cap, &#8377;)</label>
                            <asp:TextBox ID="txtLateFeeMax" runat="server" CssClass="form-control" TextMode="Number" step="0.01" />
                        </div>
                    </div>

                    <div class="d-flex gap-2">
                        <asp:Button ID="btnSave" runat="server" Text="Save Fee Structure" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        <a href="FeeStructureManagement.aspx" class="btn btn-outline-secondary">Cancel</a>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    </form>
</body>
</html>
