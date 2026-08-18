<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TimetableSetup.aspx.cs" Inherits="StudentRegistrationSystem.TimetableSetup" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Timetable &gt; Academic Setup | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }
        .table thead th { background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase; letter-spacing: .5px; border-bottom: 2px solid #dde7e4; }
        .table td { vertical-align: middle; font-size: .88rem; }
        .badge-status { font-size: .74rem; padding: .35em .7em; border-radius: 20px; font-weight: 700; }
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
                    <a href="TimetableSetup.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-calendar3 me-1"></i>Academic Setup</a>
                    <a href="TimetableFacultyManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-person-badge me-1"></i>Faculty</a>
                    <a href="TimetableRoomManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-door-open me-1"></i>Rooms/Labs</a>
                    <a href="TimetableGenerate.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-magic me-1"></i>Generate</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <ul class="nav nav-tabs mb-3">
                <li class="nav-item"><a class="nav-link <%= ActiveTab=="schedule"?"active":"" %>" href="TimetableSetup.aspx?tab=schedule">Working Schedule</a></li>
                <li class="nav-item"><a class="nav-link <%= ActiveTab=="divisions"?"active":"" %>" href="TimetableSetup.aspx?tab=divisions">Divisions</a></li>
                <li class="nav-item"><a class="nav-link <%= ActiveTab=="subjects"?"active":"" %>" href="TimetableSetup.aspx?tab=subjects">Subjects</a></li>
            </ul>

            <%-- ===================== WORKING SCHEDULE ===================== --%>
            <asp:Panel ID="pnlSchedule" runat="server">
                <div class="card shadow-sm border-0 mb-4">
                    <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-calendar-week me-2"></i>Working Days</h5></div>
                    <div class="card-body p-3 p-md-4">
                        <asp:GridView ID="gvDays" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mb-0" GridLines="None"
                            DataKeyNames="DayID" OnRowCommand="gvDays_RowCommand">
                            <Columns>
                                <asp:BoundField DataField="DayName" HeaderText="Day" />
                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate><span class='badge-status <%# (bool)Eval("IsWorkingDay") ? "badge-active" : "badge-inactive" %>'><%# (bool)Eval("IsWorkingDay") ? "Working Day" : "Off" %></span></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="">
                                    <ItemTemplate><asp:LinkButton runat="server" CommandName="Toggle" CommandArgument='<%# Eval("DayID") %>' CssClass="btn btn-sm btn-outline-secondary">Toggle</asp:LinkButton></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

                <div class="card shadow-sm border-0 mb-4">
                    <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-clock-history me-2"></i>Periods</h5></div>
                    <div class="card-body p-3 p-md-4">
                        <asp:GridView ID="gvPeriods" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mb-0" GridLines="None"
                            DataKeyNames="PeriodID" OnRowCommand="gvPeriods_RowCommand" OnRowDataBound="gvPeriods_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="PeriodNumber" HeaderText="#" />
                                <asp:BoundField DataField="Label" HeaderText="Label" />
                                <asp:BoundField DataField="StartTime" HeaderText="Start" />
                                <asp:BoundField DataField="EndTime" HeaderText="End" />
                                <asp:TemplateField HeaderText="Type"><ItemTemplate><asp:Literal ID="litType" runat="server" /></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText=""><ItemTemplate><asp:LinkButton runat="server" CommandName="Delete" CommandArgument='<%# Eval("PeriodID") %>' CssClass="btn btn-sm btn-outline-danger" OnClientClick="return confirm('Remove this period?');">Remove</asp:LinkButton></ItemTemplate></asp:TemplateField>
                            </Columns>
                        </asp:GridView>

                        <hr />
                        <div class="row g-2 align-items-end">
                            <div class="col-md-2"><label class="form-label small">Period #</label><asp:TextBox ID="txtPeriodNum" runat="server" CssClass="form-control form-control-sm" TextMode="Number" /></div>
                            <div class="col-md-3"><label class="form-label small">Label</label><asp:TextBox ID="txtPeriodLabel" runat="server" CssClass="form-control form-control-sm" placeholder="Period 8 / Break" /></div>
                            <div class="col-md-2"><label class="form-label small">Start (HH:mm)</label><asp:TextBox ID="txtPeriodStart" runat="server" CssClass="form-control form-control-sm" placeholder="16:00" /></div>
                            <div class="col-md-2"><label class="form-label small">End (HH:mm)</label><asp:TextBox ID="txtPeriodEnd" runat="server" CssClass="form-control form-control-sm" placeholder="16:55" /></div>
                            <div class="col-md-2"><div class="form-check mt-4"><asp:CheckBox ID="chkPeriodBreak" runat="server" CssClass="form-check-input" /><label class="form-check-label small">Is Break</label></div></div>
                            <div class="col-md-1"><asp:Button ID="btnAddPeriod" runat="server" Text="Add" CssClass="btn btn-sm btn-primary w-100" OnClick="btnAddPeriod_Click" /></div>
                        </div>
                    </div>
                </div>

                <div class="card shadow-sm border-0 mb-4">
                    <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-sliders me-2"></i>Generation Limits</h5></div>
                    <div class="card-body p-3 p-md-4">
                        <div class="row g-2 align-items-end">
                            <div class="col-md-3"><label class="form-label small">Max classes per day (per division / faculty)</label><asp:TextBox ID="txtMaxPerDay" runat="server" CssClass="form-control form-control-sm" TextMode="Number" /></div>
                            <div class="col-md-2"><asp:Button ID="btnSaveMax" runat="server" Text="Save" CssClass="btn btn-sm btn-primary w-100" OnClick="btnSaveMax_Click" /></div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <%-- ===================== DIVISIONS ===================== --%>
            <asp:Panel ID="pnlDivisions" runat="server">
                <div class="card shadow-sm border-0 mb-4">
                    <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-diagram-3 me-2"></i>Divisions / Classes</h5></div>
                    <div class="card-body p-3 p-md-4">
                        <asp:GridView ID="gvDivisions" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mb-0" GridLines="None"
                            DataKeyNames="DivisionID" OnRowCommand="gvDivisions_RowCommand" OnRowDataBound="gvDivisions_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="YearLabel" HeaderText="Academic Year" />
                                <asp:BoundField DataField="CourseName" HeaderText="Course" />
                                <asp:BoundField DataField="YearSemester" HeaderText="Year/Sem" />
                                <asp:BoundField DataField="DivisionName" HeaderText="Division" />
                                <asp:BoundField DataField="StudentStrength" HeaderText="Strength" />
                                <asp:TemplateField HeaderText="Status"><ItemTemplate><asp:Literal ID="litStatus" runat="server" /></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText=""><ItemTemplate><asp:LinkButton runat="server" CommandName="Toggle" CommandArgument='<%# Eval("DivisionID") %>' CssClass="btn btn-sm btn-outline-secondary">Toggle Active</asp:LinkButton></ItemTemplate></asp:TemplateField>
                            </Columns>
                        </asp:GridView>

                        <hr />
                        <div class="row g-2 align-items-end">
                            <div class="col-md-2"><label class="form-label small">Academic Year</label><asp:DropDownList ID="ddlDivAcademicYear" runat="server" CssClass="form-select form-select-sm" /></div>
                            <div class="col-md-3"><label class="form-label small">Course</label><asp:DropDownList ID="ddlDivCourse" runat="server" CssClass="form-select form-select-sm" /></div>
                            <div class="col-md-2"><label class="form-label small">Year/Sem</label><asp:TextBox ID="txtDivYearSem" runat="server" CssClass="form-control form-control-sm" placeholder="Year 2 - Sem 3" /></div>
                            <div class="col-md-2"><label class="form-label small">Division</label><asp:TextBox ID="txtDivName" runat="server" CssClass="form-control form-control-sm" placeholder="A" /></div>
                            <div class="col-md-2"><label class="form-label small">Strength</label><asp:TextBox ID="txtDivStrength" runat="server" CssClass="form-control form-control-sm" TextMode="Number" /></div>
                            <div class="col-md-1"><asp:Button ID="btnAddDivision" runat="server" Text="Add" CssClass="btn btn-sm btn-primary w-100" OnClick="btnAddDivision_Click" /></div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <%-- ===================== SUBJECTS ===================== --%>
            <asp:Panel ID="pnlSubjects" runat="server">
                <div class="card shadow-sm border-0 mb-4">
                    <div class="card-header card-header-gradient py-3"><h5 class="mb-0"><i class="bi bi-journal-text me-2"></i>Subjects</h5></div>
                    <div class="card-body p-3 p-md-4">
                        <asp:GridView ID="gvSubjects" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mb-0" GridLines="None"
                            DataKeyNames="SubjectID" OnRowCommand="gvSubjects_RowCommand" OnRowDataBound="gvSubjects_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="SubjectCode" HeaderText="Code" />
                                <asp:BoundField DataField="SubjectName" HeaderText="Subject" />
                                <asp:BoundField DataField="CourseName" HeaderText="Course" />
                                <asp:BoundField DataField="YearSemester" HeaderText="Year/Sem" />
                                <asp:BoundField DataField="SubjectType" HeaderText="Type" />
                                <asp:BoundField DataField="WeeklyHours" HeaderText="Weekly Hrs" />
                                <asp:TemplateField HeaderText="Status"><ItemTemplate><asp:Literal ID="litStatus" runat="server" /></ItemTemplate></asp:TemplateField>
                                <asp:TemplateField HeaderText=""><ItemTemplate><asp:LinkButton runat="server" CommandName="Toggle" CommandArgument='<%# Eval("SubjectID") %>' CssClass="btn btn-sm btn-outline-secondary">Toggle Active</asp:LinkButton></ItemTemplate></asp:TemplateField>
                            </Columns>
                        </asp:GridView>

                        <hr />
                        <div class="row g-2 align-items-end">
                            <div class="col-md-2"><label class="form-label small">Code</label><asp:TextBox ID="txtSubCode" runat="server" CssClass="form-control form-control-sm" /></div>
                            <div class="col-md-3"><label class="form-label small">Name</label><asp:TextBox ID="txtSubName" runat="server" CssClass="form-control form-control-sm" /></div>
                            <div class="col-md-2"><label class="form-label small">Course</label><asp:DropDownList ID="ddlSubCourse" runat="server" CssClass="form-select form-select-sm" /></div>
                            <div class="col-md-2"><label class="form-label small">Year/Sem</label><asp:TextBox ID="txtSubYearSem" runat="server" CssClass="form-control form-control-sm" placeholder="Year 2 - Sem 3" /></div>
                            <div class="col-md-1"><label class="form-label small">Type</label>
                                <asp:DropDownList ID="ddlSubType" runat="server" CssClass="form-select form-select-sm">
                                    <asp:ListItem Text="Theory" Value="Theory" />
                                    <asp:ListItem Text="Practical" Value="Practical" />
                                    <asp:ListItem Text="Tutorial" Value="Tutorial" />
                                </asp:DropDownList>
                            </div>
                            <div class="col-md-1"><label class="form-label small">Wk Hrs</label><asp:TextBox ID="txtSubWeeklyHours" runat="server" CssClass="form-control form-control-sm" TextMode="Number" Text="1" /></div>
                            <div class="col-md-1"><asp:Button ID="btnAddSubject" runat="server" Text="Add" CssClass="btn btn-sm btn-primary w-100" OnClick="btnAddSubject_Click" /></div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

        </div>
    </form>
</body>
</html>
