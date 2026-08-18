<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="InstituteRegister.aspx.cs" Inherits="StudentRegistrationSystem.InstituteRegister" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Institute Registration | Advanced Student Registration System</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />
    <!-- Shared button styling (btn-auth-institute) -->
    <link rel="stylesheet" href="Content/auth.css" />

    <style>
        body { background: #f4f7f6; }
        .reg-header {
            background: linear-gradient(135deg, #3730a3 0%, #6366f1 100%);
            color: #fff; border-radius: 18px 18px 0 0; padding: 28px 32px;
        }
        .reg-header .badge-pill {
            display: inline-block; background: rgba(255,255,255,.94); color: #4338ca;
            font-size: .72rem; font-weight: 800; letter-spacing: .07em; padding: 5px 14px;
            border-radius: 999px; margin-bottom: 12px;
        }
        .step-badge {
            display: inline-flex; align-items: center; justify-content: center;
            width: 26px; height: 26px; border-radius: 50%; background: #4338ca; color: #fff;
            font-size: .8rem; font-weight: 800; margin-right: 8px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container py-5" style="max-width: 760px;">
            <div class="card shadow-sm border-0">
                <div class="reg-header">
                    <span class="badge-pill">INSTITUTE ONBOARDING</span>
                    <h2 class="mb-1">Register Your Institute</h2>
                    <p class="mb-0">Submit your institute's details. Once an administrator approves your
                        submission, your institute will appear in the Student Registration form.</p>
                </div>

                <div class="card-body p-4 p-md-5">

                    <asp:Panel ID="pnlErrorMsg" runat="server" CssClass="alert alert-danger d-none" role="alert"></asp:Panel>

                    <asp:Panel ID="pnlSuccess" runat="server" CssClass="text-center py-4" Visible="false">
                        <i class="bi bi-check-circle-fill text-success" style="font-size: 3.5rem;"></i>
                        <h4 class="mt-3">Registration Submitted</h4>
                        <p class="text-muted">
                            Thank you! Your institute has been submitted for review. You'll be able to select it
                            in the Student Registration form as soon as an administrator approves it.
                        </p>
                        <a href="Home.aspx" class="btn btn-outline-primary btn-sm mt-2"><i class="bi bi-house-door-fill me-1"></i>Back to Home</a>
                    </asp:Panel>

                    <asp:Panel ID="pnlForm" runat="server">

                        <div class="row g-3">
                            <div class="col-12"><span class="step-badge">1</span><strong>Institute Details</strong></div>

                            <div class="col-md-8">
                                <label class="form-label">Institute Name <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtInstituteName" runat="server" CssClass="form-control" placeholder="e.g. XYZ College of Engineering" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtInstituteName" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Institute Name is required." ValidationGroup="InstForm" />
                            </div>

                            <div class="col-md-4">
                                <label class="form-label">Student Capacity <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtCapacity" runat="server" CssClass="form-control" TextMode="Number" placeholder="e.g. 1200" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCapacity" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Capacity is required." ValidationGroup="InstForm" />
                                <asp:RangeValidator runat="server" ControlToValidate="txtCapacity" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Capacity must be a positive number." ValidationGroup="InstForm" MinimumValue="1" MaximumValue="1000000" Type="Integer" />
                            </div>

                            <div class="col-12">
                                <label class="form-label">Address</label>
                                <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" placeholder="Street / Area" />
                            </div>

                            <div class="col-md-6">
                                <label class="form-label">City <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtCity" runat="server" CssClass="form-control" placeholder="e.g. Kolhapur" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCity" CssClass="text-danger small" Display="Dynamic" ErrorMessage="City is required." ValidationGroup="InstForm" />
                            </div>

                            <div class="col-md-6">
                                <label class="form-label">Website</label>
                                <asp:TextBox ID="txtWebsite" runat="server" CssClass="form-control" placeholder="https://..." />
                            </div>
                        </div>

                        <hr class="mt-4" />

                        <div class="row g-3">
                            <div class="col-12"><span class="step-badge">2</span><strong>Contact Details</strong></div>

                            <div class="col-md-6">
                                <label class="form-label">Contact Email <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtContactEmail" runat="server" CssClass="form-control" TextMode="Email" placeholder="office@institute.edu" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtContactEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Contact Email is required." ValidationGroup="InstForm" />
                                <asp:RegularExpressionValidator runat="server" ControlToValidate="txtContactEmail" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Enter a valid email address." ValidationGroup="InstForm" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" />
                            </div>

                            <div class="col-md-6">
                                <label class="form-label">Contact Phone <span class="required-star">*</span></label>
                                <asp:TextBox ID="txtContactPhone" runat="server" CssClass="form-control" placeholder="+91 20 1234 5678" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtContactPhone" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Contact Phone is required." ValidationGroup="InstForm" />
                            </div>
                        </div>

                        <hr class="mt-4" />

                        <div class="row g-3">
                            <div class="col-12"><span class="step-badge">3</span><strong>Courses Offered</strong>
                                <div class="form-text mb-0">One course per line (or comma-separated) - e.g. "B.Tech Computer Science".
                                    These become selectable in the student Registration form once you're approved.</div>
                            </div>
                            <div class="col-12">
                                <asp:TextBox ID="txtCourses" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" placeholder="B.Tech Computer Science&#10;B.Tech Mechanical&#10;Diploma Electronics" />
                                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCourses" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Please list at least one course your institute offers." ValidationGroup="InstForm" />
                            </div>
                        </div>

                        <div class="d-grid mt-4">
                            <asp:Button ID="btnSubmit" runat="server" Text="Submit for Approval" CssClass="btn-auth-institute" OnClick="btnSubmit_Click" ValidationGroup="InstForm" />
                        </div>

                        <div class="text-center mt-3">
                            <a href="Home.aspx" class="small text-muted">&larr; Back to Home</a>
                        </div>

                    </asp:Panel>

                </div>
            </div>
        </div>
    </form>
</body>
</html>
