<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="StudentRegistrationSystem.Home" %>
<%@ Import Namespace="System.Web" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Home | Advanced Student Registration System</title>

    <!-- Bootstrap 5 -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
    <!-- Shared site styles -->
    <link rel="stylesheet" href="Content/site.css" />

    <style>
        /* ---------- Hero banner slider ---------- */
        .hero-carousel {
            border-radius: var(--radius-lg);
            overflow: hidden;
            box-shadow: var(--shadow-card);
            background: #eef4f2;
        }
        .hero-carousel .carousel-item {
            height: 420px;
            background-size: cover;
            background-position: center;
            background-color: #0f4c46;
        }
        .hero-carousel .carousel-caption {
            background: linear-gradient(180deg, transparent 0%, rgba(9,30,27,.82) 78%);
            bottom: 0; left: 0; right: 0; text-align: left;
            padding: 2.25rem 2rem 1.5rem;
            border-radius: 0 0 var(--radius-lg) var(--radius-lg);
        }
        .hero-carousel .carousel-caption h3 { color: #fff; font-weight: 800; margin-bottom: .35rem; }
        .hero-carousel .carousel-caption p { color: #e6f2ef; margin-bottom: 0; font-size: .98rem; max-width: 620px; }
        .hero-carousel .carousel-control-prev, .hero-carousel .carousel-control-next { width: 6%; }
        .hero-carousel .carousel-indicators [data-bs-target] {
            width: 9px; height: 9px; border-radius: 50%; margin: 0 5px;
        }
        .hero-fallback {
            height: 420px; border-radius: var(--radius-lg);
            background: linear-gradient(120deg, #0b2b28, #0f4c46 55%, #0d9488);
            display: flex; flex-direction: column; align-items: center; justify-content: center;
            text-align: center; color: #fff; padding: 2rem;
        }
        .hero-fallback i { font-size: 3rem; margin-bottom: .75rem; color: #7fe0d3; }
        @media (max-width: 767px) {
            .hero-carousel .carousel-item, .hero-fallback { height: 280px; }
            .hero-carousel .carousel-caption { padding: 1.25rem 1.1rem 1rem; }
            .hero-carousel .carousel-caption h3 { font-size: 1.15rem; }
            .hero-carousel .carousel-caption p { font-size: .88rem; }
        }

        /* ---------- Info / action cards ---------- */
        .action-card {
            background: #fff; border: 1px solid #e6ece9; border-radius: var(--radius-lg);
            padding: 22px; height: 100%; display: flex; flex-direction: column;
            transition: box-shadow .2s ease, transform .2s ease;
        }
        .action-card:hover { box-shadow: var(--shadow-hover); transform: translateY(-2px); }
        .action-card .action-icon {
            width: 52px; height: 52px; border-radius: 14px; display: inline-flex;
            align-items: center; justify-content: center; font-size: 24px; margin-bottom: 14px;
            background: var(--brand-soft); color: var(--brand-dark);
        }
        .action-card h5 { margin-bottom: .4rem; }
        .action-card p { color: var(--ink-muted); font-size: .92rem; flex-grow: 1; }
        .action-card .btn { align-self: flex-start; margin-top: .75rem; }

        .section-title { font-weight: 800; letter-spacing: -.01em; }
        .section-sub { color: var(--ink-muted); font-size: 1rem; }

        /* ---------- Task 14: Registered Active Candidates carousel ---------- */
        .candidate-carousel { padding-bottom: 2.75rem; position: relative; }
        .candidate-carousel .carousel-item { transition: transform .6s ease-in-out; }
        .candidate-carousel .carousel-item .row { min-height: 1px; }
        .candidate-card {
            background: #fff; border: 1px solid #e6ece9; border-radius: var(--radius-lg);
            box-shadow: var(--shadow-soft); padding: 24px 20px; text-align: center; height: 100%;
            display: flex; flex-direction: column; align-items: center;
            transition: box-shadow .2s ease, transform .2s ease;
        }
        .candidate-card:hover { box-shadow: var(--shadow-hover); transform: translateY(-2px); }
        .candidate-photo {
            width: 92px; height: 92px; border-radius: 50%; object-fit: cover;
            border: 3px solid var(--brand-soft-strong); margin-bottom: 14px;
        }
        .candidate-photo-fallback {
            width: 92px; height: 92px; border-radius: 50%; margin-bottom: 14px;
            background: var(--brand-soft); color: var(--brand-dark);
            display: flex; align-items: center; justify-content: center; font-size: 2.4rem;
        }
        .candidate-name { font-weight: 700; margin-bottom: .3rem; color: var(--ink); }
        .candidate-meta { color: var(--ink-muted); font-size: .86rem; margin-bottom: .2rem; }
        .candidate-carousel .carousel-control-prev,
        .candidate-carousel .carousel-control-next {
            width: 5%; opacity: 0; top: 0; bottom: 2.75rem; transition: opacity .2s ease;
        }
        .candidate-carousel:hover .carousel-control-prev,
        .candidate-carousel:hover .carousel-control-next { opacity: 1; }
        .candidate-carousel .carousel-control-prev-icon,
        .candidate-carousel .carousel-control-next-icon {
            background-color: var(--brand-dark); border-radius: 50%; padding: 18px; background-size: 45%;
        }
        .candidate-carousel .carousel-indicators { bottom: 0; margin-bottom: 0; }
        .candidate-carousel .carousel-indicators [data-bs-target] {
            width: 9px; height: 9px; border-radius: 50%; margin: 0 5px; background-color: var(--brand-dark); opacity: .3;
        }
        .candidate-carousel .carousel-indicators .active { opacity: 1; }
        @media (max-width: 767px) {
            .candidate-card { padding: 18px 14px; }
            .candidate-photo, .candidate-photo-fallback { width: 76px; height: 76px; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar navbar-dark bg-primary mb-4">
            <div class="container-fluid px-4">
                <span class="navbar-brand mb-0 h1"><i class="bi bi-mortarboard-fill me-2"></i>Advanced Student Registration System</span>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <a href="Home.aspx" class="btn btn-light btn-sm text-primary fw-semibold"><i class="bi bi-house-door-fill me-1"></i>Home</a>
                    <a href="Register.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-person-plus-fill me-1"></i>Register</a>
                    <a href="Display.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-card-list me-1"></i>View Students</a>
                    <a href="ClassTimetableView.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-calendar3 me-1"></i>Class Timetable</a>
                    <a href="FacultyTimetableView.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-person-badge me-1"></i>Faculty Timetable</a>
                    <a href="Login.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-box-arrow-in-right me-1"></i>Student Login</a>
                    <!-- Task 16: new nav entry for the Centralised Institute Dashboard login. -->
                    <a href="InstituteLogin.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-building me-1"></i>Institute Login</a>
                    <a href="InstituteRegister.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-building-add me-1"></i>Register Institute</a>
                    <a href="AdminLogin.aspx" class="btn btn-outline-light btn-sm"><i class="bi bi-shield-lock-fill me-1"></i>Admin</a>
                </div>
            </div>
        </nav>

        <div class="container-fluid px-4 pb-5" style="max-width: 1240px;">

            <!-- Requirement 1: display of slide / banner images, dynamically managed by the admin -->
            <div class="mb-5">
                <asp:Panel ID="pnlCarousel" runat="server" CssClass="hero-carousel">
                    <div id="homeBannerCarousel" class="carousel slide carousel-fade" data-bs-ride="carousel" data-bs-interval="5000">
                        <div class="carousel-indicators" runat="server" id="carouselIndicators"></div>
                        <asp:Repeater ID="rptBanners" runat="server">
                            <HeaderTemplate><div class="carousel-inner"></HeaderTemplate>
                            <ItemTemplate>
                                <div class='<%# "carousel-item" + (Container.ItemIndex == 0 ? " active" : "") %>'
                                     style='<%# "background-image:url(\"" + ResolveUrl("~/" + Eval("ImagePath").ToString().TrimStart('~','/')) + "\");" %>'>
                                    <div class="carousel-caption">
                                        <h3><%# Eval("Title") %></h3>
                                        <asp:Literal runat="server" Text='<%# Eval("Caption") == DBNull.Value ? "" : "<p>" + HttpUtility.HtmlEncode(Eval("Caption").ToString()) + "</p>" %>' />
                                    </div>
                                </div>
                            </ItemTemplate>
                            <FooterTemplate></div></FooterTemplate>
                        </asp:Repeater>
                        <asp:Panel ID="pnlCarouselControls" runat="server">
                            <button class="carousel-control-prev" type="button" data-bs-target="#homeBannerCarousel" data-bs-slide="prev">
                                <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                            </button>
                            <button class="carousel-control-next" type="button" data-bs-target="#homeBannerCarousel" data-bs-slide="next">
                                <span class="carousel-control-next-icon" aria-hidden="true"></span>
                            </button>
                        </asp:Panel>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlNoBanners" runat="server" CssClass="hero-fallback" Visible="false">
                    <i class="bi bi-images"></i>
                    <h4 class="mb-1">Welcome to the Student Management System</h4>
                    <p class="mb-0" style="max-width:520px;">No banners are configured yet &mdash; an administrator can add slide images from the Admin Panel &gt; Home Banners screen.</p>
                </asp:Panel>
            </div>

            <!-- Task 14: carousel displaying registered active candidates -->
            <div class="mb-5">
                <div class="text-center mb-4">
                    <h2 class="section-title">Meet Our Registered Candidates</h2>
                    <p class="section-sub">A look at students who are currently registered and active in the system.</p>
                </div>

                <asp:Panel ID="pnlCandidateCarousel" runat="server" CssClass="candidate-carousel">
                    <div id="candidateCarousel" class="carousel slide" data-bs-ride="carousel" data-bs-interval="6000">
                        <div class="carousel-indicators" runat="server" id="candidateIndicators"></div>
                        <asp:Repeater ID="rptCandidateSlides" runat="server" OnItemDataBound="rptCandidateSlides_ItemDataBound">
                            <HeaderTemplate><div class="carousel-inner"></HeaderTemplate>
                            <ItemTemplate>
                                <div class='<%# "carousel-item" + (Container.ItemIndex == 0 ? " active" : "") %>'>
                                    <div class="row g-4 justify-content-center px-2 px-md-4">
                                        <asp:Repeater ID="rptCandidatesInSlide" runat="server">
                                            <ItemTemplate>
                                                <div class="col-12 col-sm-6 col-lg-4">
                                                    <div class="candidate-card">
                                                        <asp:Literal ID="litCandidatePhoto" runat="server" Text='<%# BuildCandidatePhotoHtml(Eval("PhotoPath")) %>' />
                                                        <div class="candidate-name"><%# HttpUtility.HtmlEncode(Eval("FullName").ToString()) %></div>
                                                        <div class="candidate-meta"><i class="bi bi-geo-alt-fill me-1"></i><%# HttpUtility.HtmlEncode(Eval("Location").ToString()) %></div>
                                                        <div class="candidate-meta"><i class="bi bi-calendar-check-fill me-1"></i>Registered <%# Convert.ToDateTime(Eval("RegistrationDate")).ToString("dd MMM yyyy") %></div>
                                                    </div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </div>
                            </ItemTemplate>
                            <FooterTemplate></div></FooterTemplate>
                        </asp:Repeater>
                        <asp:Panel ID="pnlCandidateControls" runat="server">
                            <button class="carousel-control-prev" type="button" data-bs-target="#candidateCarousel" data-bs-slide="prev">
                                <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                                <span class="visually-hidden">Previous</span>
                            </button>
                            <button class="carousel-control-next" type="button" data-bs-target="#candidateCarousel" data-bs-slide="next">
                                <span class="carousel-control-next-icon" aria-hidden="true"></span>
                                <span class="visually-hidden">Next</span>
                            </button>
                        </asp:Panel>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlNoCandidates" runat="server" CssClass="hero-fallback" Visible="false">
                    <i class="bi bi-person-check"></i>
                    <h4 class="mb-1">No Active Candidates Yet</h4>
                    <p class="mb-0" style="max-width:520px;">Once students register and are approved by the admin, they'll appear here.</p>
                </asp:Panel>
            </div>

            <!-- Requirement: information section with buttons for redirection to the existing task pages -->
            <div class="text-center mb-4">
                <h2 class="section-title">Everything You Need, In One Place</h2>
                <p class="section-sub">Jump straight to any part of the Student Management System.</p>
            </div>

            <div class="row g-4 mb-4">
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-person-plus-fill"></i></div>
                        <h5>Student Registration</h5>
                        <p>Register a new student with OTP-verified email, photo upload, and cascading location details.</p>
                        <a href="Register.aspx" class="btn btn-primary btn-sm">Register Now <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-people-fill"></i></div>
                        <h5>Bulk Registration</h5>
                        <p>Stage multiple student records at once and save them all together in a single transaction.</p>
                        <a href="BulkRegister.aspx" class="btn btn-primary btn-sm">Bulk Register <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-card-list"></i></div>
                        <h5>View Student Records</h5>
                        <p>Search, print, and export the full list of registered students.</p>
                        <a href="Display.aspx" class="btn btn-primary btn-sm">View Records <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-calendar3"></i></div>
                        <h5>Timetable</h5>
                        <p>Check your division's class schedule or a faculty member's weekly timetable.</p>
                        <a href="ClassTimetableView.aspx" class="btn btn-primary btn-sm">View Timetable <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-box-arrow-in-right"></i></div>
                        <h5>Student Login</h5>
                        <p>Existing students can sign in to view and update their profile and check fee status.</p>
                        <a href="Login.aspx" class="btn btn-primary btn-sm">Student Login <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-cash-coin"></i></div>
                        <h5>Registration Fees</h5>
                        <p>Check applicable fee structures for your programme, category and academic year.</p>
                        <a href="Login.aspx" class="btn btn-primary btn-sm">Check My Fees <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
                <div class="col-md-6 col-lg-4">
                    <div class="action-card">
                        <div class="action-icon"><i class="bi bi-shield-lock-fill"></i></div>
                        <h5>Admin Panel</h5>
                        <p>Manage registrations, advertisements, fees, and Home Page banners.</p>
                        <a href="AdminLogin.aspx" class="btn btn-primary btn-sm">Admin Login <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
            </div>

        </div>

        <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    </form>
</body>
</html>
