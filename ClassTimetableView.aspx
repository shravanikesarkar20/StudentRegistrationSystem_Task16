<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ClassTimetableView.aspx.cs" Inherits="StudentRegistrationSystem.ClassTimetableView" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Class Timetable | Advanced Student Registration System</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        .tt-grid td, .tt-grid th { text-align: center; vertical-align: middle; font-size: .85rem; }
        .tt-cell { background: #eef6f3; border-radius: 6px; padding: .4rem; }
        .tt-cell .sub { font-weight: 700; color: #1f6f5c; display: block; }
        .tt-cell .meta { font-size: .74rem; color: #6c757d; }
        .tt-empty { color: #cfd8d5; }
        @media print { .no-print { display: none !important; } }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <nav class="navbar navbar-dark bg-primary mb-4 no-print">
            <div class="container">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <a href="Home.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-house-door-fill me-1"></i>Home</a>
            </div>
        </nav>

        <div class="container pb-5">
            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h4 class="mb-0"><i class="bi bi-calendar3 me-2"></i>Class / Division Timetable</h4></div>
                <div class="card-body p-3 p-md-4">
                    <div class="row g-2 align-items-end no-print">
                        <div class="col-md-3"><label class="form-label small">Academic Year</label><asp:DropDownList ID="ddlAcademicYear" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlAcademicYear_SelectedIndexChanged" /></div>
                        <div class="col-md-6"><label class="form-label small">Division</label><asp:DropDownList ID="ddlDivision" runat="server" CssClass="form-select form-select-sm" /></div>
                        <div class="col-md-3"><asp:Button ID="btnView" runat="server" Text="View Timetable" CssClass="btn btn-sm btn-primary w-100" OnClick="btnView_Click" /></div>
                    </div>

                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-muted py-5">
                        <i class="bi bi-calendar-x display-6 d-block mb-2"></i>No timetable has been published for this division yet.
                    </asp:Panel>

                    <asp:Panel ID="pnlGrid" runat="server" Visible="false" CssClass="mt-4">
                        <h5><asp:Literal ID="litHeading" runat="server" /></h5>
                        <asp:Literal ID="litGrid" runat="server" />
                    </asp:Panel>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
