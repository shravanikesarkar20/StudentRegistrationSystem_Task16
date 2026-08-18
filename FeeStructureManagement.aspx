<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FeeStructureManagement.aspx.cs" Inherits="StudentRegistrationSystem.FeeStructureManagement" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Fee Structures | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }
        .table thead th {
            background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase;
            letter-spacing: .5px; border-bottom: 2px solid #dde7e4;
        }
        .table td { vertical-align: middle; font-size: .9rem; }
        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; letter-spacing: .2px; }
        .badge-active   { background: rgba(22,163,74,.15); color: #16a34a; }
        .badge-inactive { background: rgba(108,117,125,.16); color: #6c757d; }
        #<%= txtSearch.ClientID %> { width: 220px; }
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
                    <a href="StudentFeeDemand.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-receipt me-1"></i>Student Dues</a>
                    <a href="FeeReconciliation.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-arrow-left-right me-1"></i>Reconciliation</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
                    <h4 class="mb-0"><i class="bi bi-cash-coin me-2"></i>Registration Fee Structures</h4>
                    <a href="FeeStructureEdit.aspx" class="btn btn-light btn-sm fw-semibold"><i class="bi bi-plus-lg me-1"></i>New Fee Structure</a>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                        <p class="text-muted small mb-0"><asp:Literal ID="litResultCount" runat="server" /></p>
                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search course, fee type, year..." />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                            <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnClear_Click" CausesValidation="false" />
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="gvStructures" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvStructures_RowCommand" OnRowDataBound="gvStructures_RowDataBound"
                            EmptyDataText="No fee structures configured yet. Click &quot;New Fee Structure&quot; to add one.">
                            <Columns>
                                <asp:TemplateField HeaderText="Academic Year"><ItemTemplate><%# Eval("YearLabel") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Institute"><ItemTemplate><%# Eval("InstituteName") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Course"><ItemTemplate><%# Eval("CourseName") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Year/Sem"><ItemTemplate><%# Eval("YearSemester") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Category"><ItemTemplate><%# Eval("CategoryName") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Fee Type"><ItemTemplate><%# Eval("FeeTypeName") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Amount" ItemStyle-CssClass="text-end">
                                    <ItemTemplate>&#8377; <%# Eval("FeeAmount", "{0:N2}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Due Date"><ItemTemplate><%# Eval("DueDate", "{0:dd-MMM-yyyy}") %></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText="Installments">
                                    <ItemTemplate><%# Convert.ToBoolean(Eval("InstallmentsAllowed")) ? Eval("NumberOfInstallments") + "x" : "Single" %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Late Fee">
                                    <ItemTemplate><%# Eval("LateFeeType") %> &#8377;<%# Eval("LateFeeValue") %> (grace <%# Eval("LateFeeGraceDays") %>d)</ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnToggleActive" runat="server" CssClass="btn btn-sm p-0 border-0 bg-transparent"
                                            CommandName="ToggleActive" CommandArgument='<%# Eval("FeeStructureID") %>' ToolTip="Click to toggle active/inactive">
                                            <asp:Literal ID="litStatusBadge" runat="server" />
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="d-flex gap-1 justify-content-center">
                                            <a href='<%# "FeeStructureEdit.aspx?id=" + Eval("FeeStructureID") %>' class="btn btn-sm btn-outline-primary" title="Edit"><i class="bi bi-pencil"></i></a>
                                            <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-outline-danger" ToolTip="Delete"
                                                CommandName="DeleteStructure" CommandArgument='<%# Eval("FeeStructureID") %>'
                                                OnClientClick="return confirm('Delete this fee structure? This only works if no student fee demands have been generated from it.');">
                                                <i class="bi bi-trash"></i></asp:LinkButton>
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
