<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdvertisementManagement.aspx.cs" Inherits="StudentRegistrationSystem.AdvertisementManagement" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Advertisements | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }

        .table thead th {
            background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase;
            letter-spacing: .5px; border-bottom: 2px solid #dde7e4;
        }
        .table td { vertical-align: middle; font-size: .93rem; }

        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; letter-spacing: .2px; }
        .badge-active   { background: rgba(22,163,74,.15); color: #16a34a; }
        .badge-inactive { background: rgba(108,117,125,.16); color: #6c757d; }

        .ad-thumb {
            width: 72px; height: 48px; object-fit: cover; border-radius: 8px; border: 1px solid #e6ece9; background: #f4f7f6;
        }
        .ad-thumb-empty {
            width: 72px; height: 48px; border-radius: 8px; border: 1px dashed #c7cdd6; background: #fff;
            display: flex; align-items: center; justify-content: center; color: #b9c4c1; font-size: 1.1rem;
        }

        .ad-title-cell { max-width: 260px; }
        .ad-title-cell .ad-title-text {
            overflow: hidden; text-overflow: ellipsis; white-space: nowrap; display: block;
            font-weight: 600; color: #111827;
        }

        .order-controls .btn { padding: .15rem .4rem; line-height: 1; }

        .global-toggle-card {
            border-radius: 16px; border: 1px solid #e6ece9; background: #fff; padding: 16px 20px;
        }
        .form-switch .form-check-input { width: 2.75em; height: 1.5em; cursor: pointer; }

        #<%= txtSearch.ClientID %> { width: 240px; }
        @media (max-width: 767px) {
            #<%= txtSearch.ClientID %> { width: 100%; }
            .ad-title-cell { max-width: 160px; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-shield-lock-fill me-2"></i>Admin Panel</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="AdminDashboard.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a>
                    <a href="BannerManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-images me-1"></i>Home Banners</a>
                    <a href="RichTextEditor.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-file-earmark-richtext me-1"></i>Rich Text Editor</a>
                    <a href="AdvertisementManagement.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-megaphone-fill me-1"></i>Advertisements</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <!-- Requirement 2: global enable/disable switch for the whole advertisement modal feature -->
            <div class="global-toggle-card mb-4 d-flex flex-wrap justify-content-between align-items-center gap-3">
                <div>
                    <h6 class="mb-1 fw-bold"><i class="bi bi-toggle2-on me-2"></i>Advertisement Modal</h6>
                    <p class="text-muted small mb-0">Master switch — when off, no advertisement will ever be shown on the Student Registration page, regardless of individual ad status.</p>
                </div>
                <div class="form-check form-switch mb-0">
                    <asp:CheckBox ID="chkModalEnabled" runat="server" CssClass="form-check-input" AutoPostBack="true" OnCheckedChanged="chkModalEnabled_CheckedChanged" />
                    <label class="form-check-label fw-semibold"><asp:Literal ID="litToggleLabel" runat="server" /></label>
                </div>
            </div>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
                    <h4 class="mb-0"><i class="bi bi-megaphone-fill me-2"></i>Advertisements &mdash; Registration Page Modal</h4>
                    <a href="AdvertisementEdit.aspx" class="btn btn-light btn-sm fw-semibold"><i class="bi bi-plus-lg me-1"></i>New Advertisement</a>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                        <p class="text-muted small mb-0"><asp:Literal ID="litResultCount" runat="server" /></p>

                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search by advertisement title..." />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                            <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnClear_Click" CausesValidation="false" />
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="gvAds" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvAds_RowCommand" OnRowDataBound="gvAds_RowDataBound"
                            EmptyDataText="No advertisements found. Click &quot;New Advertisement&quot; to create one.">
                            <Columns>
                                <asp:TemplateField HeaderText="Order" ItemStyle-Width="90">
                                    <ItemTemplate>
                                        <div class="d-flex align-items-center gap-2 order-controls">
                                            <span class="fw-semibold"><%# Eval("DisplayOrder") %></span>
                                            <div class="btn-group-vertical btn-group-sm">
                                                <asp:LinkButton ID="btnMoveUp" runat="server" CssClass="btn btn-outline-secondary" ToolTip="Move Up"
                                                    CommandName="MoveUp" CommandArgument='<%# Eval("AdvertisementID") %>'
                                                    Enabled='<%# Container.DataItemIndex > 0 %>'><i class="bi bi-caret-up-fill"></i></asp:LinkButton>
                                                <asp:LinkButton ID="btnMoveDown" runat="server" CssClass="btn btn-outline-secondary" ToolTip="Move Down"
                                                    CommandName="MoveDown" CommandArgument='<%# Eval("AdvertisementID") %>'><i class="bi bi-caret-down-fill"></i></asp:LinkButton>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Banner" ItemStyle-Width="90">
                                    <ItemTemplate>
                                        <asp:Image ID="imgThumb" runat="server" CssClass="ad-thumb" Visible="false" />
                                        <asp:Literal ID="litNoThumb" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Title">
                                    <ItemTemplate>
                                        <div class="ad-title-cell">
                                            <span class="ad-title-text"><%# Eval("Title") %></span>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status" ItemStyle-Width="110">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnToggleActive" runat="server" CssClass="btn btn-sm p-0 border-0 bg-transparent"
                                            CommandName="ToggleActive" CommandArgument='<%# Eval("AdvertisementID") %>' ToolTip="Click to toggle active/inactive">
                                            <asp:Literal ID="litStatusBadge" runat="server" />
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Created" SortExpression="CreatedDate" ItemStyle-Width="140">
                                    <ItemTemplate><%# Eval("CreatedDate", "{0:dd-MMM-yyyy hh:mm tt}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Updated" SortExpression="UpdatedDate" ItemStyle-Width="140">
                                    <ItemTemplate><%# Eval("UpdatedDate", "{0:dd-MMM-yyyy hh:mm tt}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="120" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="d-flex gap-1 justify-content-center">
                                            <a href='<%# "AdvertisementEdit.aspx?id=" + Eval("AdvertisementID") %>' class="btn btn-sm btn-outline-primary" title="Edit"><i class="bi bi-pencil"></i></a>
                                            <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-outline-danger" ToolTip="Delete"
                                                CommandName="DeleteAd" CommandArgument='<%# Eval("AdvertisementID") %>'
                                                OnClientClick='<%# "return confirm(\"Delete the advertisement \\\"" + System.Web.HttpUtility.JavaScriptStringEncode(Eval("Title").ToString()) + "\\\"? This cannot be undone.\");" %>'>
                                                <i class="bi bi-trash"></i></asp:LinkButton>
                                        </div>
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
