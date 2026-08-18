<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="InstituteManagement.aspx.cs" Inherits="StudentRegistrationSystem.InstituteManagement" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Institute Management | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }

        .table thead th {
            background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase;
            letter-spacing: .5px; border-bottom: 2px solid #dde7e4;
        }
        .table td { vertical-align: middle; font-size: .93rem; }

        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; letter-spacing: .2px; }
        .badge-pending  { background: rgba(245,158,11,.18); color: #b45309; }
        .badge-approved { background: rgba(22,163,74,.15); color: #16a34a; }
        .badge-rejected { background: rgba(225,29,72,.15); color: #e11d48; }
        .badge-active   { background: rgba(13,148,136,.15); color: #0d9488; }
        .badge-inactive { background: rgba(108,117,125,.15); color: #6c757d; }

        .empty-state { padding: 40px 20px; text-align: center; color: #b9c4c1; }
        .courses-cell { max-width: 220px; white-space: normal; font-size: .82rem; color: #58636d; }

        #<%= txtSearch.ClientID %> { width: 240px; }
        @media (max-width: 767px) {
            #<%= txtSearch.ClientID %> { width: 100%; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-shield-lock-fill me-2"></i>Admin Panel</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="AdminDashboard.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a>
                    <a href="InstituteManagement.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-building me-1"></i>Institutes</a>
                    <a href="FeeStructureManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-cash-coin me-1"></i>Registration Fees</a>
                    <a href="TimetableSetup.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-calendar3 me-1"></i>Timetable</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3">
                    <h4 class="mb-0"><i class="bi bi-building me-2"></i>Institute Registrations</h4>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                        <ul class="nav nav-tabs flex-nowrap">
                            <li class="nav-item"><asp:LinkButton ID="lnkTabPending" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="Pending">Pending</asp:LinkButton></li>
                            <li class="nav-item"><asp:LinkButton ID="lnkTabApproved" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="Approved">Approved</asp:LinkButton></li>
                            <li class="nav-item"><asp:LinkButton ID="lnkTabRejected" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="Rejected">Rejected</asp:LinkButton></li>
                            <li class="nav-item"><asp:LinkButton ID="lnkTabAll" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="All">All Institutes</asp:LinkButton></li>
                        </ul>

                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search name, city..." />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="gvInstitutes" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvInstitutes_RowCommand" OnRowDataBound="gvInstitutes_RowDataBound"
                            EmptyDataText="No institutes found for this view.">
                            <Columns>
                                <asp:BoundField DataField="InstituteID" HeaderText="ID" />
                                <asp:BoundField DataField="InstituteName" HeaderText="Institute Name" />
                                <asp:BoundField DataField="City" HeaderText="City" />
                                <asp:BoundField DataField="Capacity" HeaderText="Capacity" />
                                <asp:TemplateField HeaderText="Contact">
                                    <ItemTemplate>
                                        <div><%# Eval("ContactEmail") %></div>
                                        <div class="text-muted small"><%# Eval("ContactPhone") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Courses Offered">
                                    <ItemTemplate>
                                        <div class="courses-cell"><%# Eval("CoursesOffered") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Submitted">
                                    <ItemTemplate><%# Eval("SubmittedDate", "{0:dd-MMM-yyyy}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate>
                                        <asp:Literal ID="litApprovalBadge" runat="server" /><br />
                                        <asp:Literal ID="litActiveBadge" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="70" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="dropdown">
                                            <button class="btn action-menu-btn" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                                                <i class="bi bi-three-dots-vertical"></i>
                                            </button>
                                            <ul class="dropdown-menu dropdown-menu-end">
                                                <li><asp:LinkButton ID="btnApprove" runat="server" CssClass="dropdown-item text-success" CommandName="Approve" CommandArgument='<%# Eval("InstituteID") %>'><i class="bi bi-check-lg"></i>Approve</asp:LinkButton></li>
                                                <li><asp:LinkButton ID="btnReject" runat="server" CssClass="dropdown-item text-danger" CommandName="OpenReject" CommandArgument='<%# Eval("InstituteID") %>' OnClientClick='<%# "return openRejectModal(" + Eval("InstituteID") + ", " + JsStringLiteral(Eval("InstituteName").ToString()) + ");" %>'><i class="bi bi-x-lg"></i>Reject</asp:LinkButton></li>
                                                <li><hr class="dropdown-divider" /></li>
                                                <li><asp:LinkButton ID="btnActivate" runat="server" CssClass="dropdown-item" CommandName="Activate" CommandArgument='<%# Eval("InstituteID") %>'><i class="bi bi-toggle-on"></i>Activate</asp:LinkButton></li>
                                                <li><asp:LinkButton ID="btnDeactivate" runat="server" CssClass="dropdown-item" CommandName="Deactivate" CommandArgument='<%# Eval("InstituteID") %>' OnClientClick="return confirm('Deactivate this institute? It will disappear from the Student Registration form until reactivated.');"><i class="bi bi-toggle-off"></i>Deactivate</asp:LinkButton></li>
                                            </ul>
                                        </div>
                                        <asp:Panel ID="pnlRemark" runat="server" CssClass="mt-1 small text-danger" Visible="false">
                                            <i class="bi bi-chat-left-quote"></i> <asp:Literal ID="litRemark" runat="server" />
                                        </asp:Panel>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>

                </div>
            </div>
        </div>

        <!-- Reject remark modal -->
        <div class="modal fade" id="rejectModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header card-header-gradient">
                        <h5 class="modal-title"><i class="bi bi-x-circle-fill me-2"></i>Reject Institute</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p class="small text-muted mb-3">You are rejecting the registration for <strong id="rejectInstituteName"></strong> (ID <span id="rejectInstituteIdLabel"></span>).</p>
                        <label class="form-label small">Rejection Remark <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtRejectRemark" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" placeholder="Explain why this registration is being rejected..." />
                        <asp:RequiredFieldValidator ID="rfvRemark" runat="server" ControlToValidate="txtRejectRemark" CssClass="text-danger small d-block mt-1" Display="Dynamic" ErrorMessage="A rejection remark is required." ValidationGroup="RejectForm" />
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                        <asp:Button ID="btnConfirmReject" runat="server" Text="Confirm Rejection" CssClass="btn btn-danger btn-sm" OnClick="btnConfirmReject_Click" ValidationGroup="RejectForm" />
                    </div>
                </div>
            </div>
        </div>
        <asp:HiddenField ID="hdnRejectInstituteId" runat="server" />

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <script>
            var rejectModalInstance = null;
            function openRejectModal(instituteId, instituteName) {
                document.getElementById("<%= hdnRejectInstituteId.ClientID %>").value = instituteId;
                document.getElementById("rejectInstituteIdLabel").innerText = instituteId;
                document.getElementById("rejectInstituteName").innerText = instituteName;
                rejectModalInstance = new bootstrap.Modal(document.getElementById('rejectModal'));
                rejectModalInstance.show();
                return false;
            }
        </script>
    </form>
</body>
</html>
