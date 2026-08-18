<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="StudentRegistrationSystem.Login" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Login | Advanced Student Registration System</title>

    <!-- Bootstrap 5 -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />
    <!-- Shared auth-page look (login screens only) -->
    <link rel="stylesheet" href="Content/auth.css" />
</head>
<body class="auth-body">
    <form id="form1" runat="server" autocomplete="off">

        <div class="auth-wrap">
            <div class="auth-card">
                <div class="auth-card-header auth-header-student">
                    <span class="auth-badge">NEW INSTITUTE</span>
                    <h1>Student Login</h1>
                    <p>Sign in with your registered email and mobile number.</p>
                </div>

                <div class="auth-card-body">

                    <asp:Panel ID="pnlErrorMsg" runat="server" CssClass="alert alert-danger d-none" role="alert"></asp:Panel>

                    <div class="mb-3">
                        <label class="form-label">Email Address <span class="required-star">*</span></label>
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" placeholder="student@example.com" TextMode="Email" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Email is required." ValidationGroup="LoginForm" />
                    </div>

                    <div class="mb-3">
                        <label class="form-label">Password (10-digit Mobile Number) <span class="required-star">*</span></label>
                        <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password" placeholder="Enter your 10-digit mobile number" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Password is required." ValidationGroup="LoginForm" />
                    </div>

                    <div class="mb-3">
                        <label class="form-label">CAPTCHA Verification <span class="required-star">*</span></label>
                        <div class="d-flex align-items-center gap-2 mb-2">
                            <img id="captchaImg" class="captcha-img" src="CaptchaHandler.ashx" alt="CAPTCHA" />
                            <button type="button" class="btn btn-outline-secondary btn-sm" onclick="refreshCaptcha()" aria-label="Refresh CAPTCHA">
                                <i class="bi bi-arrow-clockwise"></i>
                            </button>
                        </div>
                        <asp:TextBox ID="txtCaptchaInput" runat="server" CssClass="form-control" placeholder="Enter the code shown above" autocomplete="off" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCaptchaInput" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Please enter the CAPTCHA code." ValidationGroup="LoginForm" />
                    </div>

                    <div class="d-grid">
                        <asp:Button ID="btnLogin" runat="server" Text="Login" CssClass="btn-auth-student" OnClick="btnLogin_Click" ValidationGroup="LoginForm" />
                    </div>

                    <div class="auth-footer-links mt-4">
                        New student? <a href="Register.aspx">Register here &rarr;</a>
                        <div class="mt-2">
                            <a href="AdminLogin.aspx" class="auth-link-admin">Admin Login &rarr;</a>
                        </div>
                        <div class="mt-2">
                            <a href="Home.aspx"><i class="bi bi-house-door-fill me-1"></i>Back to Home</a>
                        </div>
                    </div>

                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <script>
            function refreshCaptcha() {
                document.getElementById("captchaImg").src = "CaptchaHandler.ashx?t=" + new Date().getTime();
            }
        </script>
    </form>
</body>
</html>
