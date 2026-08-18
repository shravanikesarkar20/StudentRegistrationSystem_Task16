using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StudentRegistrationSystem
{
    /// <summary>
    /// Task 13: public Home Page. Shows the active slide/banner images (managed by the admin
    /// through BannerManagement.aspx / BannerEdit.aspx) and an information section with
    /// buttons that redirect to the existing task pages (Register, Bulk Register, Display,
    /// Login, Fees, Admin Login).
    ///
    /// Task 14: adds a second carousel, directly below the banner slider, that showcases
    /// registered active candidates (students whose ApprovalStatus = 'Approved' and
    /// AccountStatus = 'Active' — see CandidateCarouselHelper).
    /// </summary>
    public partial class Home : Page
    {
        /// <summary>How many candidate cards are shown per carousel slide. Bootstrap's grid
        /// (col-12 / col-sm-6 / col-lg-4) then collapses that same slide down to 1 card on
        /// mobile and 2 on tablet, so the carousel stays responsive across breakpoints.</summary>
        private const int CANDIDATES_PER_SLIDE = 3;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadBanners();
                LoadCandidates();
            }
        }

        /// <summary>Requirement: display of slide / banner images — only active banners, in
        /// DisplayOrder, exactly as configured through the Admin Panel.</summary>
        private void LoadBanners()
        {
            try
            {
                DataTable dt = HomeBannerHelper.GetActiveBannersForDisplay();

                if (dt == null || dt.Rows.Count == 0)
                {
                    pnlCarousel.Visible = false;
                    pnlNoBanners.Visible = true;
                    return;
                }

                rptBanners.DataSource = dt;
                rptBanners.DataBind();

                // Prev/Next controls and indicator dots only make sense with more than one slide.
                bool multipleSlides = dt.Rows.Count > 1;
                pnlCarouselControls.Visible = multipleSlides;

                StringBuilder indicators = new StringBuilder();
                if (multipleSlides)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        indicators.AppendFormat(
                            "<button type=\"button\" data-bs-target=\"#homeBannerCarousel\" data-bs-slide-to=\"{0}\"{1} aria-label=\"Slide {2}\"></button>",
                            i, i == 0 ? " class=\"active\" aria-current=\"true\"" : "", i + 1);
                    }
                }
                carouselIndicators.InnerHtml = indicators.ToString();

                pnlCarousel.Visible = true;
                pnlNoBanners.Visible = false;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Home.LoadBanners", "Failed to load home page banners.", ex);
                // Fail safe: show the friendly fallback panel instead of a broken/empty carousel.
                pnlCarousel.Visible = false;
                pnlNoBanners.Visible = true;
            }
        }

        /// <summary>Task 14, Requirement: display registered active candidate cards dynamically,
        /// grouped into slides of <see cref="CANDIDATES_PER_SLIDE"/>, with Previous/Next
        /// controls, indicators, automatic transition, and a friendly empty state when there
        /// are no active candidates.</summary>
        private void LoadCandidates()
        {
            try
            {
                DataTable dt = CandidateCarouselHelper.GetActiveCandidatesForDisplay();

                if (dt == null || dt.Rows.Count == 0)
                {
                    pnlCandidateCarousel.Visible = false;
                    pnlNoCandidates.Visible = true;
                    return;
                }

                // Split the flat result set into fixed-size "slides". Each slide is kept as its
                // own DataTable (same schema, cloned) rather than a List<DataRow> so the nested
                // Repeater's ItemTemplate can keep using ordinary Eval("ColumnName") bindings —
                // Eval only resolves against DataRowView, which a DataTable's default view gives.
                List<DataTable> slides = new List<DataTable>();
                DataTable currentSlide = null;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i % CANDIDATES_PER_SLIDE == 0)
                    {
                        currentSlide = dt.Clone();
                        slides.Add(currentSlide);
                    }
                    currentSlide.ImportRow(dt.Rows[i]);
                }

                rptCandidateSlides.DataSource = slides;
                rptCandidateSlides.DataBind();

                bool multipleSlides = slides.Count > 1;
                pnlCandidateControls.Visible = multipleSlides;

                StringBuilder indicators = new StringBuilder();
                if (multipleSlides)
                {
                    for (int i = 0; i < slides.Count; i++)
                    {
                        indicators.AppendFormat(
                            "<button type=\"button\" data-bs-target=\"#candidateCarousel\" data-bs-slide-to=\"{0}\"{1} aria-label=\"Slide {2}\"></button>",
                            i, i == 0 ? " class=\"active\" aria-current=\"true\"" : "", i + 1);
                    }
                }
                candidateIndicators.InnerHtml = indicators.ToString();

                pnlCandidateCarousel.Visible = true;
                pnlNoCandidates.Visible = false;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Home.LoadCandidates", "Failed to load the active candidates carousel.", ex);
                // Fail safe: show the friendly fallback panel instead of a broken/empty carousel.
                pnlCandidateCarousel.Visible = false;
                pnlNoCandidates.Visible = true;
            }
        }

        /// <summary>Binds each slide's chunk (a small DataTable) into its nested Repeater.</summary>
        protected void rptCandidateSlides_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            Repeater rptCandidatesInSlide = (Repeater)e.Item.FindControl("rptCandidatesInSlide");
            rptCandidatesInSlide.DataSource = (DataTable)e.Item.DataItem;
            rptCandidatesInSlide.DataBind();
        }

        /// <summary>Returns the &lt;img&gt; markup for a candidate's uploaded photo, or a
        /// neutral avatar icon fallback when no photo was uploaded — mirrors how Display.aspx
        /// handles a missing PhotoPath, but resolves the app-relative URL properly (via
        /// ResolveUrl) instead of relying on a raw "~/" prefix.</summary>
        protected string BuildCandidatePhotoHtml(object photoPathObj)
        {
            string photoPath = photoPathObj as string;

            if (string.IsNullOrEmpty(photoPath))
            {
                return "<div class=\"candidate-photo-fallback\"><i class=\"bi bi-person-fill\"></i></div>";
            }

            string url = ResolveUrl("~/" + photoPath.TrimStart('~', '/'));
            return "<img class=\"candidate-photo\" src=\"" + url + "\" alt=\"Candidate photo\" />";
        }
    }
}
