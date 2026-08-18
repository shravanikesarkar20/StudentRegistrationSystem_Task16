<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TimetableRoomManagement.aspx.cs" Inherits="StudentRegistrationSystem.TimetableRoomManagement" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Timetable &gt; Rooms/Labs | Admin Panel</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .table thead th { background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase; letter-spacing: .5px; border-bottom: 2px solid #dde7e4; }
        .table td { vertical-align: middle; font-size: .9rem; }
        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; }
        .badge-active { background: rgba(22,163,74,.15); color: #16a34a; }
        .badge-inactive { background: rgba(108,117,125,.16); color: #6c757d; }
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
                    <a href="TimetableRoomManagement.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-door-open me-1"></i>Rooms/Labs</a>
                    <a href="TimetableGenerate.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-magic me-1"></i>Generate</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-door-open me-2"></i>Rooms &amp; Laboratories</h5></div>
                <div class="card-body p-3 p-md-4">
                    <asp:GridView ID="gvRooms" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mb-0" GridLines="None"
                        DataKeyNames="RoomID" OnRowCommand="gvRooms_RowCommand" OnRowDataBound="gvRooms_RowDataBound">
                        <Columns>
                            <asp:BoundField DataField="RoomNumber" HeaderText="Room #" />
                            <asp:BoundField DataField="RoomType" HeaderText="Type" />
                            <asp:BoundField DataField="Capacity" HeaderText="Capacity" />
                            <asp:BoundField DataField="Building" HeaderText="Building" />
                            <asp:TemplateField HeaderText="Status"><ItemTemplate><asp:Literal ID="litStatus" runat="server" /></ItemTemplate></asp:TemplateField>
                            <asp:TemplateField HeaderText="">
                                <ItemTemplate>
                                    <a href='TimetableRoomEdit.aspx?id=<%# Eval("RoomID") %>' class="btn btn-sm btn-outline-primary">Availability</a>
                                    <asp:LinkButton runat="server" CommandName="Toggle" CommandArgument='<%# Eval("RoomID") %>' CssClass="btn btn-sm btn-outline-secondary">Toggle Active</asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>

                    <hr />
                    <div class="row g-2 align-items-end">
                        <div class="col-md-2"><label class="form-label small">Room #</label><asp:TextBox ID="txtRoomNumber" runat="server" CssClass="form-control form-control-sm" /></div>
                        <div class="col-md-2"><label class="form-label small">Type</label>
                            <asp:DropDownList ID="ddlRoomType" runat="server" CssClass="form-select form-select-sm">
                                <asp:ListItem Text="Classroom" Value="Classroom" />
                                <asp:ListItem Text="Laboratory" Value="Laboratory" />
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2"><label class="form-label small">Capacity</label><asp:TextBox ID="txtCapacity" runat="server" CssClass="form-control form-control-sm" TextMode="Number" /></div>
                        <div class="col-md-3"><label class="form-label small">Building</label><asp:TextBox ID="txtBuilding" runat="server" CssClass="form-control form-control-sm" /></div>
                        <div class="col-md-2"><asp:Button ID="btnAddRoom" runat="server" Text="Add Room" CssClass="btn btn-sm btn-primary w-100" OnClick="btnAddRoom_Click" /></div>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
