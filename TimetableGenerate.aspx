<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TimetableGenerate.aspx.cs" Inherits="StudentRegistrationSystem.TimetableGenerate" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Timetable &gt; Generate | Admin Panel</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .tt-grid td, .tt-grid th { text-align: center; vertical-align: middle; font-size: .82rem; }
        .tt-cell { background: #eef6f3; border-radius: 6px; padding: .4rem; }
        .tt-cell .sub { font-weight: 700; color: #1f6f5c; display: block; }
        .tt-cell .meta { font-size: .72rem; color: #6c757d; }
        .tt-empty { color: #cfd8d5; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-shield-lock-fill me-2"></i>Admin Panel</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="AdminDashboard.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a>
                    <a href="TimetableSetup.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-calendar3 me-1"></i>Academic Setup</a>
                    <a href="TimetableFacultyManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-person-badge me-1"></i>Faculty</a>
                    <a href="TimetableRoomManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-door-open me-1"></i>Rooms/Labs</a>
                    <a href="TimetableGenerate.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-magic me-1"></i>Generate</a>
                    <a href="RoomTimetableView.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-grid-3x3 me-1"></i>Room Utilization</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-magic me-2"></i>Auto-Generate Timetable</h5></div>
                <div class="card-body p-3 p-md-4">
                    <div class="row g-2 align-items-end">
                        <div class="col-md-3"><label class="form-label small">Academic Year</label><asp:DropDownList ID="ddlAcademicYear" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlAcademicYear_SelectedIndexChanged" /></div>
                        <div class="col-md-5"><label class="form-label small">Division</label><asp:DropDownList ID="ddlDivision" runat="server" CssClass="form-select form-select-sm" /></div>
                        <div class="col-md-2"><asp:Button ID="btnGenerate" runat="server" Text="Generate" CssClass="btn btn-sm btn-primary w-100" OnClick="btnGenerate_Click" /></div>
                        <div class="col-md-2"><asp:Button ID="btnView" runat="server" Text="View Only" CssClass="btn btn-sm btn-outline-secondary w-100" OnClick="btnView_Click" CausesValidation="false" /></div>
                    </div>
                </div>
            </div>

            <asp:Panel ID="pnlUnresolved" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4 border-warning">
                <div class="card-header bg-warning-subtle py-3"><h5 class="mb-0 text-warning-emphasis"><i class="bi bi-exclamation-triangle me-2"></i>Unresolved Conflicts</h5></div>
                <div class="card-body p-3 p-md-4">
                    <asp:Literal ID="litUnresolved" runat="server" />
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlGrid" runat="server" Visible="false" CssClass="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3 d-flex justify-content-between align-items-center">
                    <h5 class="mb-0"><i class="bi bi-grid-3x3 me-2"></i>Generated Timetable</h5>
                    <asp:HyperLink ID="hlEdit" runat="server" CssClass="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-pencil-square me-1"></i>Edit Manually</asp:HyperLink>
                </div>
                <div class="card-body p-3 p-md-4">
                    <asp:Literal ID="litGrid" runat="server" />
                </div>
            </asp:Panel>
        </div>
    </form>
</body>
</html>
