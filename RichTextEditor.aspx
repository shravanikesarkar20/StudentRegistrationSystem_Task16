<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RichTextEditor.aspx.cs" Inherits="StudentRegistrationSystem.RichTextEditor" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Rich Text Editor | Admin Panel</title>

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
        .badge-published { background: rgba(22,163,74,.15); color: #16a34a; }
        .badge-draft { background: rgba(245,158,11,.18); color: #b45309; }

        .empty-state { padding: 40px 20px; text-align: center; color: #b9c4c1; }

        .doc-title-cell { max-width: 360px; }
        .doc-title-cell .doc-title-text {
            overflow: hidden; text-overflow: ellipsis; white-space: nowrap; display: block;
            font-weight: 600; color: #111827;
        }

        #<%= txtSearch.ClientID %> { width: 240px; }
        @media (max-width: 767px) {
            #<%= txtSearch.ClientID %> { width: 100%; }
        }

        .preview-body { max-height: 65vh; overflow-y: auto; border: 1px solid #e6ece9; border-radius: 12px; padding: 20px; background: #fff; }
        .preview-body img { max-width: 100%; height: auto; }
        .preview-body table { border-collapse: collapse; width: 100%; }
        .preview-body table td, .preview-body table th { border: 1px solid #d6dedb; padding: 6px 10px; }
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
                    <a href="RichTextEditor.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-file-earmark-richtext me-1"></i>Rich Text Editor</a>
                    <a href="AdvertisementManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-megaphone-fill me-1"></i>Advertisements</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
                    <h4 class="mb-0"><i class="bi bi-file-earmark-richtext me-2"></i>Rich Text Editor &mdash; Documents</h4>
                    <a href="RichTextEditorEdit.aspx" class="btn btn-light btn-sm fw-semibold"><i class="bi bi-plus-lg me-1"></i>New Document</a>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                        <p class="text-muted small mb-0"><asp:Literal ID="litResultCount" runat="server" /></p>

                        <div class="d-flex gap-2">
                            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm" placeholder="Search by document title..." />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-sm btn-primary" OnClick="btnSearch_Click" />
                            <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnClear_Click" CausesValidation="false" />
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="gvDocuments" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                            GridLines="None" OnRowCommand="gvDocuments_RowCommand" OnRowDataBound="gvDocuments_RowDataBound"
                            OnSorting="gvDocuments_Sorting" AllowSorting="true"
                            EmptyDataText="No documents found. Click &quot;New Document&quot; to create one.">
                            <Columns>
                                <asp:TemplateField HeaderText="Sr. No." ItemStyle-Width="70">
                                    <ItemTemplate><%# Container.DataItemIndex + 1 %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Document Title" SortExpression="Title">
                                    <ItemTemplate>
                                        <div class="doc-title-cell">
                                            <span class="doc-title-text"><%# Eval("Title") %></span>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status" ItemStyle-Width="110">
                                    <ItemTemplate>
                                        <asp:Literal ID="litStatusBadge" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Created Date" SortExpression="CreatedDate" ItemStyle-Width="150">
                                    <ItemTemplate><%# Eval("CreatedDate", "{0:dd-MMM-yyyy hh:mm tt}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Last Modified" SortExpression="ModifiedDate" ItemStyle-Width="150">
                                    <ItemTemplate><%# Eval("ModifiedDate", "{0:dd-MMM-yyyy hh:mm tt}") %></ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="140" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <div class="d-flex gap-1 justify-content-center">
                                            <asp:LinkButton ID="btnView" runat="server" CssClass="btn btn-sm btn-outline-secondary" ToolTip="View / Preview"
                                                CommandName="ViewDoc" CommandArgument='<%# Eval("DocumentID") %>'><i class="bi bi-eye"></i></asp:LinkButton>
                                            <a href='<%# "RichTextEditorEdit.aspx?id=" + Eval("DocumentID") %>' class="btn btn-sm btn-outline-primary" title="Edit"><i class="bi bi-pencil"></i></a>
                                            <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-outline-danger" ToolTip="Delete"
                                                CommandName="DeleteDoc" CommandArgument='<%# Eval("DocumentID") %>'
                                                OnClientClick='<%# "return confirm(\"Delete the document \\\"" + System.Web.HttpUtility.JavaScriptStringEncode(Eval("Title").ToString()) + "\\\"? This cannot be undone.\");" %>'>
                                                <i class="bi bi-trash"></i></asp:LinkButton>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>

                    <!-- Pagination -->
                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mt-3">
                        <div class="small text-muted">
                            <asp:Literal ID="litPageInfo" runat="server" />
                        </div>
                        <div class="btn-group btn-group-sm">
                            <asp:LinkButton ID="btnFirst" runat="server" CssClass="btn btn-outline-secondary" OnClick="btnFirst_Click" CausesValidation="false"><i class="bi bi-chevron-double-left"></i></asp:LinkButton>
                            <asp:LinkButton ID="btnPrev" runat="server" CssClass="btn btn-outline-secondary" OnClick="btnPrev_Click" CausesValidation="false"><i class="bi bi-chevron-left"></i> Prev</asp:LinkButton>
                            <asp:LinkButton ID="btnNext" runat="server" CssClass="btn btn-outline-secondary" OnClick="btnNext_Click" CausesValidation="false">Next <i class="bi bi-chevron-right"></i></asp:LinkButton>
                            <asp:LinkButton ID="btnLast" runat="server" CssClass="btn btn-outline-secondary" OnClick="btnLast_Click" CausesValidation="false"><i class="bi bi-chevron-double-right"></i></asp:LinkButton>
                        </div>
                    </div>

                </div>
            </div>
        </div>

        <!-- View / Preview modal -->
        <div class="modal fade" id="viewModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-scrollable">
                <div class="modal-content">
                    <div class="modal-header card-header-gradient">
                        <h5 class="modal-title"><i class="bi bi-eye me-2"></i><asp:Literal ID="litViewTitle" runat="server" /></h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="preview-body">
                            <asp:Literal ID="litViewContent" runat="server" />
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <asp:HiddenField ID="hdnOpenViewModal" runat="server" Value="0" />

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <script>
            window.addEventListener("load", function () {
                var flag = document.getElementById("<%= hdnOpenViewModal.ClientID %>");
                if (flag && flag.value === "1") {
                    var modal = new bootstrap.Modal(document.getElementById('viewModal'));
                    modal.show();
                    flag.value = "0";
                }
            });
        </script>
    </form>
</body>
</html>
