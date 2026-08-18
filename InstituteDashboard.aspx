<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="InstituteDashboard.aspx.cs" Inherits="StudentRegistrationSystem.InstituteDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Institute Dashboard | Advanced Student Registration System</title>

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        body { background: #f4f7f6; }

        .stat-card {
            background: #fff; border: 1px solid #e6ece9; border-radius: 18px;
            padding: 18px 20px; height: 100%;
        }
        .stat-card .stat-icon {
            width: 44px; height: 44px; border-radius: 12px;
            display: inline-flex; align-items: center; justify-content: center;
            font-size: 20px; margin-bottom: 10px;
        }
        .stat-card .stat-value { font-size: 1.5rem; font-weight: 800; color: #111827; margin: 0; line-height: 1.2; word-break: break-word; }
        .stat-card .stat-label { font-size: .82rem; color: #78818c; margin-top: 6px; font-weight: 600; }

        .icon-indigo { background: rgba(99,102,241,.14); color: #4338ca; }
        .icon-teal   { background: rgba(45,212,191,.18); color: #0f766e; }
        .icon-green  { background: rgba(22,163,74,.12);  color: #16a34a; }
        .icon-gray   { background: rgba(108,117,125,.12);color: #6c757d; }

        .badge-status { font-size: .76rem; padding: .4em .75em; border-radius: 20px; font-weight: 700; letter-spacing: .2px; }
        .badge-active   { background: rgba(22,163,74,.15); color: #16a34a; }
        .badge-inactive { background: rgba(225,29,72,.15); color: #e11d48; }

        /* Clickable module cards */
        .module-card {
            display: block; width: 100%; text-align: left; background: #fff;
            border: 1.5px solid #e6ece9; border-radius: 16px; padding: 18px;
            cursor: pointer; transition: box-shadow .15s ease, border-color .15s ease, transform .1s ease;
        }
        .module-card:hover { box-shadow: 0 8px 22px rgba(0,0,0,.08); transform: translateY(-2px); border-color: #c7d2fe; }
        .module-card-selected { border-color: #4338ca; box-shadow: 0 8px 22px rgba(67,56,202,.18); background: #f5f5ff; }
        .module-card .module-icon {
            width: 46px; height: 46px; border-radius: 12px; background: rgba(99,102,241,.14); color: #4338ca;
            display: inline-flex; align-items: center; justify-content: center; font-size: 22px; margin-bottom: 10px;
        }
        .module-card .module-name { font-weight: 800; color: #1f2937; font-size: 1rem; margin: 0; }
        .module-card .module-cta { font-size: .8rem; color: #4338ca; font-weight: 700; margin-top: 4px; }

        .empty-state { padding: 40px 20px; text-align: center; color: #b9c4c1; }

        .table thead th {
            background: #eef4f2; color: #395550; font-size: .78rem; text-transform: uppercase;
            letter-spacing: .5px; border-bottom: 2px solid #dde7e4;
        }
        .table td { vertical-align: middle; font-size: .93rem; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar navbar-dark mb-4" style="background: linear-gradient(135deg, #3730a3, #6366f1);">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-grid-1x2-fill me-2"></i>Institute Dashboard</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <span class="badge bg-light text-dark py-2 px-3">
                        <i class="bi bi-building me-1"></i><asp:Literal ID="litInstName" runat="server" />
                        &nbsp;(<asp:Literal ID="litInstId" runat="server" />)
                    </span>
                    <asp:LinkButton ID="btnLogout" runat="server" CssClass="btn btn-outline-light btn-sm" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-1"></i>Logout</asp:LinkButton>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5">

            <!-- Institute info + module count -->
            <div class="row g-3 mb-4">
                <div class="col-6 col-md-3">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-indigo"><i class="bi bi-building"></i></div>
                        <p class="stat-value" style="font-size:1.05rem;"><asp:Literal ID="litInstName2" runat="server" /></p>
                        <p class="stat-label">Institute Name</p>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-teal"><i class="bi bi-hash"></i></div>
                        <p class="stat-value"><asp:Literal ID="litInstId2" runat="server" /></p>
                        <p class="stat-label">Institute Id</p>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-green"><i class="bi bi-shield-check"></i></div>
                        <p class="stat-value"><asp:Label ID="lblStatusBadge" runat="server" CssClass="badge-status" /></p>
                        <p class="stat-label">Institute Status</p>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="stat-card shadow-sm">
                        <div class="stat-icon icon-gray"><i class="bi bi-collection-fill"></i></div>
                        <p class="stat-value"><asp:Literal ID="litModuleCount" runat="server">0</asp:Literal></p>
                        <p class="stat-label">Active Modules</p>
                    </div>
                </div>
            </div>

            <!-- Active modules -->
            <div class="card shadow-sm border-0 mb-4">
                <div class="card-header card-header-gradient py-3">
                    <h4 class="mb-0"><i class="bi bi-grid-3x3-gap-fill me-2"></i>Active Modules</h4>
                </div>
                <div class="card-body p-3 p-md-4">

                    <asp:Panel ID="pnlEmptyModules" runat="server" CssClass="empty-state" Visible="false">
                        <i class="bi bi-inbox"></i>
                        No active modules have been assigned to your institute yet. Please contact the administrator.
                    </asp:Panel>

                    <div class="row g-3">
                        <asp:Repeater ID="rptModules" runat="server" OnItemCommand="rptModules_ItemCommand">
                            <ItemTemplate>
                                <div class="col-6 col-md-4 col-lg-3">
                                    <asp:LinkButton ID="btnModule" runat="server"
                                        CssClass='<%# GetModuleCardClass(Eval("modulename")) %>'
                                        CommandName="SelectModule" CommandArgument='<%# Eval("modulename") %>'>
                                        <div class="module-icon"><i class='<%# "bi " + GetModuleIcon(Eval("modulename")) %>'></i></div>
                                        <p class="module-name"><%# Eval("modulename") %></p>
                                        <p class="module-cta">View details <i class="bi bi-arrow-right"></i></p>
                                    </asp:LinkButton>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                </div>
            </div>

            <!-- Module detail (dynamic, per selected module) -->
            <asp:Panel ID="pnlNoSelection" runat="server" CssClass="card shadow-sm border-0">
                <div class="card-body empty-state">
                    <i class="bi bi-cursor-fill"></i>
                    Click any active module above to view its details.
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlModuleDetail" runat="server" CssClass="card shadow-sm border-0">
                <div class="card-header card-header-gradient py-3">
                    <h4 class="mb-0"><i class="bi bi-info-circle-fill me-2"></i><asp:Literal ID="litModuleTitle" runat="server" /></h4>
                </div>
                <div class="card-body p-3 p-md-4">

                    <asp:Panel ID="pnlModuleNote" runat="server" CssClass="alert alert-info small" Visible="false">
                        <i class="bi bi-info-circle me-1"></i><asp:Literal ID="litModuleNote" runat="server" />
                    </asp:Panel>

                    <div class="row g-3 mb-4">
                        <asp:Repeater ID="rptStats" runat="server">
                            <ItemTemplate>
                                <div class="col-6 col-md-3">
                                    <div class="stat-card shadow-sm">
                                        <p class="stat-value" style="font-size:1.25rem;"><%# Eval("Value") %></p>
                                        <p class="stat-label"><%# Eval("Label") %></p>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <h5 class="mb-3"><asp:Literal ID="litRecentSectionTitle" runat="server">Recent Records</asp:Literal></h5>

                    <div class="table-responsive">
                        <asp:GridView ID="gvRecent" runat="server" AutoGenerateColumns="true" CssClass="table table-hover align-middle" GridLines="None" />
                    </div>
                    <asp:Panel ID="pnlRecentEmpty" runat="server" CssClass="empty-state" Visible="false" />

                </div>
            </asp:Panel>

        </div>

    </form>
</body>
</html>
