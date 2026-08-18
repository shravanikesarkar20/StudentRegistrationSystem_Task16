<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="StudentRegistrationSystem.ChangePassword" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Change Password | Advanced Student Registration System</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />


</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <a href="Dashboard.aspx" class="btn btn-light btn-sm"><i class="bi bi-arrow-left me-1"></i>Back to Dashboard</a>
            </div>
        </nav>

        <div class="container mb-5">
            <div class="row justify-content-center">
                <div class="col-lg-5">
                    <div class="card shadow-sm border-0">
                        <div class="card-header card-header-gradient py-3">
                            <h4 class="mb-0"><i class="bi bi-key-fill me-2"></i>Change Password</h4>
                        </div>
                        <div class="card-body p-4">

                            <asp:Panel ID="pnlSuccessMsg" runat="server" CssClass="alert alert-success d-none" role="alert"></asp:Panel>
                            <asp:Panel ID="pnlErrorMsg" runat="server" CssClass="alert alert-danger d-none" role="alert"></asp:Panel>

                            <p class="text-muted small">
                                <i class="bi bi-info-circle-fill"></i>
                                Your password is your registered mobile number, so changing it here also
                                updates the mobile number on your profile.
                            </p>

                            <div class="mb-3">
                                <label class="form-label">Current Password <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtCurrentPassword" runat="server" CssClass="form-control" TextMode="Password" autocomplete="off" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCurrentPassword" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Current password is required." ValidationGroup="PwdForm" />
                            </div>

                            <div class="mb-3">
                                <label class="form-label">New Password (10-digit mobile number) <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtNewPassword" runat="server" CssClass="form-control" TextMode="Password" autocomplete="off" MaxLength="10" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNewPassword" CssClass="text-danger small" Display="Dynamic" ErrorMessage="New password is required." ValidationGroup="PwdForm" />
                                <asp:RegularExpressionValidator runat="server" ControlToValidate="txtNewPassword" CssClass="text-danger small" Display="Dynamic" ErrorMessage="New password must be exactly 10 digits." ValidationExpression="^\d{10}$" ValidationGroup="PwdForm" />
                            </div>

                            <div class="mb-3">
                                <label class="form-label">Confirm New Password <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtConfirmPassword" runat="server" CssClass="form-control" TextMode="Password" autocomplete="off" MaxLength="10" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtConfirmPassword" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Please confirm the new password." ValidationGroup="PwdForm" />
                                <asp:CompareValidator runat="server" ControlToValidate="txtConfirmPassword" ControlToCompare="txtNewPassword" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Passwords do not match." ValidationGroup="PwdForm" />
                            </div>

                            <div class="d-grid">
                                <asp:Button ID="btnChangePassword" runat="server" Text="Update Password" CssClass="btn btn-primary" OnClick="btnChangePassword_Click" ValidationGroup="PwdForm" />
                            </div>

                        </div>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    </form>
</body>
</html>
