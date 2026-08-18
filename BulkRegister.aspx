<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BulkRegister.aspx.cs" Inherits="StudentRegistrationSystem.BulkRegister" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Bulk Student Registration | Advanced Student Registration System</title>

    <!-- Bootstrap 5 -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />
    <!-- intl-tel-input -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/intl-tel-input@23.8.0/build/css/intlTelInput.css" />

    <style>
        .iti { width: 100%; }
        .import-status-ok  { color:#198754; font-weight:700; }
        .import-status-bad { color:#dc3545; font-weight:700; }
        .import-error-cell { font-size: .82rem; color:#dc3545; max-width: 260px; }
    </style>
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true" EnablePageMethods="true" />

        <div class="page-busy-overlay" id="busyOverlay">
            <div class="text-center">
                <div class="spinner-border text-primary" role="status"></div>
                <div class="mt-2 fw-semibold text-primary">Working on it&hellip;</div>
            </div>
        </div>

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <div>
                    <a href="Home.aspx" class="btn btn-outline-light btn-sm me-2"><i class="bi bi-house-door-fill me-1"></i>Home</a>
                    <a href="Register.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-person-plus-fill me-1"></i>Single Registration</a>
                    <a href="Display.aspx" class="btn btn-light btn-sm"><i class="bi bi-card-list me-1"></i>View Students</a>
                </div>
            </div>
        </nav>

        <div class="container mb-5">

            <div class="mb-4">
                <h2 class="mb-1"><i class="bi bi-people-fill me-2 text-primary"></i>Bulk Student Registration</h2>
                <p class="text-muted mb-0">Add several students at once — type them in one by one, or import a whole list from an Excel / CSV file. Nothing is saved to the database until you click <strong>Save All</strong>.</p>
            </div>

            <asp:Panel ID="pnlSuccessMsg" runat="server" CssClass="alert alert-success d-none" role="alert"></asp:Panel>
            <asp:Panel ID="pnlErrorMsg" runat="server" CssClass="alert alert-danger d-none" role="alert"></asp:Panel>
            <asp:Panel ID="pnlWarningMsg" runat="server" CssClass="alert alert-warning d-none" role="alert"></asp:Panel>

            <div class="row justify-content-center">
                <div class="col-lg-10">

                    <!-- ==================== MODE SWITCH ==================== -->
                    <ul class="nav mode-tabs mb-3" id="bulkModeTabs">
                        <li class="nav-item">
                            <button type="button" class="nav-link active" id="tabBtnManual" onclick="showBulkTab('manual')">
                                <i class="bi bi-keyboard"></i>Add Manually
                            </button>
                        </li>
                        <li class="nav-item">
                            <button type="button" class="nav-link" id="tabBtnImport" onclick="showBulkTab('import')">
                                <i class="bi bi-file-earmark-arrow-up"></i>Import from Excel / CSV
                            </button>
                        </li>
                    </ul>

                    <!-- ==================== TAB 1: ADD RECORD FORM ==================== -->
                    <div id="paneManual" class="bulk-pane">
                        <div class="card shadow-sm border-0 mb-4">
                            <div class="card-header card-header-gradient py-3">
                                <h4 class="mb-0"><i class="bi bi-person-plus-fill me-2"></i>Add One Student</h4>
                            </div>
                            <div class="card-body p-4">
                                <p class="text-muted small">
                                    Fill in a student's details and click <strong>Add Record</strong> to stage them below.
                                    <em>Note: bulk-added records skip the email OTP verification used on the single Registration page — use that page instead when a student needs to self-verify their own email.</em>
                                </p>

                                <div class="row g-3">
                                    <div class="col-md-4">
                                        <label class="form-label"><span class="step-badge">1</span>Full Name <span class="required-star">*</span></label>
                                        <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" placeholder="e.g. Aarav Sharma" MaxLength="150" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFullName" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Full name is required." ValidationGroup="BulkForm" />
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Email Address <span class="required-star">*</span></label>
                                        <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" placeholder="student@example.com" TextMode="Email" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Email is required." ValidationGroup="BulkForm" />
                                        <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Enter a valid email address." ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ValidationGroup="BulkForm" />
                                        <span id="emailDuplicateWarning" class="text-danger small d-none"><i class="bi bi-exclamation-circle-fill"></i> A student is already registered with this email address.</span>
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Mobile Number <span class="required-star">*</span></label>
                                        <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control" placeholder="Mobile number" />
                                        <asp:HiddenField ID="hdnFullMobile" runat="server" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMobile" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Mobile number is required." ValidationGroup="BulkForm" />
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Gender <span class="required-star">*</span></label>
                                        <asp:DropDownList ID="ddlGender" runat="server" CssClass="form-select">
                                            <asp:ListItem Text="Select Gender" Value="" />
                                            <asp:ListItem Text="Male" Value="Male" />
                                            <asp:ListItem Text="Female" Value="Female" />
                                            <asp:ListItem Text="Other" Value="Other" />
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlGender" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Please select gender." ValidationGroup="BulkForm" />
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Date of Birth <span class="required-star">*</span></label>
                                        <asp:TextBox ID="txtDOB" runat="server" CssClass="form-control" TextMode="Date" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDOB" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Date of birth is required." ValidationGroup="BulkForm" />
                                    </div>

                                    <asp:UpdatePanel ID="upLocation" runat="server" UpdateMode="Conditional">
                                        <ContentTemplate>
                                            <div class="col-md-4">
                                                <label class="form-label">Country <span class="required-star">*</span></label>
                                                <asp:DropDownList ID="ddlCountry" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlCountry_SelectedIndexChanged" />
                                            </div>
                                            <div class="col-md-4">
                                                <label class="form-label">State <span class="required-star">*</span></label>
                                                <asp:DropDownList ID="ddlState" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlState_SelectedIndexChanged" />
                                            </div>
                                            <div class="col-md-4">
                                                <label class="form-label">District <span class="required-star">*</span></label>
                                                <asp:DropDownList ID="ddlDistrict" runat="server" CssClass="form-select" />
                                            </div>
                                        </ContentTemplate>
                                    </asp:UpdatePanel>

                                    <div class="col-12">
                                        <label class="form-label">Full Address</label>
                                        <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" placeholder="House / Street / Area" />
                                    </div>
                                </div>

                                <div class="d-flex justify-content-end mt-3">
                                    <asp:Button ID="btnAddRecord" runat="server" Text="Add Record" CssClass="btn btn-primary px-4" OnClick="btnAddRecord_Click" ValidationGroup="BulkForm" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- ==================== TAB 2: IMPORT FROM FILE ==================== -->
                    <div id="paneImport" class="bulk-pane" style="display:none;">
                        <div class="card shadow-sm border-0 mb-4">
                            <div class="card-header card-header-gradient py-3">
                                <h4 class="mb-0"><i class="bi bi-file-earmark-arrow-up me-2"></i>Import Students from a File</h4>
                            </div>
                            <div class="card-body p-4">
                                <p class="text-muted small mb-3">
                                    Upload an Excel (<code>.xlsx</code>) or CSV (<code>.csv</code>) file with one student per row.
                                    The first row must be column headers. Don't have a file yet?
                                    <asp:LinkButton ID="btnDownloadTemplate" runat="server" OnClick="btnDownloadTemplate_Click" CausesValidation="false"><i class="bi bi-download me-1"></i>Download a ready-made template</asp:LinkButton>.
                                </p>

                                <div class="alert alert-info py-2 small mb-3">
                                    <i class="bi bi-info-circle-fill me-1"></i>
                                    Expected columns: <strong>FullName, Email, Mobile, Gender, DOB, Country, State, District, Address</strong>
                                    (Address is optional). Country / State / District must match the names already in the system —
                                    the preview below will flag any row it can't match.
                                </div>

                                <label class="file-drop-zone d-block mb-3" for="<%= fuBulkFile.ClientID %>" id="dropZone">
                                    <i class="bi bi-cloud-arrow-up-fill"></i>
                                    <div class="fw-semibold mt-2">Click to choose a file, or drag one here</div>
                                    <div class="text-muted-tight">.xlsx or .csv &middot; first row = column headers</div>
                                    <div id="fileNameChip" class="file-name-chip mt-2 d-none"><i class="bi bi-file-earmark-check-fill"></i><span id="fileNameChipText"></span></div>
                                    <asp:FileUpload ID="fuBulkFile" runat="server" />
                                </label>

                                <div class="d-flex flex-wrap gap-2 justify-content-end">
                                    <asp:Button ID="btnPreviewFile" runat="server" Text="Preview File" CssClass="btn btn-primary px-4" OnClick="btnPreviewFile_Click" CausesValidation="false" />
                                </div>

                                <!-- ---- Preview / validation results ---- -->
                                <asp:Panel ID="pnlImportPreview" runat="server" CssClass="mt-4 d-none">
                                    <div class="divider-text">Preview</div>

                                    <asp:Panel ID="pnlImportSummary" runat="server" CssClass="alert" />

                                    <div class="table-responsive">
                                        <asp:GridView ID="gvImportPreview" runat="server" CssClass="table table-hover table-bordered align-middle"
                                            AutoGenerateColumns="false" GridLines="None" EmptyDataText="No rows found in the uploaded file.">
                                            <Columns>
                                                <asp:BoundField DataField="RowNumber" HeaderText="#" ItemStyle-Width="40" />
                                                <asp:BoundField DataField="FullName" HeaderText="Full Name" />
                                                <asp:BoundField DataField="Email" HeaderText="Email" />
                                                <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                                                <asp:BoundField DataField="Gender" HeaderText="Gender" />
                                                <asp:BoundField DataField="DOBDisplay" HeaderText="DOB" />
                                                <asp:BoundField DataField="LocationDisplay" HeaderText="Location" />
                                                <asp:TemplateField HeaderText="Status">
                                                    <ItemTemplate>
                                                        <%# (bool)Eval("IsValid")
                                                            ? "<span class='import-status-ok'><i class=\"bi bi-check-circle-fill\"></i> Ready</span>"
                                                            : "<span class='import-status-bad'><i class=\"bi bi-x-circle-fill\"></i> Skipped</span>" %>
                                                        <div class="import-error-cell"><%# Eval("ErrorMessage") %></div>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>

                                    <div class="d-flex flex-wrap gap-2 justify-content-end mt-2">
                                        <asp:Button ID="btnCancelImport" runat="server" Text="Discard Preview" CssClass="btn btn-outline-secondary" OnClick="btnCancelImport_Click" CausesValidation="false" />
                                        <asp:Button ID="btnImportValidRows" runat="server" Text="Add Valid Rows to Pending List" CssClass="btn btn-success px-4" OnClick="btnImportValidRows_Click" CausesValidation="false" />
                                    </div>
                                </asp:Panel>
                            </div>
                        </div>
                    </div>

                    <!-- ==================== TEMPORARY LIST (shared by both tabs) ==================== -->
                    <div class="card shadow-sm border-0">
                        <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                            <h5 class="mb-0"><i class="bi bi-hourglass-split me-2"></i>Pending Records (not yet saved)</h5>
                            <asp:Label ID="lblTempCount" runat="server" CssClass="badge badge-pending" />
                        </div>
                        <div class="card-body">
                            <div class="table-responsive">
                                <asp:GridView ID="gvTemp" runat="server" CssClass="table table-hover table-bordered align-middle"
                                    AutoGenerateColumns="false" GridLines="None" EmptyDataText="No pending records yet. Add a student above or import a file."
                                    DataKeyNames="RowKey" OnRowCommand="gvTemp_RowCommand">
                                    <Columns>
                                        <asp:BoundField DataField="FullName" HeaderText="Full Name" />
                                        <asp:BoundField DataField="Email" HeaderText="Email" />
                                        <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                                        <asp:BoundField DataField="Gender" HeaderText="Gender" />
                                        <asp:BoundField DataField="DOB" HeaderText="DOB" DataFormatString="{0:dd MMM yyyy}" />
                                        <asp:BoundField DataField="Location" HeaderText="Location" />
                                        <asp:TemplateField HeaderText="Remove" ItemStyle-Width="80">
                                            <ItemTemplate>
                                                <asp:LinkButton runat="server" CssClass="btn btn-outline-danger btn-sm" CommandName="RemoveRow" CommandArgument='<%# Eval("RowKey") %>' CausesValidation="false">
                                                    <i class="bi bi-trash3-fill"></i>
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </div>

                            <div class="d-flex justify-content-between mt-3">
                                <asp:Button ID="btnClearAll" runat="server" Text="Clear All Records" CssClass="btn btn-outline-secondary" OnClick="btnClearAll_Click" CausesValidation="false" OnClientClick="return confirm('Remove all pending records?');" />
                                <asp:Button ID="btnSaveAll" runat="server" Text="Save All" CssClass="btn btn-success px-4" OnClick="btnSaveAll_Click" CausesValidation="false" />
                            </div>
                        </div>
                    </div>

                </div>
            </div>
        </div>

        <!-- Bootstrap JS -->
        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <!-- intl-tel-input JS -->
        <script src="https://cdn.jsdelivr.net/npm/intl-tel-input@23.8.0/build/js/intlTelInput.min.js"></script>

        <script>
            var iti;

            function initIntlTelInput() {
                var input = document.querySelector("#<%= txtMobile.ClientID %>");
                if (!input || input.dataset.itiInit) return;
                input.dataset.itiInit = "1";
                iti = window.intlTelInput(input, {
                    initialCountry: "in",
                    separateDialCode: true,
                    utilsScript: "https://cdn.jsdelivr.net/npm/intl-tel-input@23.8.0/build/js/utils.js"
                });

                var form = document.getElementById("form1");
                form.addEventListener("submit", function () {
                    var hidden = document.querySelector("#<%= hdnFullMobile.ClientID %>");
                    if (iti && hidden) {
                        hidden.value = iti.getNumber();
                    }
                });
            }

            // ---- client-side duplicate-email check (server-side check still runs on Add / Save All) ----
            function checkEmailDuplicate() {
                var input = document.querySelector("#<%= txtEmail.ClientID %>");
                var warning = document.getElementById("emailDuplicateWarning");
                var email = input.value.trim();

                warning.classList.add("d-none");
                if (!email || email.indexOf("@") === -1) return;

                fetch("BulkRegister.aspx/CheckEmailExists", {
                    method: "POST",
                    headers: { "Content-Type": "application/json; charset=utf-8" },
                    body: JSON.stringify({ email: email })
                })
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    if (data.d) {
                        warning.classList.remove("d-none");
                    } else {
                        warning.classList.add("d-none");
                    }
                })
                .catch(function () { /* fail silently — server-side check is authoritative */ });
            }

            // ---- Tab switching between "Add Manually" and "Import from File" ----
            function showBulkTab(which) {
                var manual = document.getElementById("paneManual");
                var imp = document.getElementById("paneImport");
                var btnManual = document.getElementById("tabBtnManual");
                var btnImport = document.getElementById("tabBtnImport");

                if (which === "import") {
                    manual.style.display = "none";
                    imp.style.display = "";
                    btnImport.classList.add("active");
                    btnManual.classList.remove("active");
                } else {
                    manual.style.display = "";
                    imp.style.display = "none";
                    btnManual.classList.add("active");
                    btnImport.classList.remove("active");
                }
                try { sessionStorage.setItem("bulkActiveTab", which); } catch (e) { /* ignore */ }
            }

            // ---- Drag & drop niceties over the hidden ASP.NET FileUpload control ----
            function initFileDropZone() {
                var zone = document.getElementById("dropZone");
                var input = document.querySelector("#<%= fuBulkFile.ClientID %>");
                var chip = document.getElementById("fileNameChip");
                var chipText = document.getElementById("fileNameChipText");
                if (!zone || !input) return;

                function updateChip() {
                    if (input.files && input.files.length > 0) {
                        chipText.textContent = input.files[0].name;
                        chip.classList.remove("d-none");
                    } else {
                        chip.classList.add("d-none");
                    }
                }

                input.addEventListener("change", updateChip);

                ["dragenter", "dragover"].forEach(function (evt) {
                    zone.addEventListener(evt, function (e) {
                        e.preventDefault();
                        zone.classList.add("drag-over");
                    });
                });
                ["dragleave", "drop"].forEach(function (evt) {
                    zone.addEventListener(evt, function (e) {
                        e.preventDefault();
                        zone.classList.remove("drag-over");
                    });
                });
                zone.addEventListener("drop", function (e) {
                    if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                        input.files = e.dataTransfer.files;
                        updateChip();
                    }
                });

                updateChip();
            }

            // ---- Busy overlay while a postback (Preview / Import / Save All) is running ----
            function initBusyOverlay() {
                if (!(window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager)) return;
                var prm = Sys.WebForms.PageRequestManager.getInstance();
                var overlay = document.getElementById("busyOverlay");
                prm.add_beginRequest(function () { overlay.classList.add("active"); });
                prm.add_endRequest(function () { overlay.classList.remove("active"); });
            }

            function restoreActiveTab() {
                var saved = null;
                try { saved = sessionStorage.getItem("bulkActiveTab"); } catch (e) { /* ignore */ }
                if (saved === "import") showBulkTab("import");
            }

            document.addEventListener("DOMContentLoaded", function () {
                initIntlTelInput();
                initFileDropZone();
                initBusyOverlay();
                restoreActiveTab();
                var input = document.querySelector("#<%= txtEmail.ClientID %>");
                if (input) input.addEventListener("blur", checkEmailDuplicate);
            });
            if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    initIntlTelInput();
                });
            }
        </script>
    </form>
</body>
</html>
