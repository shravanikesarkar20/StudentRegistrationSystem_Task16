<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminLogin.aspx.cs" Inherits="StudentRegistrationSystem.AdminLogin" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Admin Login | Advanced Student Registration System</title>

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

    <div class="auth-wrap">
        <div class="auth-card">
            <div class="auth-card-header auth-header-admin">
                <span class="auth-badge">NEW INSTITUTE</span>
                <h1>Admin Login</h1>
                <p>Student Registration System &mdash; Administration.</p>
            </div>

            <div class="auth-card-body">

                <% if (!string.IsNullOrEmpty(ErrorMessage)) { %>
                    <div class="alert alert-danger py-2 small" role="alert">
                        <i class="bi bi-exclamation-triangle-fill me-1"></i><%= HttpUtility.HtmlEncode(ErrorMessage) %>
                    </div>
                <% } %>

                <form method="get" action="AdminLogin.aspx" autocomplete="off" novalidate>
                    <input type="hidden" name="login" value="1" />

                    <div class="mb-3">
                        <label class="form-label" for="username">Username <span class="required-star">*</span></label>
                        <input type="text" class="form-control" id="username" name="username"
                               placeholder="Enter your admin username" required
                               value="<%= HttpUtility.HtmlEncode(PostedUsername ?? "") %>" />
                    </div>

                    <div class="mb-3">
                        <label class="form-label" for="password">Password <span class="required-star">*</span></label>
                        <input type="password" class="form-control" id="password" name="password"
                               placeholder="Enter your password" required autocomplete="new-password" />
                    </div>

                    <div class="mb-3">
                        <label class="form-label" for="captcha">CAPTCHA Verification <span class="required-star">*</span></label>
                        <div class="d-flex align-items-center gap-2 mb-2">
                            <img id="captchaImg" class="captcha-img" src="CaptchaHandler.ashx" alt="CAPTCHA" />
                            <button type="button" class="btn btn-outline-secondary btn-sm" onclick="refreshCaptcha()" aria-label="Refresh CAPTCHA">
                                <i class="bi bi-arrow-clockwise"></i>
                            </button>
                        </div>
                        <input type="text" class="form-control" id="captcha" name="captcha"
                               placeholder="Enter the code shown above" required autocomplete="off" />
                    </div>

                    <div class="d-grid">
                        <button type="submit" class="btn btn-auth-admin">Login</button>
                    </div>
                </form>

                <div class="text-center mt-4">
                    <a href="Login.aspx" class="auth-link"><i class="bi bi-arrow-left me-1"></i>Back to Student Login</a>
                    <div class="mt-2">
                        <a href="Home.aspx" class="auth-link"><i class="bi bi-house-door-fill me-1"></i>Back to Home</a>
                    </div>
                </div>

            </div>
        </div>
    </div>

    <script>
        function refreshCaptcha() {
            document.getElementById("captchaImg").src = "CaptchaHandler.ashx?t=" + new Date().getTime();
        }
    </script>
</body>
</html>
