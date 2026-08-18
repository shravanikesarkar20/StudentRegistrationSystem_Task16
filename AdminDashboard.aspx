<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminDashboard.aspx.cs" Inherits="StudentRegistrationSystem.AdminDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Admin Dashboard | Advanced Student Registration System</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }

        .stat-card {
            background: #fff; border: 1px solid #e6ece9; border-radius: 18px;
            padding: 18px 20px; height: 100%;
        }
        .stat-card .stat-icon {
            width: 44px; height: 44px; border-radius: 12px;
            display: inline-flex; align-items: center; justify-content: center;
            font-size: 20px; margin-bottom: 10px;
        }
        .stat-card .stat-value { font-size: 1.7rem; font-weight: 800; color: #111827; margin: 0; line-height: 1; }
        .stat-card .stat-label { font-size: .82rem; color: #78818c; margin-top: 6px; font-weight: 600; }

        .icon-blue   { background: rgba(13,148,136,.13);  color: #0d9488; }
        .icon-green  { background: rgba(22,163,74,.12);  color: #16a34a; }
        .icon-gray   { background: rgba(108,117,125,.12);color: #6c757d; }
        .icon-amber  { background: rgba(245,158,11,.16);  color: #b45309; }
        .icon-teal   { background: rgba(45,212,191,.18); color: #0f766e; }
        .icon-red    { background: rgba(225,29,72,.12);  color: #e11d48; }

        /* Table header restyled: light background, uppercase micro-label —
           still legible on a PC without being visually heavy. */
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

        /* Search box grows to fill available space instead of a fixed px width,
           which is what forced the toolbar to overflow at 100% zoom before. */
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
                    <a href="AdminDashboard.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a>
                    <a href="InstituteManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-building me-1"></i>Institutes</a>
                    <a href="BannerManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-images me-1"></i>Home Banners</a>
                    <!-- Task 10, Requirement 1: new Admin Panel menu entry for the Rich Text Editor module. -->
                    <a href="RichTextEditor.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-file-earmark-richtext me-1"></i>Rich Text Editor</a>
                    <!-- Task 11, Requirement 2: new Admin Panel menu entry for the Advertisement Modal module. -->
                    <a href="AdvertisementManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-megaphone-fill me-1"></i>Advertisements</a>
                    <!-- Task 12: new Admin Panel menu entry for Registration Fee Management. -->
                    <a href="FeeStructureManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-cash-coin me-1"></i>Registration Fees</a>
                    <!-- Task 15: new Admin Panel menu entry for the Automated Timetable Management module. -->
                    <a href="TimetableSetup.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-calendar3 me-1"></i>Timetable</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <!-- Stat cards -->
            <div class="row g-3 mb-4">
                <div class="col-6 col-md-4 col-xl-2">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-blue"><i class="bi bi-people-fill"></i></div>
                        <p class="stat-value"><asp:Literal ID="litTotalRegistered" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Total Registered</p>
                    </div>
                </div>
                <div class="col-6 col-md-4 col-xl-2">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-teal"><i class="bi bi-person-check-fill"></i></div>
                        <p class="stat-value"><asp:Literal ID="litActive" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Active Students</p>
                    </div>
                </div>
                <div class="col-6 col-md-4 col-xl-2">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-gray"><i class="bi bi-person-x-fill"></i></div>
                        <p class="stat-value"><asp:Literal ID="litInactive" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Inactive Students</p>
                    </div>
                </div>
                <div class="col-6 col-md-4 col-xl-2">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-amber"><i class="bi bi-hourglass-split"></i></div>
                        <p class="stat-value"><asp:Literal ID="litPending" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Pending Approval</p>
                    </div>
                </div>
                <div class="col-6 col-md-4 col-xl-2">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-green"><i class="bi bi-check-circle-fill"></i></div>
                        <p class="stat-value"><asp:Literal ID="litApproved" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Approved</p>
                    </div>
                </div>
                <div class="col-6 col-md-4 col-xl-2">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-red"><i class="bi bi-x-circle-fill"></i></div>
                        <p class="stat-value"><asp:Literal ID="litRejected" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Rejected</p>
                    </div>
                </div>
            </div>
            <!-- Note: col-xl-2 keeps 6 cards on one row only from ~1200px up, where there is
                 comfortable room; below that they wrap to 3-per-row (col-md-4) or 2-per-row
                 (col-6), so nothing gets squeezed and no zooming is ever needed to read them. -->

            <!-- Candidate management panel -->
            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3">
                    <h4 class="mb-0"><i class="bi bi-people-fill me-2"></i>Student Applications</h4>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                        <ul class="nav nav-tabs flex-nowrap">
                            <li class="nav-item">
                                <asp:LinkButton ID="lnkTabPending" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="Pending">Pending</asp:LinkButton>
                            </li>
                            <li class="nav-item">
                                <asp:LinkButton ID="lnkTabApproved" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="Approved">Approved</asp:LinkButton>
                            </li>
                            <li class="nav-item">
                                <asp:LinkButton ID="lnkTabRejected" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="Rejected">Rejected</asp:LinkButton>
                            </li>
                            <li class="nav-item">
                                <asp:LinkButton ID="lnkTabAll" runat="server" CssClass="nav-link" OnClick="TabButton_Click" CommandArgument="All">All Students</asp:LinkButton>
                            </li>
                        </ul>

                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search name, email, ID..." />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="gvCandidates" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvCandidates_RowCommand" OnRowDataBound="gvCandidates_RowDataBound"
                            EmptyDataText="No students found for this view.">
                            <Columns>
                                <asp:BoundField DataField="StudentID" HeaderText="ID" />
                                <asp:BoundField DataField="FullName" HeaderText="Full Name" />
                                <asp:BoundField DataField="Email" HeaderText="Email" />
                                <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                                <asp:TemplateField HeaderText="Registered">
                                    <ItemTemplate><%# Eval("RegistrationDate", "{0:dd-MMM-yyyy}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Approval">
                                    <ItemTemplate>
                                        <asp:Literal ID="litApprovalBadge" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Account">
                                    <ItemTemplate>
                                        <asp:Literal ID="litAccountBadge" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="70" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="dropdown">
                                            <button class="btn action-menu-btn" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                                                <i class="bi bi-three-dots-vertical"></i>
                                            </button>
                                            <ul class="dropdown-menu dropdown-menu-end">
                                                <li>
                                                    <asp:LinkButton ID="btnApprove" runat="server" CssClass="dropdown-item text-success" CommandName="Approve" CommandArgument='<%# Eval("StudentID") %>'><i class="bi bi-check-lg"></i>Approve</asp:LinkButton>
                                                </li>
                                                <li>
                                                    <asp:LinkButton ID="btnReject" runat="server" CssClass="dropdown-item text-danger" CommandName="OpenReject" CommandArgument='<%# Eval("StudentID") %>' OnClientClick='<%# "return openRejectModal(" + Eval("StudentID") + ", " + JsStringLiteral(Eval("FullName").ToString()) + ");" %>'><i class="bi bi-x-lg"></i>Reject</asp:LinkButton>
                                                </li>
                                                <li><hr class="dropdown-divider" /></li>
                                                <li>
                                                    <asp:LinkButton ID="btnActivate" runat="server" CssClass="dropdown-item" CommandName="Activate" CommandArgument='<%# Eval("StudentID") %>'><i class="bi bi-toggle-on"></i>Activate</asp:LinkButton>
                                                </li>
                                                <li>
                                                    <asp:LinkButton ID="btnDeactivate" runat="server" CssClass="dropdown-item" CommandName="Deactivate" CommandArgument='<%# Eval("StudentID") %>'
                                                        OnClientClick="return confirm('Deactivate this student? They will not be able to log in until reactivated.');">
                                                        <i class="bi bi-toggle-off"></i>Deactivate</asp:LinkButton>
                                                </li>
                                                <li>
                                                    <asp:LinkButton ID="btnReset" runat="server" CssClass="dropdown-item" CommandName="Reset" CommandArgument='<%# Eval("StudentID") %>'
                                                        OnClientClick="return confirm('Reset this application back to Pending? The rejection remark will be cleared.');">
                                                        <i class="bi bi-arrow-counterclockwise"></i>Reset</asp:LinkButton>
                                                </li>
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
                        <h5 class="modal-title"><i class="bi bi-x-circle-fill me-2"></i>Reject Application</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p class="small text-muted mb-3">You are rejecting the application for <strong id="rejectStudentName"></strong> (Student ID <span id="rejectStudentIdLabel"></span>). A rejection remark is required and will be emailed to the student.</p>
                        <label class="form-label small">Rejection Remark <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtRejectRemark" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" placeholder="Explain why this application is being rejected..." />
                        <asp:RequiredFieldValidator ID="rfvRemark" runat="server" ControlToValidate="txtRejectRemark" CssClass="text-danger small d-block mt-1" Display="Dynamic" ErrorMessage="A rejection remark is required." ValidationGroup="RejectForm" />
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                        <asp:Button ID="btnConfirmReject" runat="server" Text="Confirm Rejection" CssClass="btn btn-danger btn-sm" OnClick="btnConfirmReject_Click" ValidationGroup="RejectForm" />
                    </div>
                </div>
            </div>
        </div>
        <asp:HiddenField ID="hdnRejectStudentId" runat="server" />

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <script>
            var rejectModalInstance = null;
            function openRejectModal(studentId, studentName) {
                document.getElementById("<%= hdnRejectStudentId.ClientID %>").value = studentId;
                document.getElementById("rejectStudentIdLabel").innerText = studentId;
                document.getElementById("rejectStudentName").innerText = studentName;
                rejectModalInstance = new bootstrap.Modal(document.getElementById('rejectModal'));
                rejectModalInstance.show();
                return false;
            }
        </script>
    </form>
</body>
</html>
