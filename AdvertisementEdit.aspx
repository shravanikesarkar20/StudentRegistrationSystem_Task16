<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdvertisementEdit.aspx.cs" Inherits="StudentRegistrationSystem.AdvertisementEdit" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><asp:Literal ID="litPageTitle" runat="server" Text="New Advertisement" /> | Admin Panel</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }

        #adImagePreview {
            width: 100%; max-width: 360px; height: 180px; object-fit: cover; border-radius: 12px;
            border: 2px dashed #c7cdd6; background: #fff; display: block;
        }
        .form-switch .form-check-input { width: 2.75em; height: 1.5em; cursor: pointer; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:HiddenField ID="hdnAdvertisementId" runat="server" Value="0" />

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-shield-lock-fill me-2"></i>Admin Panel</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="AdminDashboard.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a>
                    <a href="BannerManagement.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-images me-1"></i>Home Banners</a>
                    <a href="AdvertisementManagement.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-megaphone-fill me-1"></i>Advertisements</a>
                    <span class="badge bg-light text-primary py-2 px-3"><i class="bi bi-person-circle me-1"></i><asp:Literal ID="litAdminName" runat="server" /></span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container mb-5">
            <nav aria-label="breadcrumb" class="mb-3">
                <ol class="breadcrumb small">
                    <li class="breadcrumb-item"><a href="AdvertisementManagement.aspx">Advertisements</a></li>
                    <li class="breadcrumb-item active"><asp:Literal ID="litBreadcrumb" runat="server" Text="New Advertisement" /></li>
                </ol>
            </nav>

            <div class="row justify-content-center">
                <div class="col-lg-9">
                    <div class="card shadow-sm border-0">
                        <div class="card-header card-header-gradient py-3">
                            <h4 class="mb-0"><i class="bi bi-megaphone-fill me-2"></i><asp:Literal ID="litFormHeading" runat="server" Text="Create Advertisement" /></h4>
                        </div>
                        <div class="card-body p-4">

                            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert d-none py-2 small" role="alert"></asp:Panel>

                            <div class="row g-3">
                                <div class="col-md-8">
                                    <label class="form-label">Advertisement Title <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" MaxLength="200" placeholder="e.g. Admissions Open for 2026-27" />
                                    <asp:RequiredFieldValidator ID="rfvTitle" runat="server" ControlToValidate="txtTitle" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Advertisement title is required." ValidationGroup="AdForm" />
                                </div>

                                <div class="col-md-4">
                                    <label class="form-label">Display Order <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtDisplayOrder" runat="server" CssClass="form-control" TextMode="Number" placeholder="1" />
                                    <asp:RequiredFieldValidator ID="rfvOrder" runat="server" ControlToValidate="txtDisplayOrder" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Display order is required." ValidationGroup="AdForm" />
                                    <asp:RangeValidator ID="rvOrder" runat="server" ControlToValidate="txtDisplayOrder" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Display order must be a whole number ≥ 0." Type="Integer" MinimumValue="0" MaximumValue="999999" ValidationGroup="AdForm" />
                                    <div class="form-text">Lower numbers show first when multiple ads are active.</div>
                                </div>

                                <div class="col-12">
                                    <label class="form-label">Description <span class="required-star">*</span></label>
                                    <asp:TextBox ID="txtDescription" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" MaxLength="1000" placeholder="Detailed advertisement description shown in the modal..." />
                                    <asp:RequiredFieldValidator ID="rfvDescription" runat="server" ControlToValidate="txtDescription" CssClass="text-danger small" Display="Dynamic" ErrorMessage="Description is required." ValidationGroup="AdForm" />
                                </div>

                                <div class="col-md-6">
                                    <label class="form-label">Advertisement Image / Banner</label>
                                    <asp:FileUpload ID="fuImage" runat="server" CssClass="form-control" onchange="previewAdImage(this)" />
                                    <div class="form-text">JPG, JPEG, PNG, GIF or WEBP. Leave empty to keep the current image when editing.</div>
                                    <asp:Label ID="lblImageError" runat="server" CssClass="text-danger small d-block mt-1" />
                                </div>
                                <div class="col-md-6">
                                    <label class="form-label">Preview</label>
                                    <img id="adImagePreview" runat="server" src="https://via.placeholder.com/360x180?text=No+Image" alt="Advertisement preview" />
                                </div>

                                <div class="col-12">
                                    <div class="form-check form-switch">
                                        <asp:CheckBox ID="chkIsActive" runat="server" CssClass="form-check-input" Checked="true" />
                                        <label class="form-check-label fw-semibold">Active (advertisement will appear on the Student Registration page modal)</label>
                                    </div>
                                </div>
                            </div>

                            <hr class="mt-4" />

                            <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                                <a href="AdvertisementManagement.aspx" class="btn btn-outline-secondary px-4"><i class="bi bi-arrow-left me-1"></i>Back to List</a>
                                <div class="d-flex gap-2">
                                    <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-outline-secondary px-4" OnClick="btnReset_Click" CausesValidation="false" />
                                    <asp:Button ID="btnSave" runat="server" Text="Save Advertisement" CssClass="btn btn-primary px-4" OnClick="btnSave_Click" ValidationGroup="AdForm" />
                                </div>
                            </div>

                        </div>
                    </div>
                </div>
            </div>
        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
        <script>
            function previewAdImage(input) {
                if (input.files && input.files[0]) {
                    var reader = new FileReader();
                    reader.onload = function (e) {
                        document.getElementById("adImagePreview").src = e.target.result;
                    };
                    reader.readAsDataURL(input.files[0]);
                }
            }
        </script>
    </form>
</body>
</html>
