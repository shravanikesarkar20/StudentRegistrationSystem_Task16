# Task 11 — Student Management System: Advertisement Modal Implementation
### Implementation Notes

Built on top of the Task 10 codebase (Rich Text Editor). This document maps each requirement
from the Task 11 brief to what was added, lists the new/changed files, and gives a manual test
checklist.

---

## 1. Display Advertisement Modal (Requirement 1)

- The modal automatically appears on `Register.aspx` load via `Page_Load` →
  `LoadAdvertisementModal()`, which renders the ads server-side and sets a hidden field
  (`hdnShowAdModal`) that a small `window.addEventListener("load", ...)` script reads to call
  Bootstrap's `Modal.show()` — no manual trigger needed.
- Built with Bootstrap 5's `modal` + `carousel` components (already the project's UI stack — no
  new framework), so it is responsive out of the box across desktop, tablet and mobile; extra
  CSS in `Register.aspx` trims image height and padding at phone widths.
- The modal is dismissible via the close button, **Esc**, or a backdrop click (no
  `data-bs-backdrop="static"` trap) and never blocks the registration form underneath — closing
  it (or ignoring it) has zero effect on filling out/submitting the registration form.

## 2. Admin Panel Management (Requirement 2)

New **Advertisements** section in the Admin Panel (`AdvertisementManagement.aspx` +
`AdvertisementEdit.aspx`), linked from the navbar on `AdminDashboard.aspx`,
`RichTextEditor.aspx`, `RichTextEditorEdit.aspx`, and itself — consistent two-way navigation
matching the Task 10 pattern.

| Capability | Where |
|---|---|
| Enable/disable the advertisement modal (global) | Switch at the top of `AdvertisementManagement.aspx`, backed by `dbo.AdvertisementSettings` |
| Add advertisement | `AdvertisementEdit.aspx` (no `id` in query string) |
| Edit advertisement | `AdvertisementEdit.aspx?id={AdvertisementID}` |
| Delete advertisement | Row action on the list, with a JS `confirm()` before the postback |
| Upload image/banner | `FileUpload` control on the edit page, saved to `~/Uploads/Advertisements/` |
| Title + detailed description | Required fields on the edit page |
| Configure display order | Numeric field on create; **Move Up / Move Down** row actions on the list swap `DisplayOrder` between neighbours (no drag-and-drop library needed) |
| Set active/inactive status | Checkbox on the edit page, or click the status badge in the list to toggle instantly |

`App_Code/AdvertisementHelper.cs` is the single data-access point for every operation, all
fully parameterized (`SqlParameter`), following the same pattern as `DBHelper` /
`RichTextDocumentHelper`.

## 3. Modal Features (Requirement 3)

- Each advertisement's uploaded image, title and description are shown inside a Bootstrap
  **carousel** (`#adCarousel`) nested in the modal — one slide per active advertisement, in
  `DisplayOrder`. Indicators and prev/next controls only render when there is more than one
  active ad (`pnlAdIndicators` / `pnlAdControls`), so a single ad shows a clean, control-free
  card.
- A close button (`btn-close`) dismisses the modal.
- **Only active advertisements are ever queried** — `AdvertisementHelper.GetActiveAdvertisementsForDisplay()`
  filters `WHERE IsActive = 1` at the SQL level.
- If no advertisement is active, **or** the global switch is off, `LoadAdvertisementModal()`
  returns early: `pnlAdModal` stays `Visible="false"` and `hdnShowAdModal` stays `"0"`, so the
  modal markup isn't rendered at all and the JS never calls `.show()`.

## 4. Database Requirements (Requirement 4)

`Database.sql` adds two tables (idempotent `IF OBJECT_ID(...) IS NULL` guard, same pattern as
Task 10's `RichTextDocuments` migration):

```
dbo.Advertisements
    AdvertisementID INT IDENTITY PK
    Title           NVARCHAR(200)  NOT NULL
    Description     NVARCHAR(1000) NULL
    ImagePath       NVARCHAR(500)  NULL
    DisplayOrder    INT            NOT NULL DEFAULT 0
    IsActive        BIT            NOT NULL DEFAULT 1
    CreatedDate     DATETIME       NOT NULL DEFAULT GETDATE()
    UpdatedDate     DATETIME       NOT NULL DEFAULT GETDATE()

dbo.AdvertisementSettings          -- single fixed row (SettingID = 1)
    SettingID    INT  PK
    ModalEnabled BIT  NOT NULL DEFAULT 1
```

Indexes on `DisplayOrder` and `IsActive` keep the public-facing "active ads in order" query
cheap even as the table grows.

## 5. User Interface (Requirement 5)

Both new admin pages and the modal reuse `Content/site.css`, Bootstrap 5, and Bootstrap Icons —
the project's existing UI stack. Layout, spacing, typography, card/badge/button styling and the
navbar all match `AdminDashboard.aspx` / `RichTextEditor.aspx`. The list page uses the same
`table-responsive` + uppercase-header pattern as Task 10; the modal carousel is fully responsive
with a shorter image height and tighter padding below 576px.

## 6. Expected Deliverables (Requirement 6)

- **Admin page for Advertisement Management (Add, Edit, Delete, View)** —
  `AdvertisementManagement.aspx` (list/delete/toggle/reorder) + `AdvertisementEdit.aspx`
  (add/edit).
- **Database implementation** — `dbo.Advertisements` + `dbo.AdvertisementSettings` in
  `Database.sql`.
- **Dynamic advertisement modal integrated with the Student Registration page** — added to
  `Register.aspx` / `Register.aspx.cs`, fully data-driven from the database.
- **Validation and error handling** — server-side `RequiredFieldValidator` on Title and
  Description, `RangeValidator` on Display Order, image extension whitelist + magic-byte check +
  size limit on upload; every database call and the upload path are wrapped in try/catch, logged
  via `AppLogger`, and surface a friendly Bootstrap alert instead of a raw exception (matching
  the Task 8/10 error-handling pattern). A failure while loading ads on `Register.aspx` is
  swallowed and logged so it can never break the registration page itself.
- **Clean, readable and well-structured code** — new code follows the existing project
  conventions: `DBHelper`-style parameterized SQL, the same admin session/auth pattern
  (`Session["AdminName"]`, no-cache headers, redirect to `AdminLogin.aspx`), and the same
  Save/Reset/alert UX already used by the Rich Text Editor module.

## Security notes

- **SQL Injection**: every query in `AdvertisementHelper.cs` is parameterized — no string
  concatenation of user input into SQL.
- **Image upload security**: extension whitelist (`.jpg/.jpeg/.png/.gif/.webp`) *and* a
  magic-number check on the actual file bytes (so a renamed `.aspx` can't slip through);
  `AdvertisementImageMaxSizeMB` (default 5 MB, configurable in `Web.config`); saved with a random
  GUID filename (never the client-supplied name), preventing path traversal / overwrite —
  identical approach to `RichTextImageUpload.ashx` and the existing student-photo upload.
- **Role-based authorization**: `AdvertisementManagement.aspx` and `AdvertisementEdit.aspx` both
  check `Session["AdminName"]` before doing anything and redirect unauthenticated requests to
  `AdminLogin.aspx`, matching every other admin page in the project.
- **XSS**: Title/Description are rendered through ASP.NET's `Eval()` data-binding (HTML-encoded
  by default in the `.aspx` markup) inside the public modal; the admin grid's HTML-encodes the
  title in the delete-confirmation JS via `HttpUtility.JavaScriptStringEncode`.

## New / changed files

**New**
- `AdvertisementManagement.aspx` / `.aspx.cs` / `.aspx.designer.cs` — admin list page (global toggle, search, reorder, toggle status, delete)
- `AdvertisementEdit.aspx` / `.aspx.cs` / `.aspx.designer.cs` — create/edit form with image upload
- `App_Code/AdvertisementHelper.cs` — parameterized CRUD + settings data access
- `Uploads/Advertisements/` — image upload target folder
- `Task11_Implementation_Notes.md` — this file

**Changed**
- `Register.aspx` / `Register.aspx.cs` / `Register.aspx.designer.cs` — added the advertisement
  modal markup, carousel, auto-show script, and `LoadAdvertisementModal()` / `rptAds_ItemDataBound`
- `AdminDashboard.aspx`, `RichTextEditor.aspx`, `RichTextEditorEdit.aspx` — added the
  "Advertisements" navbar menu item
- `Database.sql` — added `dbo.Advertisements` + `dbo.AdvertisementSettings` (idempotent migration block)
- `Web.config` — added `AdvertisementImageUploadPath` / `AdvertisementImageMaxSizeMB` app settings
- `StudentRegistrationSystem.csproj` — registered all new files so they're included in the build

---

## Setup

1. Run the new block at the bottom of `Database.sql` against your existing `StudentRegistrationDB`
   (or run the whole script fresh — both new-table blocks are guarded with
   `IF OBJECT_ID(...) IS NULL`, so it's safe either way). The `AdvertisementSettings` block also
   seeds the single required settings row (`SettingID = 1`, `ModalEnabled = 1`).
2. Confirm `~/Uploads/Advertisements/` is writable by the application pool identity (same
   requirement as `~/Uploads/Students/` and `~/Uploads/RichTextImages/`).
3. No new NuGet packages are required — everything reuses classes already referenced by the
   project (`System.Data.SqlClient`, `System.Web`).

## Manual test checklist

- [ ] Load `Register.aspx` with no advertisements in the database → no modal appears
- [ ] Add one advertisement with an image, title and description, mark Active → reload
      `Register.aspx` → modal appears automatically with that content
- [ ] Add a second active advertisement with a higher Display Order → modal now shows a
      carousel with indicators and prev/next controls, in the correct order
- [ ] Click the close button → modal dismisses; the registration form is fully usable underneath
      both before and after
- [ ] Press Esc / click the backdrop → modal also dismisses (does not trap the user)
- [ ] Set an advertisement to Inactive from the admin list (click its status badge) → reload
      `Register.aspx` → that ad no longer appears in the modal
- [ ] Turn the global "Advertisement Modal" switch off in the Admin Panel → reload
      `Register.aspx` → no modal appears at all, even with active ads
- [ ] Turn the switch back on → modal reappears
- [ ] Use Move Up / Move Down on the admin list → order changes persist and reflect in the
      modal's slide order
- [ ] Edit an advertisement without uploading a new image → existing image is preserved
- [ ] Edit an advertisement and upload a new image → new image replaces the old one
- [ ] Try saving with an empty Title or Description → validation message, no postback to the
      database
- [ ] Upload a non-image file renamed to `.png` → rejected with a clear message
- [ ] Upload an image over the configured size limit → rejected with a clear message
- [ ] Delete an advertisement → confirm prompt appears → row removed after confirming
- [ ] Resize the browser to a phone width and confirm the modal, carousel, and admin list all
      remain usable
- [ ] Load `AdvertisementManagement.aspx` / `AdvertisementEdit.aspx` directly (no session) →
      redirected to `AdminLogin.aspx`
