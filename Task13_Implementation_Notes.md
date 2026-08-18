# Task 13 — Student Management System: Home Page Implementation
### Implementation Notes

Built on top of the Task 12 codebase (Registration Fee Management). This document maps each
requirement from the Task 13 brief to what was added, lists the new/changed files, and gives a
manual test checklist.

---

## 1. Home Page (Requirement 1)

New public landing page — `Home.aspx` — with no login required, matching the look and feel of
the existing public pages (`Register.aspx`, `Display.aspx`, etc.):

- **Simple and user-friendly interface**: a hero banner slider up top, followed by a clean grid
  of action cards that explain and link to every part of the system.
- **Responsive layout for both desktop and mobile devices**: built entirely with Bootstrap 5's
  grid and the project's existing `Content/site.css` design system (already responsive across
  the app). The carousel height, caption padding and card grid all collapse gracefully at phone
  widths via a `@media (max-width: 767px)` block in `Home.aspx`.
- **Display of slide / banner images**: a Bootstrap **carousel** (`#homeBannerCarousel`) shows
  every *active* banner from `dbo.HomeBanners`, in `DisplayOrder`, with a title/caption overlay.
  If no banners are configured yet, a friendly fallback panel (`pnlNoBanners`) is shown instead
  of a broken/empty slider.
- **Information section with buttons for redirection to the existing task pages**: six action
  cards link to `Register.aspx` (Student Registration), `BulkRegister.aspx` (Bulk Registration),
  `Display.aspx` (View Student Records), `Login.aspx` (Student Login — also used for the "Check
  My Fees" card, since fee status lives on the student `Dashboard.aspx` after login), and
  `AdminLogin.aspx` (Admin Panel).

`Home.aspx` is intended as the new application start page (see **Setup** below); it's also
linked from the navbar of every other public page (`Register.aspx`, `Display.aspx`,
`BulkRegister.aspx`, `Dashboard.aspx`, `Login.aspx`, `AdminLogin.aspx`) so users can always get
back to it.

## 2. Admin Login — Banner Management (Requirement 2)

New **Home Banners** section in the Admin Panel (`BannerManagement.aspx` + `BannerEdit.aspx`),
gated by the existing `Session["AdminName"]` admin login (`AdminAuth.cs` / `AdminLogin.aspx` —
no new authentication system, fully integrated with what Task 7 already built) and linked from
the navbar on `AdminDashboard.aspx` and every other Admin Panel page, following the exact
two-way navigation pattern used for Advertisements (Task 11) and Registration Fees (Task 12).

| Capability | Where |
|---|---|
| Upload new slide/banner images | `FileUpload` control on `BannerEdit.aspx`, saved to `~/Uploads/HomeBanners/` with a random GUID filename (never trusts the client-supplied name) |
| Activate / Deactivate banner images | Checkbox on the edit page, or click the status badge in the list (`BannerManagement.aspx`) to toggle instantly |
| Delete unwanted banner images | Row action on the list, with a JS `confirm()` before the postback |
| Set and update the display order of slides | Numeric field on create; **Move Up / Move Down** row actions on the list swap `DisplayOrder` between neighbours (same approach as Task 11's Advertisements — no drag-and-drop library needed) |
| Dynamically manage all slide-related operations | `App_Code/HomeBannerHelper.cs` is the single data-access point for every operation (list/search, get by id, insert, update, delete, toggle active, swap order, next order) — all fully parameterized (`SqlParameter`), following the same pattern as `DBHelper` / `AdvertisementHelper` |

Image uploads are validated the same way as the Task 11 Advertisement uploads: allowed
extensions (`.jpg/.jpeg/.png/.gif/.webp`), a configurable max size
(`HomeBannerImageMaxSizeMB` in `Web.config`, default 5 MB), and a magic-byte content check
(`LooksLikeImage`) so a renamed non-image file is rejected even if its extension looks valid.
A brand-new banner requires an image (there's nothing sensible to show in the slider without
one); editing an existing banner without picking a new file keeps the current image.

## 3. Technical Guidelines

- **Follows existing coding standards and project architecture**: `HomeBannerHelper` mirrors
  `AdvertisementHelper` method-for-method; `Home.aspx` / `BannerManagement.aspx` /
  `BannerEdit.aspx` reuse the same Bootstrap 5 + Bootstrap Icons + `Content/site.css` stack,
  the same `AppLogger` error logging, the same `ShowAlert` pattern, and the same
  `Response.Cache.SetCacheability(NoCache)` + admin-session-guard `Page_Load` boilerplate as
  every other Admin Panel page.
- **Dynamic, maintainable, and easily extendable**: adding a caption field, a "click-through
  link" per banner, or a scheduled start/end date later would only touch `HomeBannerHelper` +
  the two admin pages — `Home.aspx` already renders whatever `GetActiveBannersForDisplay()`
  returns, in order, with no hard-coded banner count or content.
- **Tested before submission**: see the manual test checklist below.

## Database

`Database_Task13_HomeBanners.sql` (idempotent, guarded with `IF OBJECT_ID(...) IS NULL` —
matching the Task 10/11/12 migration pattern):

- `dbo.HomeBanners` — `BannerID, Title, Caption, ImagePath, DisplayOrder, IsActive,
  CreatedDate, UpdatedDate`, indexed on `DisplayOrder` and `IsActive`.
- Seeds two placeholder rows (no image) on a fresh install so the table isn't empty — an admin
  replaces these with real banners via `BannerEdit.aspx`; until an image is uploaded,
  `GetActiveBannersForDisplay()` skips rows with a blank `ImagePath`, so `Home.aspx` shows the
  friendly fallback panel rather than a broken slide.

## New / changed files

**New**
- `App_Code/HomeBannerHelper.cs` — banner CRUD, active-status, display-order and image-path logic
- `Home.aspx` / `.aspx.cs` / `.aspx.designer.cs` — public Home Page
- `BannerManagement.aspx` / `.aspx.cs` / `.aspx.designer.cs` — Admin Panel banner list
- `BannerEdit.aspx` / `.aspx.cs` / `.aspx.designer.cs` — Admin Panel banner create/edit + upload
- `Database_Task13_HomeBanners.sql` — schema migration
- `Uploads/HomeBanners/` — placeholder folder for uploaded banner images

**Changed**
- `Web.config` — added `HomeBannerImageUploadPath` / `HomeBannerImageMaxSizeMB` app settings
- `StudentRegistrationSystem.csproj` — registered all new files so they're included in the build
- `AdminDashboard.aspx`, `AdvertisementManagement.aspx`, `AdvertisementEdit.aspx`,
  `FeeStructureManagement.aspx`, `FeeStructureEdit.aspx`, `StudentFeeDemand.aspx`,
  `RecordPayment.aspx`, `FeeReconciliation.aspx`, `RichTextEditor.aspx`,
  `RichTextEditorEdit.aspx` — added the "Home Banners" navbar entry
- `Register.aspx`, `Display.aspx`, `BulkRegister.aspx`, `Dashboard.aspx`, `Login.aspx`,
  `AdminLogin.aspx` — added a "Home" link back to `Home.aspx`

## Setup

1. Run `Database_Task13_HomeBanners.sql` against `StudentRegistrationDB` (after the base script
   and the Task 7/10/11/12 blocks). It's idempotent — safe on a fresh or already-running
   database.
2. No `Web.config` connection changes are required beyond the new `HomeBannerImage*` app
   settings, which are already added with sensible defaults.
3. In Visual Studio, right-click **`Home.aspx`** → **Set As Start Page** so the application
   opens on the new Home Page first (previously `Register.aspx`).
4. Log in to the Admin Panel → **Home Banners** → **New Banner** to upload real slide images;
   the two seeded placeholder rows have no image and are skipped by the public slider until
   then.

## Manual test checklist

- [ ] Load `Home.aspx` with no banners uploaded yet → friendly fallback panel shown, no broken
      image/carousel
- [ ] Admin Panel → Home Banners → New Banner → upload a `.jpg`, set title/caption/order,
      Active checked → Save → banner appears in the list and in the `Home.aspx` slider
- [ ] Upload a second banner with a lower display order number → it now shows first in the
      slider; carousel indicator dots and prev/next arrows appear (hidden entirely with only
      one active banner)
- [ ] Click **Move Up / Move Down** on a banner → order swaps with its neighbour, both in the
      admin list and on `Home.aspx` after refresh
- [ ] Click the status badge on a banner to deactivate it → disappears from `Home.aspx`
      immediately, stays visible (as Inactive) in the admin list
- [ ] Edit a banner without choosing a new file → existing image is kept; upload a new file →
      old image is replaced
- [ ] Try uploading a `.txt` file renamed to `.jpg`, or a file over the configured size limit →
      rejected with a clear inline error, nothing saved
- [ ] Delete a banner → removed from both the admin list and the public slider
- [ ] Load `BannerManagement.aspx` or `BannerEdit.aspx` directly with no admin session →
      redirected to `AdminLogin.aspx`
- [ ] Resize the browser to a phone width (or open on a real device) on `Home.aspx` → banner
      slider, navbar, and action cards all reflow cleanly with no horizontal scroll
- [ ] Click each action card's button on `Home.aspx` → lands on the correct existing page
      (`Register.aspx`, `BulkRegister.aspx`, `Display.aspx`, `Login.aspx`, `AdminLogin.aspx`)
