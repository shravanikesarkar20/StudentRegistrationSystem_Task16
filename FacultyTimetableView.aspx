<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FacultyTimetableView.aspx.cs" Inherits="StudentRegistrationSystem.FacultyTimetableView" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Faculty Timetable | Advanced Student Registration System</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        .table thead th { background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase; letter-spacing: .5px; }
        .table td { vertical-align: middle; font-size: .88rem; }
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
                <div class="card-header card-header-gradient py-3"><h4 class="mb-0"><i class="bi bi-person-badge me-2"></i>Faculty Timetable</h4></div>
                <div class="card-body p-3 p-md-4">
                    <div class="row g-2 align-items-end no-print">
                        <div class="col-md-6"><label class="form-label small">Faculty</label><asp:DropDownList ID="ddlFaculty" runat="server" CssClass="form-select form-select-sm" /></div>
                        <div class="col-md-3"><asp:Button ID="btnView" runat="server" Text="View Timetable" CssClass="btn btn-sm btn-primary w-100" OnClick="btnView_Click" /></div>
                    </div>

                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-muted py-5">
                        <i class="bi bi-calendar-x display-6 d-block mb-2"></i>No classes are currently scheduled for this faculty member.
                    </asp:Panel>

                    <asp:Panel ID="pnlGrid" runat="server" Visible="false" CssClass="mt-4">
                        <h5><asp:Literal ID="litHeading" runat="server" /></h5>
                        <asp:GridView ID="gvSchedule" runat="server" AutoGenerateColumns="false" CssClass="table table-hover" GridLines="None">
                            <Columns>
                                <asp:BoundField DataField="DayName" HeaderText="Day" />
                                <asp:BoundField DataField="PeriodLabel" HeaderText="Period" />
                                <asp:TemplateField HeaderText="Time"><ItemTemplate><%# Eval("StartTime") %> - <%# Eval("EndTime") %></ItemTemplate></asp:TemplateField>
                                <asp:BoundField DataField="SubjectName" HeaderText="Subject" />
                                <asp:TemplateField HeaderText="Class"><ItemTemplate><%# Eval("CourseName") %> &middot; <%# Eval("YearSemester") %> &middot; Div <%# Eval("DivisionName") %></ItemTemplate></asp:TemplateField>
                                <asp:BoundField DataField="RoomNumber" HeaderText="Room" />
                            </Columns>
                        </asp:GridView>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
