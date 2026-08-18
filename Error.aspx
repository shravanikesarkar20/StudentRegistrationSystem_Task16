<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="StudentRegistrationSystem.ErrorPage" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Something Went Wrong | Advanced Student Registration System</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <link rel="stylesheet" href="Content/auth.css" />
</head>
<body class="auth-body">
    <form id="form1" runat="server">
        <div class="auth-wrap">
            <div class="auth-card">
                <div class="auth-card-header auth-header-admin">
                    <span class="auth-badge">NEW INSTITUTE</span>
                    <h1><i class="bi bi-exclamation-octagon-fill me-1"></i> Oops</h1>
                    <p>Something went wrong on our end.</p>
                </div>
                <div class="auth-card-body text-center">
                    <p class="text-muted mb-4">
                        We hit an unexpected error while processing your request. Nothing was lost —
                        please try again, and contact the administration office if the problem continues.
                    </p>
                    <div class="d-grid gap-2">
                        <a href="Login.aspx" class="btn-auth-student text-center py-2">Back to Student Login</a>
                        <a href="AdminLogin.aspx" class="auth-link mt-2">Admin Login &rarr;</a>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
