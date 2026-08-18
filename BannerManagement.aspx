<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BannerManagement.aspx.cs" Inherits="StudentRegistrationSystem.BannerManagement" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Home Banners | Admin Panel</title>

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

        .banner-thumb {
            width: 96px; height: 54px; object-fit: cover; border-radius: 8px; border: 1px solid #e6ece9; background: #f4f7f6;
        }
        .banner-thumb-empty {
            width: 96px; height: 54px; border-radius: 8px; border: 1px dashed #c7cdd6; background: #fff;
            display: flex; align-items: center; justify-content: center; color: #b9c4c1; font-size: 1.1rem;
        }

        .banner-title-cell { max-width: 260px; }
        .banner-title-cell .banner-title-text {
            overflow: hidden; text-overflow: ellipsis; white-space: nowrap; display: block;
            font-weight: 600; color: #111827;
        }

        .order-controls .btn { padding: .15rem .4rem; line-height: 1; }

        #<%= txtSearch.ClientID %> { width: 240px; }
        @media (max-width: 767px) {
            #<%= txtSearch.ClientID %> { width: 100%; }
            .banner-title-cell { max-width: 160px; }
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
                    <a href="RichTextEditor.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-file-earmark-richtext me-1"></i>Rich Text Editor</a>
                    <a href="AdvertisementManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-megaphone-fill me-1"></i>Advertisements</a>
                    <a href="FeeStructureManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-cash-coin me-1"></i>Registration Fees</a>
                    <a href="BannerManagement.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-images me-1"></i>Home Banners</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
                    <h4 class="mb-0"><i class="bi bi-images me-2"></i>Home Page &mdash; Slide / Banner Management</h4>
                    <div class="d-flex gap-2">
                        <a href="Home.aspx" target="_blank" class="btn btn-outline-light btn-sm fw-semibold"><i class="bi bi-box-arrow-up-right me-1"></i>View Home Page</a>
                        <a href="BannerEdit.aspx" class="btn btn-light btn-sm fw-semibold"><i class="bi bi-plus-lg me-1"></i>New Banner</a>
                    </div>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                        <p class="text-muted small mb-0"><asp:Literal ID="litResultCount" runat="server" /></p>

                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search by banner title..." />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                            <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnClear_Click" CausesValidation="false" />
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="gvBanners" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvBanners_RowCommand" OnRowDataBound="gvBanners_RowDataBound"
                            EmptyDataText="No banners found. Click &quot;New Banner&quot; to create one.">
                            <Columns>
                                <asp:TemplateField HeaderText="Order" ItemStyle-Width="90">
                                    <ItemTemplate>
                                        <div class="d-flex align-items-center gap-2 order-controls">
                                            <span class="fw-semibold"><%# Eval("DisplayOrder") %></span>
                                            <div class="btn-group-vertical btn-group-sm">
                                                <asp:LinkButton ID="btnMoveUp" runat="server" CssClass="btn btn-outline-secondary" ToolTip="Move Up"
                                                    CommandName="MoveUp" CommandArgument='<%# Eval("BannerID") %>'
                                                    Enabled='<%# Container.DataItemIndex > 0 %>'><i class="bi bi-caret-up-fill"></i></asp:LinkButton>
                                                <asp:LinkButton ID="btnMoveDown" runat="server" CssClass="btn btn-outline-secondary" ToolTip="Move Down"
                                                    CommandName="MoveDown" CommandArgument='<%# Eval("BannerID") %>'><i class="bi bi-caret-down-fill"></i></asp:LinkButton>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Banner" ItemStyle-Width="110">
                                    <ItemTemplate>
                                        <asp:Image ID="imgThumb" runat="server" CssClass="banner-thumb" Visible="false" />
                                        <asp:Literal ID="litNoThumb" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Title">
                                    <ItemTemplate>
                                        <div class="banner-title-cell">
                                            <span class="banner-title-text"><%# Eval("Title") %></span>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status" ItemStyle-Width="110">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnToggleActive" runat="server" CssClass="btn btn-sm p-0 border-0 bg-transparent"
                                            CommandName="ToggleActive" CommandArgument='<%# Eval("BannerID") %>' ToolTip="Click to toggle active/inactive">
                                            <asp:Literal ID="litStatusBadge" runat="server" />
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Updated" SortExpression="UpdatedDate" ItemStyle-Width="140">
                                    <ItemTemplate><%# Eval("UpdatedDate", "{0:dd-MMM-yyyy hh:mm tt}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="120" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="d-flex gap-1 justify-content-center">
                                            <a href='<%# "BannerEdit.aspx?id=" + Eval("BannerID") %>' class="btn btn-sm btn-outline-primary" title="Edit"><i class="bi bi-pencil"></i></a>
                                            <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-outline-danger" ToolTip="Delete"
                                                CommandName="DeleteBanner" CommandArgument='<%# Eval("BannerID") %>'
                                                OnClientClick='<%# "return confirm(\"Delete the banner \\\"" + System.Web.HttpUtility.JavaScriptStringEncode(Eval("Title").ToString()) + "\\\"? This cannot be undone.\");" %>'>
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
