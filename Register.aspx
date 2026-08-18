<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="StudentRegistrationSystem.Register" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Registration | Advanced Student Registration System</title>

    <!-- Bootstrap 5 -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />
    <!-- intl-tel-input -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/intl-tel-input@23.8.0/build/css/intlTelInput.css" />

    <style>
        #photoPreview {
            width: 118px; height: 118px; object-fit: cover; border-radius: 12px;
            border: 2px dashed #c7cdd6; background: #fff;
        }
        .iti { width: 100%; }
        .otp-box input { text-align:center; font-weight:700; letter-spacing:4px; font-size: 1.1rem; }

        /* Task 11 (revised): Advertisement Banner — inline strip at the top of the
           registration form, replacing the old popup modal. */
        .ad-banner {
            position: relative;
            background: #fff;
            border: 1px solid #e6ece9;
            border-radius: var(--radius-lg, 14px);
            box-shadow: var(--shadow-card, 0 1px 3px rgba(0,0,0,.08));
            overflow: hidden;
        }
        .ad-banner-close {
            position: absolute; top: 10px; right: 10px; z-index: 5;
            background: rgba(255,255,255,.92); border-radius: 50%; padding: 6px; opacity: 1;
            box-shadow: 0 2px 8px rgba(0,0,0,.15); border: none;
        }
        .ad-banner-row {
            display: flex; align-items: stretch;
        }
        .ad-banner-image {
            width: 220px; min-width: 220px; height: 140px; object-fit: cover; background: #f4f7f6;
        }
        .ad-banner-text {
            padding: 16px 46px 16px 20px; display: flex; flex-direction: column; justify-content: center;
        }
        .ad-banner-text h6 { font-weight: 800; color: #111827; margin-bottom: 6px; }
        .ad-banner-text p { color: #4b5563; margin-bottom: 0; font-size: .9rem; white-space: pre-line; }
        #adBannerCarousel .carousel-indicators { position: static; margin: 0; padding: 6px 0 2px; }
        #adBannerCarousel .carousel-indicators [data-bs-target] {
            width: 7px; height: 7px; border-radius: 50%; background-color: #b9c4c0; opacity: 1;
        }
        #adBannerCarousel .carousel-indicators .active { background-color: var(--brand-dark, #0f4c46); }
        #adBannerCarousel .carousel-control-prev, #adBannerCarousel .carousel-control-next { width: 7%; }
        #adBannerCarousel .carousel-control-prev-icon, #adBannerCarousel .carousel-control-next-icon {
            background-color: rgba(0,0,0,.35); border-radius: 50%; padding: 10px; background-size: 45%;
        }
        @media (max-width: 575px) {
            .ad-banner-row { flex-direction: column; }
            .ad-banner-image { width: 100%; min-width: 100%; height: 160px; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true" EnablePageMethods="true" />

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <div>
                    <a href="Home.aspx" class="btn btn-outline-light btn-sm me-2"><i class="bi bi-house-door-fill me-1"></i>Home</a>
                    <a href="BulkRegister.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-people-fill me-1"></i>Bulk Registration</a>
                    <a href="Display.aspx" class="btn btn-light btn-sm me-2"><i class="bi bi-card-list me-1"></i>View Students</a>
                    <a href="Login.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-box-arrow-in-right me-1"></i>Student Login</a>
                </div>
            </div>
        </nav>

        <div class="container mb-5">
            <div class="row justify-content-center">
                <div class="col-lg-9">

                    <!-- Task 11 (revised): Advertisement Banner — shown inline above the
                         registration form, driven entirely by data managed through
                         Admin Panel > Advertisements. Rendered only when the global switch
                         is on AND at least one advertisement is currently active. -->
                    <asp:Panel ID="pnlAdBanner" runat="server" CssClass="ad-banner mb-4" Visible="false">
                        <button type="button" class="btn-close ad-banner-close" aria-label="Dismiss advertisement"
                            onclick="this.closest('.ad-banner').classList.add('d-none')"></button>
                        <div id="adBannerCarousel" class="carousel slide" data-bs-ride="carousel" data-bs-interval="6000">
                            <asp:Panel ID="pnlAdIndicators" runat="server" CssClass="carousel-indicators" Visible="false">
                                <asp:Repeater ID="rptAdIndicators" runat="server">
                                    <ItemTemplate>
                                        <button type="button" data-bs-target="#adBannerCarousel" data-bs-slide-to='<%# Container.ItemIndex %>'
                                            class='<%# Container.ItemIndex == 0 ? "active" : "" %>'
                                            aria-current='<%# Container.ItemIndex == 0 ? "true" : "false" %>'
                                            aria-label='<%# "Advertisement " + (Container.ItemIndex + 1) %>'></button>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </asp:Panel>
                            <div class="carousel-inner">
                                <asp:Repeater ID="rptAds" runat="server" OnItemDataBound="rptAds_ItemDataBound">
                                    <ItemTemplate>
                                        <div class='<%# "carousel-item" + (Container.ItemIndex == 0 ? " active" : "") %>'>
                                            <div class="ad-banner-row">
                                                <asp:Image ID="imgAd" runat="server" CssClass="ad-banner-image" />
                                                <div class="ad-banner-text">
                                                    <h6><%# Eval("Title") %></h6>
                                                    <p><%# Eval("Description") %></p>
                                                </div>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                            <asp:Panel ID="pnlAdControls" runat="server" Visible="false">
                                <button class="carousel-control-prev" type="button" data-bs-target="#adBannerCarousel" data-bs-slide="prev">
                                    <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                                    <span class="visually-hidden">Previous</span>
                                </button>
                                <button class="carousel-control-next" type="button" data-bs-target="#adBannerCarousel" data-bs-slide="next">
                                    <span class="carousel-control-next-icon" aria-hidden="true"></span>
                                    <span class="visually-hidden">Next</span>
                                </button>
                            </asp:Panel>
                        </div>
                    </asp:Panel>

                    <div class="card shadow-sm border-0">
                        <div class="card-header card-header-gradient py-3">
                            <h4 class="mb-0"><i class="bi bi-person-plus-fill me-2"></i>New Student Registration</h4>
                        </div>
                        <div class="card-body p-4">

                            <!-- Global status messages -->
                            <asp:Panel ID="pnlSuccessMsg" runat="server" CssClass="alert alert-success d-none" role="alert"></asp:Panel>
                            <asp:Panel ID="pnlErrorMsg" runat="server" CssClass="alert alert-danger d-none" role="alert"></asp:Panel>

                            <div class="row mb-4">
                                <div class="col-md-4">
                                    <label class="form-label text-muted">Student ID (Preview)</label>
                                    <asp:TextBox ID="txtStudentIdPreview" runat="server" CssClass="form-control" ReadOnly="true" />
                                    <div class="form-text">Actual ID is generated automatically on save.</div>
                                </div>
                            </div>

                            <div class="row g-3">
                                <div class="col-md-6">
                                    <label class="form-label"><span class="step-badge">1</span>Full Name <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" placeholder="e.g. Aarav Sharma" MaxLength="150" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFullName" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Full name is required." ValidationGroup="RegForm" />
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Gender <span class="required-star">*</span></label>
                                    <asp:DropDownList ID="ddlGender" runat="server" CssClass="form-select">
                                        <asp:ListItem Text="Select Gender" Value="" />
                                        <asp:ListItem Text="Male" Value="Male" />
                                        <asp:ListItem Text="Female" Value="Female" />
                                        <asp:ListItem Text="Other" Value="Other" />
                                    </asp:DropDownList>
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlGender" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Please select gender." ValidationGroup="RegForm" />
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Date of Birth <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtDOB" runat="server" CssClass="form-control" TextMode="Date" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDOB" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Date of birth is required." ValidationGroup="RegForm" />
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Mobile Number <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtMobile" runat="server" CssClass="form-control" placeholder="Mobile number" />
                                    <asp:HiddenField ID="hdnFullMobile" runat="server" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMobile" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Mobile number is required." ValidationGroup="RegForm" />
                                </div>

                                <hr class="mt-4" />

                                <!-- Email + OTP Section -->
                                <div class="col-md-6">
                                    <label class="form-label"><span class="step-badge">2</span>Email Address <span class="required-star">*</span></label>
                                    <div class="input-group">
                                        <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" placeholder="student@example.com" TextMode="Email" />
                                        <asp:Button ID="btnSendOTP" runat="server" Text="Send OTP" CssClass="btn btn-outline-primary" OnClick="btnSendOTP_Click" CausesValidation="false" />
                                    </div>
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Email is required." ValidationGroup="RegForm" />
                                    <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Enter a valid email address." ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ValidationGroup="RegForm" />
                                    <span id="emailDuplicateWarning" class="text-danger small d-none"><i class="bi bi-exclamation-circle-fill"></i> A student is already registered with this email address.</span>
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Enter 6-digit OTP</label>
                                    <div class="input-group otp-box">
                                        <asp:TextBox ID="txtOTP" runat="server" CssClass="form-control" MaxLength="6" placeholder="••••••" Enabled="false" />
                                        <asp:Button ID="btnVerifyOTP" runat="server" Text="Verify OTP" CssClass="btn btn-success" OnClick="btnVerifyOTP_Click" CausesValidation="false" Enabled="false" />
                                        <asp:Button ID="btnResendOTP" runat="server" Text="Resend" CssClass="btn btn-outline-secondary" OnClick="btnResendOTP_Click" CausesValidation="false" Enabled="false" />
                                    </div>
                                    <asp:Panel ID="pnlOtpStatus" runat="server" CssClass="small mt-1"></asp:Panel>
                                </div>
                            </div>

                            <hr class="mt-4" />

                            <!-- Cascading Location dropdowns (AutoPostBack inside UpdatePanel) -->
                            <div class="row g-3">
                                <div class="col-12"><span class="step-badge">3</span><strong>Location Details</strong></div>

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

                            <hr class="mt-4" />

                            <!-- Task 12: Course / Academic Details (drives fee calculation) -->
                            <div class="row g-3">
                                <div class="col-12"><span class="step-badge">4</span><strong>Course &amp; Academic Details</strong>
                                    <div class="form-text mb-0">Used to work out your registration fee — you'll see it on your dashboard right after you register.</div>
                                </div>

                                <asp:UpdatePanel ID="upAcademic" runat="server" UpdateMode="Conditional">
                                    <ContentTemplate>
                                        <div class="col-md-4">
                                            <label class="form-label">Institute <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlInstitute" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlInstitute_SelectedIndexChanged" />
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">Course <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlCourse" runat="server" CssClass="form-select" />
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">Year / Semester <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlYearSemester" runat="server" CssClass="form-select" />
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">Academic Year <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlAcademicYear" runat="server" CssClass="form-select" />
                                        </div>
                                        <div class="col-md-4">
                                            <label class="form-label">Student Category <span class="required-star">*</span></label>
                                            <asp:DropDownList ID="ddlStudentCategory" runat="server" CssClass="form-select" />
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>

                            <hr class="mt-4" />

                            <!-- Photo Upload -->
                            <div class="row g-3 align-items-center">
                                <div class="col-12"><span class="step-badge">5</span><strong>Student Photo</strong></div>
                                <div class="col-auto">
                                    <img id="photoPreview" src="https://via.placeholder.com/110x110?text=Photo" alt="Preview" />
                                </div>
                                <div class="col">
                                    <asp:FileUpload ID="fuPhoto" runat="server" CssClass="form-control" onchange="previewPhoto(this)" />
                                    <div class="form-text">Allowed formats: .jpg, .jpeg, .png (max 2MB)</div>
                                    <asp:Label ID="lblPhotoError" runat="server" CssClass="text-danger small" />
                                </div>
                            </div>

                            <hr class="mt-4" />

                            <div class="d-flex justify-content-between align-items-center">
                                <asp:Label ID="lblVerifiedStatus" runat="server" CssClass="badge bg-secondary">Email Not Verified</asp:Label>
                                <asp:Button ID="btnRegister" runat="server" Text="Register Student" CssClass="btn btn-primary px-4" OnClick="btnRegister_Click" ValidationGroup="RegForm" />
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

                // keep the hidden field updated with the full E.164-ish number on submit
                var form = document.getElementById("form1");
                form.addEventListener("submit", function () {
                    var hidden = document.querySelector("#<%= hdnFullMobile.ClientID %>");
                    if (iti && hidden) {
                        hidden.value = iti.getNumber();
                    }
                });
            }

            function previewPhoto(input) {
                if (input.files && input.files[0]) {
                    var reader = new FileReader();
                    reader.onload = function (e) {
                        document.getElementById("photoPreview").src = e.target.result;
                    };
                    reader.readAsDataURL(input.files[0]);
                }
            }

            // ---- Task 6: client-side duplicate-email check (server-side check still runs on submit) ----
            function checkEmailDuplicate() {
                var input = document.querySelector("#<%= txtEmail.ClientID %>");
                var warning = document.getElementById("emailDuplicateWarning");
                var sendOtpBtn = document.querySelector("#<%= btnSendOTP.ClientID %>");
                var email = input.value.trim();

                warning.classList.add("d-none");
                if (!email || email.indexOf("@") === -1) return;

                fetch("Register.aspx/CheckEmailExists", {
                    method: "POST",
                    headers: { "Content-Type": "application/json; charset=utf-8" },
                    body: JSON.stringify({ email: email })
                })
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    var exists = data.d;
                    if (exists) {
                        warning.classList.remove("d-none");
                        if (sendOtpBtn) sendOtpBtn.disabled = true;
                    } else {
                        warning.classList.add("d-none");
                        if (sendOtpBtn) sendOtpBtn.disabled = false;
                    }
                })
                .catch(function () { /* fail silently — server-side check is authoritative */ });
            }

            document.addEventListener("DOMContentLoaded", function () {
                var input = document.querySelector("#<%= txtEmail.ClientID %>");
                if (input) input.addEventListener("blur", checkEmailDuplicate);
            });

            document.addEventListener("DOMContentLoaded", initIntlTelInput);
            // Re-init after partial UpdatePanel postbacks (cascading dropdowns) just in case
            if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    initIntlTelInput();
                });
            }
        </script>
    </form>
</body>
</html>
