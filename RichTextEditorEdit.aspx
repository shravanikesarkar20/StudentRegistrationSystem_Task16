<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RichTextEditorEdit.aspx.cs" Inherits="StudentRegistrationSystem.RichTextEditorEdit" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><asp:Literal ID="litPageTitle" runat="server" Text="New Document" /> | Rich Text Editor</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />

    <!-- Task 10, Requirement 4: TinyMCE 6, self-hosted from jsDelivr's mirror of the open-source
         npm package (NOT the Tiny Cloud CDN), so the editor is fully functional out of the box
         with no API key / signup / usage nag required. -->
    <script src="https://cdn.jsdelivr.net/npm/tinymce@6/tinymce.min.js" referrerpolicy="origin"></script>

    <style>
        body { background: #f4f7f6; }
        .tox-tinymce { border-radius: 12px !important; border-color: #d6dedb !important; }
        .field-label { font-weight: 700; font-size: .85rem; color: #395550; }
        .required-mark { color: #e11d48; }

        .preview-body { max-height: 65vh; overflow-y: auto; border: 1px solid #e6ece9; border-radius: 12px; padding: 20px; background: #fff; }
        .preview-body img { max-width: 100%; height: auto; }
        .preview-body table { border-collapse: collapse; width: 100%; }
        .preview-body table td, .preview-body table th { border: 1px solid #d6dedb; padding: 6px 10px; }

        /* Task 10, Requirement 4 (Full Screen Editing Mode): TinyMCE's own fullscreen plugin
           already takes over the viewport; this just makes sure our page chrome doesn't
           fight it while active. */
        body.tox-fullscreen-body .navbar, body.tox-fullscreen-body .container-fluid > .card:not(:has(.tox-tinymce)) { display: none; }
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
                    <a href="AdvertisementManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-megaphone-fill me-1"></i>Advertisements</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

            <div class="d-flex justify-content-between align-items-center mb-3">
                <nav aria-label="breadcrumb">
                    <ol class="breadcrumb mb-0 small">
                        <li class="breadcrumb-item"><a href="RichTextEditor.aspx">Rich Text Editor</a></li>
                        <li class="breadcrumb-item active"><asp:Literal ID="litBreadcrumb" runat="server" Text="New Document" /></li>
                    </ol>
                </nav>
                <asp:HiddenField ID="hdnDocumentId" runat="server" Value="0" />
            </div>

            <div class="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3">
                    <h4 class="mb-0"><i class="bi bi-file-earmark-richtext me-2"></i><asp:Literal ID="litFormHeading" runat="server" Text="Create Document" /></h4>
                </div>
                <div class="card-body p-3 p-md-4">

                    <div class="row g-3 mb-3">
                        <div class="col-md-8">
                            <label class="field-label" for="<%= txtTitle.ClientID %>">Document Title <span class="required-mark">*</span></label>
                            <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" MaxLength="255" placeholder="Enter a title for this document" />
                            <asp:RequiredFieldValidator ID="rfvTitle" runat="server" ControlToValidate="txtTitle"
                                CssClass="text-danger small d-block mt-1" Display="Dynamic" ErrorMessage="Document Title is required."
                                ValidationGroup="DocForm" />
                        </div>
                        <div class="col-md-4">
                            <label class="field-label" for="<%= ddlStatus.ClientID %>">Status</label>
                            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select">
                                <asp:ListItem Text="Published" Value="Published" Selected="True" />
                                <asp:ListItem Text="Draft" Value="Draft" />
                            </asp:DropDownList>
                        </div>
                    </div>

                    <div class="mb-2">
                        <label class="field-label" for="<%= txtContent.ClientID %>">Rich Text Content <span class="required-mark">*</span></label>
                    </div>

                    <asp:TextBox ID="txtContent" runat="server" TextMode="MultiLine" EnableViewState="false" CssClass="d-none" />
                    <asp:RequiredFieldValidator ID="rfvContent" runat="server" ControlToValidate="txtContent"
                        CssClass="text-danger small d-block mt-2" Display="Dynamic" ErrorMessage="Rich Text Content is required."
                        ValidationGroup="DocForm" />
                    <asp:CustomValidator ID="cvContent" runat="server" ControlToValidate="txtContent"
                        CssClass="text-danger small d-block mt-1" Display="Dynamic" ErrorMessage="Rich Text Content cannot be empty."
                        ClientValidationFunction="validateEditorHasText" ValidationGroup="DocForm" OnServerValidate="cvContent_ServerValidate" />

                    <div class="d-flex flex-wrap gap-2 justify-content-end mt-4 pt-3 border-top">
                        <a href="RichTextEditor.aspx" class="btn btn-outline-secondary"><i class="bi bi-x-lg me-1"></i>Cancel</a>
                        <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-outline-warning" OnClick="btnReset_Click"
                            CausesValidation="false" OnClientClick="return confirm('Reset all fields? Any unsaved changes will be lost.');" />
                        <button type="button" id="btnPreview" class="btn btn-outline-primary"><i class="bi bi-eye me-1"></i>Preview</button>
                        <asp:Button ID="btnSave" runat="server" Text="Save Document" CssClass="btn btn-primary" OnClick="btnSave_Click"
                            ValidationGroup="DocForm" OnClientClick="if (typeof tinymce !== 'undefined') { tinymce.triggerSave(); }" />
                    </div>

                </div>
            </div>
        </div>

        <!-- Preview modal (Requirement 8/5: Preview Before Saving, without a round trip) -->
        <div class="modal fade" id="previewModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-scrollable">
                <div class="modal-content">
                    <div class="modal-header card-header-gradient">
                        <h5 class="modal-title"><i class="bi bi-eye me-2"></i>Preview</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <h5 id="previewTitle" class="mb-3"></h5>
                        <div class="preview-body" id="previewBody"></div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <script>
            var RTE_TEXTAREA_ID = "<%= txtContent.ClientID %>";
            var RTE_UPLOAD_URL = "RichTextImageUpload.ashx";

            tinymce.init({
                selector: "#" + RTE_TEXTAREA_ID,
                height: 520,
                menubar: "file edit view insert format tools table help",
                plugins: "advlist autolink lists link image charmap preview anchor searchreplace " +
                         "visualblocks code fullscreen insertdatetime media table hr wordcount emoticons quickbars",
                toolbar: "undo redo | blocks fontfamily fontsize | bold italic underline strikethrough superscript subscript removeformat | " +
                         "forecolor backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | " +
                         "lineheight styles | link image table hr charmap | searchreplace preview fullscreen code",
                toolbar_mode: "sliding",
                contextmenu: "link image table",
                branding: false,
                promotion: false,
                relative_urls: false,
                remove_script_host: false,
                convert_urls: false,

                // Requirement 4 (Insert > Images: upload / resize / align)
                images_upload_url: RTE_UPLOAD_URL,
                automatic_uploads: true,
                file_picker_types: "image",
                image_advtab: true,
                image_title: true,
                image_caption: true,
                resize: true,

                // Requirement 4 (Insert > Tables: rows/cols, merge/split via the table plugin's own UI)
                table_toolbar: "tableprops tabledelete | tableinsertrowbefore tableinsertrowafter tabledeleterow | " +
                                "tableinsertcolbefore tableinsertcolafter tabledeletecol | tablemergecells tablesplitcells",
                table_appearance_options: true,
                table_advtab: true,

                // Requirement 4 (Paragraph Formatting: paragraph spacing presets, via the Formats dropdown)
                style_formats: [
                    { title: "Headings", items: [
                        { title: "Heading 1", format: "h1" },
                        { title: "Heading 2", format: "h2" },
                        { title: "Heading 3", format: "h3" },
                        { title: "Paragraph", format: "p" }
                    ]},
                    { title: "Paragraph Spacing", items: [
                        { title: "Compact",  selector: "p,div", styles: { "margin-bottom": "4px" } },
                        { title: "Normal",   selector: "p,div", styles: { "margin-bottom": "10px" } },
                        { title: "Relaxed",  selector: "p,div", styles: { "margin-bottom": "20px" } },
                        { title: "Spacious", selector: "p,div", styles: { "margin-bottom": "32px" } }
                    ]}
                ],

                // Requirement 9/10: block obviously unsafe raw markup even before the server
                // sanitizer runs a second, authoritative pass on save (RichTextSanitizer.cs).
                valid_elements: "*[*]",
                invalid_elements: "script,iframe,object,embed,applet,form,meta,link,base",

                setup: function (editor) {
                    editor.on("init", function () {
                        editor.getContainer().querySelectorAll("input,button,select,textarea").forEach(function (el) {
                            el.setAttribute("tabindex", "-1");
                        });
                    });
                }
            });

            document.getElementById("btnPreview").addEventListener("click", function () {
                if (typeof tinymce !== "undefined") { tinymce.triggerSave(); }
                var titleInput = document.getElementById("<%= txtTitle.ClientID %>");
                var contentArea = document.getElementById(RTE_TEXTAREA_ID);
                document.getElementById("previewTitle").innerText = titleInput.value || "(Untitled document)";
                document.getElementById("previewBody").innerHTML = contentArea.value || "<p class='text-muted'>Nothing to preview yet.</p>";
                var modal = new bootstrap.Modal(document.getElementById("previewModal"));
                modal.show();
            });

            // CustomValidator client hook (Requirement 9: reject content that is technically
            // non-empty markup but has no real text, e.g. an empty paragraph).
            function validateEditorHasText(sender, args) {
                var ed = (typeof tinymce !== "undefined") ? tinymce.get(RTE_TEXTAREA_ID) : null;
                var text = ed ? ed.getContent({ format: "text" }).trim() : "";
                args.IsValid = text.length > 0;
            }
        </script>
    </form>
</body>
</html>
