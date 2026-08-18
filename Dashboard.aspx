<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudentRegistrationSystem.Dashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Dashboard | Advanced Student Registration System</title>

    <!-- Bootstrap 5 -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />
    <!-- intl-tel-input -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/intl-tel-input@23.8.0/build/css/intlTelInput.css" />

    <style>
        .profile-photo-lg {
            width: 128px; height: 128px; object-fit: cover; border-radius: 50%;
            border: 3px solid #fff; box-shadow: 0 0 0 2px #0d9488;
        }
        #photoPreviewEdit {
            width: 96px; height: 96px; object-fit: cover; border-radius: 10px;
            border: 2px dashed #c7cdd6; background: #fff;
        }
        .iti { width: 100%; }
        .profile-field-label { color:#78818c; font-size:.82rem; text-transform:uppercase; letter-spacing:.04em; font-weight: 700; }
        .profile-field-value { font-weight:700; font-size: 1rem; color: #111827; }

        /* Task 12: My Fees */
        .summary-tile { background: #fff; border: 1px solid #e6ece9; border-radius: 12px; padding: 14px 16px; height: 100%; }
        .summary-tile .label { font-size: .74rem; text-transform: uppercase; letter-spacing: .5px; color: #7c8b88; font-weight: 700; }
        .summary-tile .value { font-size: 1.3rem; font-weight: 700; color: #1f2d2b; }
        .summary-tile.outstanding .value { color: #dc3545; }
        .summary-tile.paid .value { color: #16a34a; }
        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; }
        .status-Paid { background: rgba(22,163,74,.15); color: #16a34a; }
        .status-PartiallyPaid { background: rgba(255,193,7,.2); color: #b8860b; }
        .status-Pending { background: rgba(13,110,253,.15); color: #0d6efd; }
        .status-Overdue { background: rgba(220,53,69,.15); color: #dc3545; }
        .status-Waived { background: rgba(108,117,125,.16); color: #6c757d; }
        .status-NoDuesGenerated { background: rgba(108,117,125,.16); color: #6c757d; }
        #myFeesCard .table td { vertical-align: middle; font-size: .9rem; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true" />

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <div>
                    <a href="Home.aspx" class="btn btn-outline-light btn-sm me-2"><i class="bi bi-house-door-fill me-1"></i>Home</a>
                    <a href="Display.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-people-fill me-1"></i>View Students</a>
                    <a href="#myFeesCard" class="btn btn-light btn-sm me-2"><i class="bi bi-cash-coin me-1"></i>My Fees</a>
                    <a href="#updateProfileCard" class="btn btn-light btn-sm me-2"><i class="bi bi-pencil-square me-1"></i>Edit Profile</a>
                    <a href="ChangePassword.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-key-fill me-1"></i>Change Password</a>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click" CausesValidation="false">
                        <i class="bi bi-box-arrow-right me-1"></i>Logout
                    </asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container mb-5">

            <asp:Panel ID="pnlSuccessMsg" runat="server" CssClass="alert alert-success d-none" role="alert"></asp:Panel>
            <asp:Panel ID="pnlErrorMsg" runat="server" CssClass="alert alert-danger d-none" role="alert"></asp:Panel>

            <div class="row justify-content-center">
                <div class="col-lg-9">

                    <!-- ==================== PROFILE OVERVIEW ==================== -->
                    <div class="card shadow-sm border-0 mb-4">
                        <div class="card-header card-header-gradient py-3">
                            <h4 class="mb-0"><i class="bi bi-person-badge-fill me-2"></i>My Profile</h4>
                        </div>
                        <div class="card-body p-4">
                            <div class="row g-4 align-items-center">
                                <div class="col-auto">
                                    <img id="imgProfilePhoto" runat="server" class="profile-photo-lg" alt="Profile Photo" />
                                </div>
                                <div class="col">
                                    <div class="row g-3">
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Student ID</div>
                                            <div class="profile-field-value"><asp:Literal ID="litStudentId" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Full Name</div>
                                            <div class="profile-field-value"><asp:Literal ID="litFullName" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Email Address</div>
                                            <div class="profile-field-value"><asp:Literal ID="litEmail" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Mobile Number</div>
                                            <div class="profile-field-value"><asp:Literal ID="litMobile" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Gender</div>
                                            <div class="profile-field-value"><asp:Literal ID="litGender" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Date of Birth</div>
                                            <div class="profile-field-value"><asp:Literal ID="litDOB" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Country</div>
                                            <div class="profile-field-value"><asp:Literal ID="litCountry" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">State</div>
                                            <div class="profile-field-value"><asp:Literal ID="litState" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">District</div>
                                            <div class="profile-field-value"><asp:Literal ID="litDistrict" runat="server" /></div>
                                        </div>
                                        <div class="col-12">
                                            <div class="profile-field-label">Address</div>
                                            <div class="profile-field-value"><asp:Literal ID="litAddress" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Registered On</div>
                                            <div class="profile-field-value"><asp:Literal ID="litRegisteredOn" runat="server" /></div>
                                        </div>
                                        <div class="col-md-4">
                                            <div class="profile-field-label">Last Login</div>
                                            <div class="profile-field-value"><asp:Literal ID="litLastLogin" runat="server" /></div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- ==================== MY FEES (Task 12) ==================== -->
                    <div class="card shadow-sm border-0 mb-4" id="myFeesCard">
                        <div class="card-header card-header-gradient py-3">
                            <h4 class="mb-0"><i class="bi bi-cash-coin me-2"></i>My Fees</h4>
                        </div>
                        <div class="card-body p-4">

                            <asp:Panel ID="pnlNoFees" runat="server" CssClass="text-center text-muted py-3" Visible="false">
                                <i class="bi bi-info-circle fs-3 d-block mb-2"></i>
                                <asp:Literal ID="litNoFeesMessage" runat="server" />
                            </asp:Panel>

                            <asp:Panel ID="pnlFeeSummary" runat="server" Visible="false">
                                <div class="row g-3 mb-3">
                                    <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Total Payable</div><div class="value">&#8377;<asp:Literal ID="litTotalPayable" runat="server" /></div></div></div>
                                    <div class="col-6 col-md-2"><div class="summary-tile paid"><div class="label">Amount Paid</div><div class="value">&#8377;<asp:Literal ID="litAmountPaid" runat="server" /></div></div></div>
                                    <div class="col-6 col-md-2"><div class="summary-tile outstanding"><div class="label">Outstanding</div><div class="value">&#8377;<asp:Literal ID="litOutstanding" runat="server" /></div></div></div>
                                    <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Late Fee</div><div class="value">&#8377;<asp:Literal ID="litLateFee" runat="server" /></div></div></div>
                                    <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Scholarship / Discount</div><div class="value">&#8377;<asp:Literal ID="litDiscount" runat="server" /></div></div></div>
                                    <div class="col-6 col-md-2"><div class="summary-tile"><div class="label">Net Payable</div><div class="value">&#8377;<asp:Literal ID="litNetPayable" runat="server" /></div></div></div>
                                </div>
                                <p class="mb-4"><span class="label text-muted small">Payment Status:</span> <asp:Literal ID="litPaymentStatus" runat="server" /></p>

                                <h6 class="fw-bold text-primary mb-3">Fee Head Breakdown</h6>
                                <div class="table-responsive">
                                    <asp:GridView ID="gvMyFees" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                                        GridLines="None" EmptyDataText="No fee heads yet.">
                                        <Columns>
                                            <asp:TemplateField HeaderText="Fee Type"><ItemTemplate><%# Eval("FeeTypeName") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Total Payable" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("GrossAmount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Scholarship / Discount" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("DiscountAmount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Amount Paid" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("AmountPaid", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Outstanding" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("OutstandingPrincipal", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Late Fee" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("LiveLateFeeOutstanding", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Due Date"><ItemTemplate><%# Eval("DueDate", "{0:dd-MMM-yyyy}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Status">
                                                <ItemTemplate><span class='badge-status status-<%# Eval("Status") %>'><%# Eval("Status") %></span></ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>

                                <h6 class="fw-bold text-primary mb-3 mt-4">Payment History</h6>
                                <div class="table-responsive">
                                    <asp:GridView ID="gvMyPayments" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle"
                                        GridLines="None" EmptyDataText="No payments recorded yet.">
                                        <Columns>
                                            <asp:TemplateField HeaderText="Receipt No."><ItemTemplate><%# Eval("TransactionRef") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Date"><ItemTemplate><%# Eval("PaymentDate", "{0:dd-MMM-yyyy}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Mode"><ItemTemplate><%# Eval("PaymentMode") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Amount" ItemStyle-CssClass="text-end"><ItemTemplate>&#8377;<%# Eval("Amount", "{0:N2}") %></ItemTemplate></asp:TemplateField>
                                            <asp:TemplateField HeaderText="Status"><ItemTemplate><%# Eval("Status") %></ItemTemplate></asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </asp:Panel>

                        </div>
                    </div>

                    <!-- ==================== UPDATE PROFILE ==================== -->
                    <div class="card shadow-sm border-0" id="updateProfileCard">
                        <div class="card-header card-header-gradient py-3">
                            <h4 class="mb-0"><i class="bi bi-pencil-square me-2"></i>Update Profile</h4>
                        </div>
                        <div class="card-body p-4">

                            <div class="row g-3">
                                <div class="col-md-6">
                                    <label class="form-label text-muted">Student ID</label>
                                    <asp:TextBox ID="txtEditStudentId" runat="server" CssClass="form-control" ReadOnly="true" />
                                </div>
                                <div class="col-md-6">
                                    <label class="form-label text-muted">Email Address</label>
                                    <asp:TextBox ID="txtEditEmail" runat="server" CssClass="form-control" ReadOnly="true" />
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Full Name <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtEditFullName" runat="server" CssClass="form-control" MaxLength="150" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEditFullName" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Full name is required." ValidationGroup="ProfileForm" />
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Mobile Number <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtEditMobile" runat="server" CssClass="form-control" />
                                    <asp:HiddenField ID="hdnEditFullMobile" runat="server" />
                                    <div class="form-text text-warning">
                                        <i class="bi bi-exclamation-triangle-fill"></i>
                                        Your password is your mobile number — changing it here also changes your login password.
                                    </div>
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEditMobile" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Mobile number is required." ValidationGroup="ProfileForm" />
                                </div>

                                <asp:UpdatePanel ID="upEditLocation" runat="server" UpdateMode="Conditional">
                                    <ContentTemplate>
                                        <div class="col-md-4">
                                            <label class="form-label">Country <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlEditCountry" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlEditCountry_SelectedIndexChanged" />
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">State <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlEditState" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlEditState_SelectedIndexChanged" />
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">District <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlEditDistrict" runat="server" CssClass="form-select" />
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>

                                <div class="col-12">
                                    <label class="form-label">Full Address</label>
                                    <asp:TextBox ID="txtEditAddress" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" />
                                </div>

                                <div class="col-12">
                                    <hr />
                                </div>

                                <div class="col-12 d-flex align-items-center gap-3">
                                    <img id="photoPreviewEdit" runat="server" alt="Preview" />
                                    <div class="flex-grow-1">
                                        <label class="form-label">Profile Photo</label>
                                        <asp:FileUpload ID="fuEditPhoto" runat="server" CssClass="form-control" onchange="previewEditPhoto(this)" />
                                        <div class="form-text">Upload a new photo to replace the current one. Allowed: .jpg, .jpeg, .png (max 2MB)</div>
                                        <asp:Label ID="lblPhotoError" runat="server" CssClass="text-danger small" />
                                    </div>
                                </div>

                                <div class="col-12 text-end mt-3">
                                    <asp:Button ID="btnUpdateProfile" runat="server" Text="Save Changes" CssClass="btn btn-primary px-4" OnClick="btnUpdateProfile_Click" ValidationGroup="ProfileForm" />
                                </div>
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
            var itiEdit;

            function initIntlTelInputEdit() {
                var input = document.querySelector("#<%= txtEditMobile.ClientID %>");
                if (!input || input.dataset.itiInit) return;
                input.dataset.itiInit = "1";
                itiEdit = window.intlTelInput(input, {
                    initialCountry: "in",
                    separateDialCode: true,
                    utilsScript: "https://cdn.jsdelivr.net/npm/intl-tel-input@23.8.0/build/js/utils.js"
                });

                var form = document.getElementById("form1");
                form.addEventListener("submit", function () {
                    var hidden = document.querySelector("#<%= hdnEditFullMobile.ClientID %>");
                    if (itiEdit && hidden) {
                        hidden.value = itiEdit.getNumber();
                    }
                });
            }

            function previewEditPhoto(input) {
                if (input.files && input.files[0]) {
                    var reader = new FileReader();
                    reader.onload = function (e) {
                        document.getElementById("<%= photoPreviewEdit.ClientID %>").src = e.target.result;
                    };
                    reader.readAsDataURL(input.files[0]);
                }
            }

            document.addEventListener("DOMContentLoaded", initIntlTelInputEdit);
            if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    initIntlTelInputEdit();
                });
            }
        </script>
    </form>
</body>
</html>
