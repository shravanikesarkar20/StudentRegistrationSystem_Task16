<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TimetableFacultyEdit.aspx.cs" Inherits="StudentRegistrationSystem.TimetableFacultyEdit" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Faculty Assignment | Admin Panel</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <style>
        body { background: #f4f7f6; }
        .table td, .table th { vertical-align: middle; font-size: .85rem; text-align: center; }
        .avail-cell { cursor: pointer; user-select: none; padding: .35rem; border-radius: 4px; }
        .avail-on { background: rgba(22,163,74,.18); color: #16a34a; font-weight: 700; }
        .avail-off { background: #f1f1f1; color: #adb5bd; }
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
                    <a href="TimetableFacultyManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-arrow-left me-1"></i>Back to Faculty</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <h4 class="mb-3"><i class="bi bi-person-badge me-2"></i><asp:Literal ID="litFacultyName" runat="server" /></h4>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3"><h5 class="mb-0">Subject / Class Assignments</h5></div>
                <div class="card-body p-3 p-md-4">
                    <asp:GridView ID="gvAssignments" runat="server" AutoGenerateColumns="false" CssClass="table table-hover mb-0" GridLines="None"
                        DataKeyNames="FacultySubjectID" OnRowCommand="gvAssignments_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="SubjectCode" HeaderText="Code" />
                            <asp:BoundField DataField="SubjectName" HeaderText="Subject" />
                            <asp:BoundField DataField="SubjectType" HeaderText="Type" />
                            <asp:BoundField DataField="CourseName" HeaderText="Course" />
                            <asp:BoundField DataField="YearSemester" HeaderText="Year/Sem" />
                            <asp:BoundField DataField="DivisionName" HeaderText="Division" />
                            <asp:TemplateField HeaderText=""><ItemTemplate><asp:LinkButton runat="server" CommandName="Remove" CommandArgument='<%# Eval("FacultySubjectID") %>' CssClass="btn btn-sm btn-outline-danger" OnClientClick="return confirm('Remove this assignment?');">Remove</asp:LinkButton></ItemTemplate></asp:TemplateField>
                        </Columns>
                    </asp:GridView>

                    <hr />
                    <div class="row g-2 align-items-end">
                        <div class="col-md-3"><label class="form-label small">Division</label><asp:DropDownList ID="ddlDivision" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlDivision_SelectedIndexChanged" /></div>
                        <div class="col-md-6"><label class="form-label small">Subject (subjects available to selected division)</label><asp:DropDownList ID="ddlSubject" runat="server" CssClass="form-select form-select-sm" /></div>
                        <div class="col-md-3"><asp:Button ID="btnAssign" runat="server" Text="Assign" CssClass="btn btn-sm btn-primary w-100" OnClick="btnAssign_Click" /></div>
                    </div>
                </div>
            </div>

            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3 d-flex justify-content-between align-items-center">
                    <h5 class="mb-0">Availability</h5>
                    <small class="text-white-50">Click cells to toggle. No cells set = available every period by default.</small>
                </div>
                <div class="card-body p-3 p-md-4">
                    <asp:Literal ID="litAvailabilityGrid" runat="server" />
                    <div class="d-flex gap-2 mt-2">
                        <asp:Button ID="btnSaveAvailability" runat="server" Text="Save Availability" CssClass="btn btn-sm btn-primary" OnClick="btnSaveAvailability_Click" CausesValidation="false" />
                        <asp:Button ID="btnClearAvailability" runat="server" Text="Reset to Default (Available Every Period)" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnClearAvailability_Click" CausesValidation="false" />
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
