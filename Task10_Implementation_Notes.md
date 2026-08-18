# Task 10 — Student Management System: Rich Text Editor Implementation
### Implementation Notes

Built on top of the Task 9 codebase (email API integration, approval workflow, authenticated
admin login). This document maps each requirement from the Task 10 brief to what was added,
lists the new/changed files, and gives a manual test checklist.

---

## 1. Admin Panel Integration (Requirement 1)

- `AdminDashboard.aspx` navbar now has a **Rich Text Editor** button next to Dashboard/Logout,
  styled with the same Bootstrap classes as the rest of the navbar (no new CSS framework).
- Selecting it goes to `RichTextEditor.aspx` (the document list). Both new pages also link back
  to `AdminDashboard.aspx`, so navigation is two-way and consistent everywhere.
- Both pages reuse `Content/site.css` and the same auth/session pattern as
  `AdminDashboard.aspx.cs` (`Session["AdminName"]`, no-cache headers, redirect to
  `AdminLogin.aspx` if not logged in) — no separate login system was introduced.

## 2 & 5. Document Management / CRUD (Requirements 2, 5)

| Action  | Page                                          |
|---------|------------------------------------------------|
| List / search / sort / paginate | `RichTextEditor.aspx` |
| Create  | `RichTextEditorEdit.aspx` (no `id` in query string) |
| Edit    | `RichTextEditorEdit.aspx?id={DocumentID}` |
| Preview | Client-side modal on the edit page (no save needed) **and** a read-only modal from the list page's "View" action |
| Delete  | Row action on `RichTextEditor.aspx`, with a JS `confirm()` before the postback |
| Reset   | Button on the edit page — for a new document it clears the form; for an existing one it reloads the last saved version from the database, discarding unsaved edits |

`App_Code/RichTextDocumentHelper.cs` is the single data-access point for all five operations,
all fully parameterized (`SqlParameter`), following the same pattern as `DBHelper`/`AdminAuth`.

## 3. Document Information & Validation (Requirements 3, 9)

- **Document Title**: `RequiredFieldValidator`.
- **Rich Text Content**: validated two ways — a `RequiredFieldValidator` (catches a fully empty
  textarea) plus a `CustomValidator` (`validateEditorHasText` client function +
  `cvContent_ServerValidate` server function) that strips tags and rejects content that is
  *technically* non-empty markup but has no real text (e.g. an empty `<p>&nbsp;</p>`).
- The **Save**/**Update** button's `OnClientClick` calls `tinymce.triggerSave()` first, so the
  hidden `<textarea>` (and therefore the validators, which read that textarea) always see the
  live editor content, not whatever was last synced.
- Image type/size are validated in `RichTextImageUpload.ashx` (see §9/10 below).

## 4. Rich Text Editor Features (Requirement 4)

Editor: **TinyMCE 6**, loaded from `https://cdn.jsdelivr.net/npm/tinymce@6/tinymce.min.js` —
this is jsDelivr's static mirror of the open-source npm package, **not** the Tiny Cloud CDN, so
there's no API key, signup, or "this domain isn't registered" nag. Configuration lives inline in
`RichTextEditorEdit.aspx`.

| Brief requirement | How it's implemented |
|---|---|
| Bold / Italic / Underline / Strikethrough / Superscript / Subscript / Remove Formatting | Core toolbar buttons (no plugin needed) |
| Font Family / Size / Color / Background Highlight | `fontfamily`, `fontsize`, `forecolor`, `backcolor` toolbar buttons |
| Align Left/Center/Right/Justify | Core alignment buttons |
| Line Spacing | Core `lineheight` toolbar button |
| Paragraph Spacing | Custom entries in the **Formats** dropdown (`style_formats`) — Compact/Normal/Relaxed/Spacious margin presets applied to the current paragraph |
| Increase/Decrease Indentation | `indent` / `outdent` |
| Ordered / Unordered Lists | `lists` plugin |
| Hyperlinks (insert/edit/remove) | `link` plugin |
| Images (upload / resize / align) | `image` plugin, `images_upload_url` → `RichTextImageUpload.ashx`, `image_advtab: true` for alignment; drag-handle resize is on by default |
| Tables (insert, add/delete row/col, merge/split cells) | `table` plugin with its full contextual `table_toolbar` |
| Horizontal Line | `hr` toolbar button |
| Special Characters | `charmap` plugin |
| Undo / Redo | Core buttons |
| Cut / Copy / Paste | Standard browser/OS clipboard shortcuts (Ctrl+X/C/V) — modern browsers block script-triggered clipboard writes from a toolbar button for security, so, as with Word/Docs/Gmail, these are keyboard/right-click operations rather than toolbar buttons |
| Find / Replace | `searchreplace` plugin |
| Preview Before Saving | Custom **Preview** button → Bootstrap modal showing the live (unsaved) content, plus TinyMCE's own built-in `preview` plugin |
| Full Screen Editing Mode | `fullscreen` plugin |

## 6. Database (Requirement 6)

`Database.sql` adds `dbo.RichTextDocuments` (idempotent `IF OBJECT_ID(...) IS NULL` guard, same
pattern as the Task 7 `Admins` migration):

```
DocumentID    INT IDENTITY PK
Title         NVARCHAR(255)  NOT NULL
ContentHtml   NVARCHAR(MAX)  NOT NULL   -- sanitized HTML, formatting preserved
CreatedDate   DATETIME       NOT NULL
ModifiedDate  DATETIME       NOT NULL
CreatedBy     NVARCHAR(100)  NULL       -- admin's FullName from Session
ModifiedBy    NVARCHAR(100)  NULL
Status        NVARCHAR(20)   NOT NULL   -- 'Draft' | 'Published'
```

`NVARCHAR(MAX)` plus a sanitize-on-write / sanitize-on-read pass (never a lossy strip) is what
guarantees formatting survives a save → reload → edit round trip.

## 7. Document Listing (Requirement 7)

`RichTextEditor.aspx` — a single responsive `GridView` (Bootstrap `table-responsive`) with:
**Sr. No.**, **Document Title**, **Status**, **Created Date**, **Last Modified Date**, **Actions**
(View / Edit / Delete). Search (`LIKE` on Title), column sorting (click Title / Created / Last
Modified — toggles ASC/DESC), and server-side paging (10 rows/page, `OFFSET`/`FETCH`) are all
implemented directly against SQL in `RichTextDocumentHelper.GetDocuments`, so large document
sets don't load an unbounded result set into memory.

## 8. Preview (Requirement 8)

Two preview paths, both rendering the sanitized HTML in the same `.preview-body` styled
container so they look identical to the saved output:
1. **Before saving** — the edit page's Preview button (no round-trip to the server).
2. **After saving** — the list page's "View" row action opens a read-only modal.

## 9 & 10. Validation & Security (Requirements 9, 10)

- **XSS / stored-HTML injection**: `App_Code/RichTextSanitizer.cs` is a dependency-free,
  allow-list HTML sanitizer applied **server-side on every save** (authoritative) and again on
  every read-back (defense in depth). It strips `<script>`, `<iframe>`, `<object>`, `<embed>`,
  `<form>`, etc. entirely; removes every `on*="..."` event-handler attribute; neutralizes
  `javascript:` URLs and non-image `data:` URLs; and reduces every `style="..."` attribute down
  to a small whitelist of safe CSS declarations (so TinyMCE's color/alignment/spacing still
  round-trips, but `expression()`/`url()` injection does not). This runs independently of
  whatever the browser-side TinyMCE `invalid_elements` config already blocks, so a request that
  bypasses the editor entirely (e.g. a raw POST) is still sanitized.
- **SQL Injection**: every query in `RichTextDocumentHelper.cs` is parameterized
  (`SqlParameter`) — no string concatenation of user input into SQL, matching the existing
  `DBHelper` convention.
- **Image upload security** (`RichTextImageUpload.ashx`): requires an authenticated admin
  session; whitelists file **extension** (`.jpg/.jpeg/.png/.gif/.webp`) *and* verifies the
  **actual file bytes** look like that image type (magic-number check) so a renamed `.aspx`
  can't slip through; enforces `RichTextImageMaxSizeMB` (default 5 MB, configurable in
  `Web.config`); saves with a random GUID filename (never the client-supplied name) so path
  traversal / overwrite isn't possible.
- **Role-based authorization**: every new page and the upload handler check
  `Session["AdminName"]` before doing anything; unauthenticated requests are redirected to
  `AdminLogin.aspx` (pages) or receive HTTP 401 JSON (`RichTextImageUpload.ashx`).
- **Request size limits**: `httpRuntime/@maxRequestLength` and IIS
  `requestFiltering/requestLimits/@maxAllowedContentLength` were both raised to 10 MB in
  `Web.config` to comfortably fit a 5 MB image upload.

## 11. Exception Handling (Requirement 11)

Every new database call, the image upload handler, and both Save/Update handlers are wrapped in
try/catch, logged via the existing `AppLogger` (`App_Code/AppLogger.cs`, unchanged), and surface
a friendly Bootstrap alert instead of a raw exception — matching the Task 8 error-handling
pattern already used by `AdminDashboard.aspx.cs`. Uncaught exceptions still fall through to the
site-wide `Error.aspx` / `customErrors` handling from Task 8.

## 12. UI (Requirement 12)

Both new pages reuse `Content/site.css` and Bootstrap 5 (already the project's UI stack — no new
CSS framework introduced), are responsive at mobile/tablet/desktop widths (`table-responsive`,
wrapping toolbar/search controls, TinyMCE's own responsive toolbar), and match the navbar/card/
badge/button styling already established by `AdminDashboard.aspx`.

---

## New / changed files

**New**
- `RichTextEditor.aspx` / `.aspx.cs` / `.aspx.designer.cs` — document list (search/sort/page/view/delete)
- `RichTextEditorEdit.aspx` / `.aspx.cs` / `.aspx.designer.cs` — create/edit + TinyMCE + preview
- `RichTextImageUpload.ashx` — secure image upload endpoint for the editor
- `App_Code/RichTextDocumentHelper.cs` — parameterized CRUD data access
- `App_Code/RichTextSanitizer.cs` — server-side HTML sanitizer
- `Uploads/RichTextImages/` — image upload target folder
- `Task10_Implementation_Notes.md` — this file

**Changed**
- `AdminDashboard.aspx` — added the "Rich Text Editor" navbar menu item
- `Database.sql` — added the `dbo.RichTextDocuments` table (idempotent migration block)
- `Web.config` — added `RichTextImageUploadPath` / `RichTextImageMaxSizeMB` app settings, raised
  `maxRequestLength` and added IIS `requestFiltering` to match
- `StudentRegistrationSystem.csproj` — registered all new files so they're included in the build

---

## Setup

1. Run the new block at the bottom of `Database.sql` against your existing `StudentRegistrationDB`
   (or run the whole script fresh — the `RichTextDocuments` block is guarded with
   `IF OBJECT_ID(...) IS NULL`, so it's safe either way).
2. No new NuGet packages or App_Code assembly references are required — the sanitizer and image
   handler use only classes already referenced by the project
   (`System.Text.RegularExpressions`, `System.Web.Script.Serialization` via the existing
   `System.Web.Extensions` reference).
3. TinyMCE and Bootstrap load from CDN at runtime — the admin machine opening the page needs
   internet access (same as the rest of the app, which already loads Bootstrap from `jsdelivr`).
   For a fully offline deployment, download the `tinymce` npm package and point
   `RichTextEditorEdit.aspx`'s `<script src="...">` at a local copy instead.
4. Confirm `~/Uploads/RichTextImages/` is writable by the application pool identity (same
   requirement as the existing `~/Uploads/Students/` folder from Task 4/5).

## Manual test checklist (Requirement 13)

- [ ] Create a new document with title + formatted content (bold/italic/list/table/image/link) → Save → appears in the list
- [ ] Edit an existing document, change formatting, Update → reload the list and confirm the change persisted with formatting intact
- [ ] Delete a document → confirm prompt appears → row removed after confirming
- [ ] Preview (unsaved) on the edit page matches what appears after Save
- [ ] View (saved) on the list page matches what was saved
- [ ] Save with an empty Title → validation message, no postback to the database
- [ ] Save with empty/whitespace-only content → validation message
- [ ] Insert an image via upload → appears inline, resizes via drag handles, alignment via Image dialog
- [ ] Upload a non-image file renamed to `.png` → rejected by `RichTextImageUpload.ashx`
- [ ] Upload an image over the configured size limit → rejected with a clear message
- [ ] Insert a table, add/remove a row and a column, merge two cells
- [ ] Undo/Redo after several edits
- [ ] Find & Replace a word in a long document
- [ ] Enter Full Screen mode and back out
- [ ] Try pasting `<script>alert(1)</script>` directly into the Title field and Save → confirm it's stored/rendered as literal text, not executed
- [ ] Search the document list by partial title match
- [ ] Sort by Title / Created Date / Last Modified (asc and desc)
- [ ] Page through more than 10 documents
- [ ] Resize the browser to a phone width and confirm the list table and toolbar remain usable
- [ ] Load `RichTextEditor.aspx` and `RichTextEditorEdit.aspx` directly (no session) → redirected to `AdminLogin.aspx`
