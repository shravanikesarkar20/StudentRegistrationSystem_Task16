<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RoomTimetableView.aspx.cs" Inherits="StudentRegistrationSystem.RoomTimetableView" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Room Utilization | Admin Panel</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .table thead th { background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase; letter-spacing: .5px; }
        .table td { vertical-align: middle; font-size: .88rem; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-shield-lock-fill me-2"></i>Admin Panel</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="TimetableGenerate.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-arrow-left me-1"></i>Back to Timetable</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h4 class="mb-0"><i class="bi bi-grid-3x3 me-2"></i>Room Utilization</h4></div>
                <div class="card-body p-3 p-md-4">
                    <div class="row g-2 align-items-end">
                        <div class="col-md-6"><label class="form-label small">Room</label><asp:DropDownList ID="ddlRoom" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlRoom_SelectedIndexChanged" /></div>
                    </div>

                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="text-center text-muted py-5">
                        <i class="bi bi-door-open display-6 d-block mb-2"></i>This room is currently unused &mdash; free for every period.
                    </asp:Panel>

                    <asp:Panel ID="pnlGrid" runat="server" Visible="false" CssClass="mt-4">
                        <asp:GridView ID="gvUsage" runat="server" AutoGenerateColumns="false" CssClass="table table-hover" GridLines="None">
                            <Columns>
                                <asp:BoundField DataField="DayName" HeaderText="Day" />
                                <asp:BoundField DataField="PeriodLabel" HeaderText="Period" />
                                <asp:BoundField DataField="SubjectName" HeaderText="Subject" />
                                <asp:TemplateField HeaderText="Class"><ItemTemplate><%# Eval("CourseName") %> &middot; Div <%# Eval("DivisionName") %><%# string.IsNullOrEmpty(Eval("BatchLabel").ToString())? "" : " (" + Eval("BatchLabel") + ")" %></ItemTemplate></asp:TemplateField>
                                <asp:BoundField DataField="FacultyName" HeaderText="Faculty" />
                            </Columns>
                        </asp:GridView>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
