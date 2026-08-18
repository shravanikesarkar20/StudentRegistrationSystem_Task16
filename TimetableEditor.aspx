<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TimetableEditor.aspx.cs" Inherits="StudentRegistrationSystem.TimetableEditor" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Timetable Editor | Admin Panel</title>
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
                    <a href="TimetableGenerate.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-arrow-left me-1"></i>Back to Generate</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <h4 class="mb-3"><i class="bi bi-pencil-square me-2"></i><asp:Literal ID="litDivisionName" runat="server" /></h4>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h5 class="mb-0">Current Timetable</h5></div>
                <div class="card-body p-3 p-md-4"><asp:Literal ID="litGrid" runat="server" /></div>
            </div>

            <div class="row">
                <div class="col-lg-6">
                    <div class="card shadow-sm border-0 mb-4">
                        <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-arrows-move me-2"></i>Move a Class</h5></div>
                        <div class="card-body p-3 p-md-4">
                            <div class="mb-2"><label class="form-label small">Select entry</label><asp:DropDownList ID="ddlEditEntry" runat="server" CssClass="form-select form-select-sm" /></div>
                            <div class="row g-2">
                                <div class="col-6"><label class="form-label small">New Day</label><asp:DropDownList ID="ddlEditDay" runat="server" CssClass="form-select form-select-sm" /></div>
                                <div class="col-6"><label class="form-label small">New Period</label><asp:DropDownList ID="ddlEditPeriod" runat="server" CssClass="form-select form-select-sm" /></div>
                                <div class="col-6"><label class="form-label small">Faculty</label><asp:DropDownList ID="ddlEditFaculty" runat="server" CssClass="form-select form-select-sm" /></div>
                                <div class="col-6"><label class="form-label small">Room</label><asp:DropDownList ID="ddlEditRoom" runat="server" CssClass="form-select form-select-sm" /></div>
                            </div>
                            <asp:Button ID="btnMove" runat="server" Text="Validate &amp; Save" CssClass="btn btn-sm btn-primary mt-3" OnClick="btnMove_Click" />
                            <asp:Literal ID="litMoveResult" runat="server" />
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card shadow-sm border-0 mb-4">
                        <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-arrow-left-right me-2"></i>Swap Two Classes</h5></div>
                        <div class="card-body p-3 p-md-4">
                            <div class="mb-2"><label class="form-label small">First entry</label><asp:DropDownList ID="ddlSwapA" runat="server" CssClass="form-select form-select-sm" /></div>
                            <div class="mb-2"><label class="form-label small">Second entry</label><asp:DropDownList ID="ddlSwapB" runat="server" CssClass="form-select form-select-sm" /></div>
                            <asp:Button ID="btnSwap" runat="server" Text="Validate &amp; Swap" CssClass="btn btn-sm btn-primary mt-2" OnClick="btnSwap_Click" />
                            <asp:Literal ID="litSwapResult" runat="server" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
