<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Display.aspx.cs" Inherits="StudentRegistrationSystem.Display" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Records | Advanced Student Registration System</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        .student-photo { width:48px; height:48px; object-fit:cover; border-radius:50%; border:1.5px solid #e3e5f0; }
        #printHeader { display:none; }

        /* ---------- PRINT STYLES ---------- */
        @media print {
            body { background:#fff !important; }
            .no-print { display:none !important; }
            .navbar, .toolbar, .btn, footer { display:none !important; }
            #printHeader { display:block !important; text-align:center; margin-bottom:20px; }
            .card { border:none !important; box-shadow:none !important; }
            .table { font-size:12px; }
            .table thead th { background:#eee !important; color:#000 !important; -webkit-print-color-adjust: exact; }
            .student-photo { width:36px; height:36px; }
            a[href]:after { content: "" !important; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar navbar-dark bg-primary mb-4 no-print">
            <div class="container">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <div>
                    <a href="Home.aspx" class="btn btn-outline-light btn-sm me-2"><i class="bi bi-house-door-fill me-1"></i>Home</a>
                    <a href="Register.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-person-plus-fill me-1"></i>New Registration</a>
                    <a href="BulkRegister.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-people-fill me-1"></i>Bulk Registration</a>
                    <a href="AdminLogin.aspx" class="btn btn-outline-light btn-sm me-2"><i class="bi bi-shield-lock-fill me-1"></i>Admin</a>
                    <a href="Login.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-box-arrow-in-right me-1"></i>Student Login</a>
                </div>
            </div>
        </nav>

        <div class="container mb-5">

            <div id="printHeader">
                <h3>Advanced Student Registration System</h3>
                <p>Registered Students Report &mdash; Generated on <%= DateTime.Now.ToString("dd MMM yyyy, hh:mm tt") %></p>
                <hr />
            </div>

            <div class="card shadow-sm border-0">
                <div class="card-header bg-white py-3 toolbar no-print">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <h5 class="mb-0"><i class="bi bi-people-fill me-2"></i>Registered Students</h5>
                        <div>
                            <button type="button" class="btn btn-outline-dark" onclick="window.print();"><i class="bi bi-printer-fill me-1"></i>Print</button>
                            <asp:Button ID="btnExport" runat="server" Text="Export to Excel" CssClass="btn btn-success" OnClick="btnExport_Click" CausesValidation="false" />
                        </div>
                    </div>
                    <div class="row g-2 align-items-end">
                        <div class="col-md-3">
                            <label class="form-label small text-muted mb-1">Student Name</label>
                            <asp:TextBox ID="txtSearchName" runat="server" CssClass="form-control form-control-sm" placeholder="Search by name..." />
                        </div>
                        <div class="col-md-3">
                            <label class="form-label small text-muted mb-1">Email Address</label>
                            <asp:TextBox ID="txtSearchEmail" runat="server" CssClass="form-control form-control-sm" placeholder="Search by email..." />
                        </div>
                        <div class="col-md-2">
                            <label class="form-label small text-muted mb-1">Mobile Number</label>
                            <asp:TextBox ID="txtSearchMobile" runat="server" CssClass="form-control form-control-sm" placeholder="Search by mobile..." />
                        </div>
                        <div class="col-md-2">
                            <label class="form-label small text-muted mb-1">Gender</label>
                            <asp:DropDownList ID="ddlFilterGender" runat="server" CssClass="form-select form-select-sm">
                                <asp:ListItem Text="All Genders" Value="" />
                                <asp:ListItem Text="Male" Value="Male" />
                                <asp:ListItem Text="Female" Value="Female" />
                                <asp:ListItem Text="Other" Value="Other" />
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2 d-flex gap-2">
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-outline-primary btn-sm w-100" OnClick="btnSearch_Click" />
                            <asp:Button ID="btnReset" runat="server" Text="Reset Filter" CssClass="btn btn-outline-secondary btn-sm w-100" OnClick="btnReset_Click" CausesValidation="false" />
                        </div>
                    </div>
                </div>

                <div class="card-body">
                    <asp:Label ID="lblRecordCount" runat="server" CssClass="text-muted small d-block mb-2 no-print" />

                    <div class="table-responsive">
                        <asp:GridView ID="gvStudents" runat="server" CssClass="table table-hover table-bordered align-middle"
                            AutoGenerateColumns="false" GridLines="None" EmptyDataText="No student records found."
                            DataKeyNames="StudentID" AllowSorting="true" OnSorting="gvStudents_Sorting">
                            <Columns>
                                <asp:TemplateField HeaderText="Photo" ItemStyle-Width="60">
                                    <ItemTemplate>
                                        <img class="student-photo"
                                             src='<%# string.IsNullOrEmpty(Eval("PhotoPath") as string) ? "https://via.placeholder.com/48x48?text=NA" : "~/" + Eval("PhotoPath") %>'
                                             alt="Photo" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="StudentID" HeaderText="ID" />
                                <asp:BoundField DataField="FullName" HeaderText="Full Name" SortExpression="FullName" />
                                <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                                <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                                <asp:BoundField DataField="Location" HeaderText="Location" />
                                <asp:BoundField DataField="Gender" HeaderText="Gender" />
                                <asp:BoundField DataField="RegistrationDate" HeaderText="Registered On" DataFormatString="{0:dd MMM yyyy}" SortExpression="RegistrationDate" />
                                <asp:TemplateField HeaderText="Verified">
                                    <ItemTemplate>
                                        <span class='badge <%# (bool)Eval("IsEmailVerified") ? "bg-success" : "bg-secondary" %>'>
                                            <%# (bool)Eval("IsEmailVerified") ? "Verified" : "Pending" %>
                                        </span>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    </form>
</body>
</html>
