# Task 14 — Carousel Implementation (Registered Active Candidates)
### Implementation Notes

Continuation of Task 13 (`Home.aspx`). Adds a second Bootstrap carousel to the public Home
Page, directly below the existing banner slider, that showcases **registered active
candidates** — i.e. students in `dbo.Students` with `ApprovalStatus = 'Approved'` and
`AccountStatus = 'Active'` (the same approval/account-status columns the Admin Dashboard has
used since Task 7/8).

## New / changed files

| File | Change |
|---|---|
| `App_Code/CandidateCarouselHelper.cs` | **New.** Data-access layer for the feature — `GetActiveCandidatesForDisplay()` (joins `Students` → `Districts` → `States` for a human-readable location, filtered to Approved + Active, newest first, capped at 30) and `GetActiveCandidateCount()`. Fully parameterized, same style as `HomeBannerHelper`. |
| `Home.aspx` | Added the `pnlCandidateCarousel` / `#candidateCarousel` markup block, plus the `.candidate-carousel` / `.candidate-card` / `.candidate-photo` styles (scoped `<style>` block, consistent with the existing hero-carousel / action-card styling). |
| `Home.aspx.cs` | Added `LoadCandidates()`, `rptCandidateSlides_ItemDataBound`, and `BuildCandidatePhotoHtml()`; wired into `Page_Load` alongside the existing `LoadBanners()`. |
| `Home.aspx.designer.cs` | Added field declarations for the new server controls. |
| `StudentRegistrationSystem.csproj` | Registered the new `CandidateCarouselHelper.cs` compile item. |

No database schema changes were needed — `ApprovalStatus` / `AccountStatus` already exist on
`dbo.Students` from the Task 7 migration in `Database.sql`.

## Requirement checklist

- **Display registered and active candidate cards dynamically** — `CandidateCarouselHelper`
  queries `Students` live on every page load (`Page_Load` → `LoadCandidates()`, no caching), so
  newly approved/activated students appear automatically.
- **Candidate image, name, and relevant basic information** — each card shows the uploaded
  photo (`PhotoPath`, resolved via `ResolveUrl`) or a neutral avatar icon fallback when none was
  uploaded, the candidate's full name, their District/State location, and their registration
  date.
- **Previous and Next navigation controls** — standard Bootstrap `.carousel-control-prev` /
  `-next` buttons (`#candidateCarousel`), only rendered when there's more than one slide
  (`pnlCandidateControls.Visible`).
- **Carousel indicators** — built server-side the same way the Task 13 banner carousel does
  (`candidateIndicators.InnerHtml`, one dot per slide, `data-bs-slide-to`).
- **Automatic slide transition** — `data-bs-ride="carousel" data-bs-interval="6000"` (6s, matches
  the app's design conventions).
- **Responsive across desktop, tablet, and mobile** — candidates are grouped into slides of 3
  (`CANDIDATES_PER_SLIDE`); each slide is a Bootstrap row using `col-12 col-sm-6 col-lg-4`, so
  the same slide shows 1 card per row on phones, 2 on tablets, and 3 on desktop. A
  `@media (max-width: 767px)` block also shrinks the card padding and avatar size.
- **Clean, professional UI consistent with the Home Page** — reuses the project's existing
  `--brand` / `--radius-lg` / `--shadow-*` CSS variables from `Content/site.css`, matching the
  look of the Task 13 hero carousel and the action cards below it.
- **Proper handling when there are no active candidates** — `pnlNoCandidates` (same
  `.hero-fallback` treatment as the Task 13 "no banners" state) renders instead of an empty
  carousel shell.
- **Candidate information retrieved correctly from the database** — single parameterized query
  joining `Students` → `Districts` → `States`; failures are caught and logged
  (`AppLogger.Error("Home.LoadCandidates", ...)`) and fall back to the empty-state panel rather
  than crashing the page.
- **No existing functionality affected** — the banner carousel (`LoadBanners()`) and the action
  card grid below are untouched; the new section is purely additive.

## Manual test checklist

1. With several `Approved` + `Active` students in the DB, load `Home.aspx` — confirm the new
   "Meet Our Registered Candidates" carousel renders, grouped 3-per-slide on desktop.
2. Resize to tablet/mobile widths (or use browser dev tools) — confirm the same slide's cards
   stack to 2-per-row, then 1-per-row.
3. Click Previous/Next and the indicator dots — confirm the correct slide shows and the active
   dot updates.
4. Let the page sit idle 6+ seconds — confirm it auto-advances.
5. In the Admin Dashboard, set a candidate's `AccountStatus` to `Inactive` (or leave
   `ApprovalStatus = 'Pending'`) — reload `Home.aspx` and confirm that candidate no longer
   appears.
6. Temporarily point the DB connection string somewhere invalid — confirm `Home.aspx` still
   loads and shows the "No Active Candidates Yet" fallback instead of an error (and that the
   failure is written to the App_Data logs via `AppLogger`).
7. Confirm the Task 13 banner carousel and the "Everything You Need" action cards below still
   work exactly as before.
