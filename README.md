# Advanced Student Registration System
ASP.NET Web Forms (.NET Framework 4.8) &bull; SQL Server &bull; Bootstrap 5

## 📁 Project Structure
```
StudentRegistrationSystem/
├── StudentRegistrationSystem.sln    # Open THIS file in Visual Studio
├── StudentRegistrationSystem.csproj # Web Application Project file
├── Global.asax / Global.asax.cs     # Application lifecycle events
├── Properties/AssemblyInfo.cs       # Assembly metadata
├── Database.sql                     # Schema + lookup tables + 50 seed students
├── Web.config                       # Connection string + SMTP settings
├── App_Code/
│   ├── DBHelper.cs                  # ADO.NET data access helper
│   └── EmailHelper.cs               # OTP generation + templated HTML email sender
├── Templates/
│   ├── StudentOTP.html              # Email sent to student with OTP
│   └── AdminNotification.html       # Email sent to admin after registration
├── Home.aspx / .aspx.cs / .designer.cs        # Public landing page + banner slider (Task 13)
├── Register.aspx / .aspx.cs / .designer.cs   # Registration form + OTP flow
├── BulkRegister.aspx / .aspx.cs / .designer.cs  # Bulk staging + Save All (Task 6)
├── Display.aspx / .aspx.cs / .designer.cs    # GridView, search, print, export
├── Login.aspx / .aspx.cs / .designer.cs      # Student login (email + mobile password + CAPTCHA)
├── Dashboard.aspx / .aspx.cs / .designer.cs  # Session-gated profile view + update
├── ChangePassword.aspx / .aspx.cs / .designer.cs  # Session-gated password change (Task 6)
├── CaptchaHandler.ashx               # Generates the distorted CAPTCHA image (GDI+)
└── Uploads/Students/                # Uploaded student photos
```

## ⚙️ Setup Steps

1. **Create the database**
   - Open SQL Server Management Studio.
   - Run `Database.sql` in full. It creates `StudentRegistrationDB`, the
     `Countries` / `States` / `Districts` lookup tables, the `Students`
     table, and inserts 50 mock student records.

2. **Configure `Web.config`**
   - Update `<connectionStrings>` → point `StudentDBConnection` to your SQL
     Server instance.
   - Update `<appSettings>`:
     - `SmtpHost`, `SmtpPort`, `SmtpEnableSSL`, `SmtpUser`, `SmtpPassword`
       (e.g. a Gmail account with an **App Password**, or your institute's
       SMTP relay).
     - `AdminEmail` → mailbox that should receive new-registration alerts.

3. **Open in Visual Studio**
   - Double-click **`StudentRegistrationSystem.sln`** — this opens directly
     as a Web Application Project (Web Forms), no manual project creation
     needed.
   - If Visual Studio prompts to install missing components (e.g. the
     "ASP.NET and web development" workload, or the .NET Framework 4.8
     targeting pack), let it install them — it will show a banner in the
     Solution Explorer or an install dialog on open.
   - Ensure `Uploads/Students/` exists and IIS/IIS Express has write access
     (it does by default under your Windows user account).

4. **Run**
   - Set `Home.aspx` as the start page (this is the new public landing page —
     Task 13 — with the banner slider and links to every other page; you can
     still open `Register.aspx` directly at any time).
   - Fill the form → click **Send OTP** → check the student's inbox →
     enter the OTP → **Verify OTP** → **Register Student**.
   - Visit `Display.aspx` to view, search, print, and export records.
   - Visit `Login.aspx` to sign in as a registered student — **User ID** is
     the registered email address, **Password** is the registered 10-digit
     mobile number. Complete the CAPTCHA to log in.
   - After login you're redirected to `Dashboard.aspx`, which shows the
     student's profile and lets them update it (Student ID and Email stay
     read-only). **Logout** ends the session and returns to `Login.aspx`.

## 🔑 Key Behaviors

- **OTP Verification**: OTP + timestamp + target email are stored in
  `Session`. `btnRegister_Click` is blocked server-side unless
  `Session["Reg_EmailVerified"] == true`, so the check cannot be bypassed
  by disabling client-side JS.
- **Cascading Dropdowns**: Country → State → District use `AutoPostBack`
  inside an `UpdatePanel` for smooth AJAX-style updates without full page
  reloads.
- **Photo Upload**: Only `.jpg`, `.jpeg`, `.png` accepted, max size from
  `Web.config` (`MaxPhotoSizeMB`), saved with a GUID filename to avoid
  collisions.
- **Print**: `window.print()` combined with `@media print` CSS hides the
  navbar/toolbar/buttons and shows a clean report header.
- **Export to Excel**: Renders the `GridView` to an HTML table with
  `application/vnd.ms-excel` content type — opens natively in Excel.
- **Student Login**: `Login.aspx.cs` validates the email against `Students`,
  then compares the entered password to the last 10 digits of the stored
  `Mobile` value (so the same field doubles as the phone number and the
  login password, per the Task 5 spec). CAPTCHA is validated server-side
  against `Session["CaptchaCode"]` before the credential check ever runs.
- **CAPTCHA**: `CaptchaHandler.ashx` draws a 6-character code onto a
  `System.Drawing.Bitmap` (noise lines/dots + rotated glyphs) and stores the
  code in `Session`, so it can't be read from the page source. The image
  refreshes on every full page load/postback, plus a manual refresh button.
- **Session Management**: `Dashboard.aspx.cs` checks
  `Session["StudentID"]` on every `Page_Load` and redirects to `Login.aspx`
  if it's missing, so the dashboard can't be reached by URL alone. Logout
  calls `Session.Clear()` + `Session.Abandon()`.
- **Profile Update**: Editing the mobile number changes the student's login
  password too, since both come from the same `Mobile` column — the edit
  form calls this out with an inline warning. Photo replacement reuses the
  same GUID-filename upload pattern as `Register.aspx.cs`.

## 🆕 Task 6 Additions

- **Bulk Insert (`BulkRegister.aspx`)**: Records are validated and staged in
  a `DataTable` kept in `Session` — nothing touches the database until
  **Save All**. **Add Record** blocks duplicates already in the pending
  list and duplicates already in the database; **Remove Selected Record**
  drops one staged row; **Clear All Records** empties the list. **Save All**
  re-checks all staged emails against the database in a single `IN (...)`
  query, then inserts the rest inside one SQL transaction via
  `DBHelper.ExecuteTransactionalBatch` (all rows commit together, or none
  do). Any email that turns out to already be registered is skipped and
  left in the pending list for correction — it never blocks the others.
  This page intentionally skips the OTP step used on `Register.aspx`,
  since bulk entry is a staff workflow rather than student self-service.
- **Advanced Search & Filter (`Display.aspx`)**: Separate Name / Email /
  Mobile filters plus a Gender dropdown, combined with `AND` in the
  `WHERE` clause. **Reset Filter** clears all of them. Sorting is native
  `GridView` column-header sorting (`AllowSorting`) on Student Name,
  Registration Date, and Email — clicking a header again reverses
  direction. The sortable column is checked against a whitelist before
  ever touching the SQL string, so user input can't reach `ORDER BY`.
- **Prevent Duplicate Registration**: Both `Register.aspx` and
  `BulkRegister.aspx` check the email client-side (a `[WebMethod]`
  `CheckEmailExists` called via `fetch()` on blur — needs
  `EnablePageMethods="true"` on the `ScriptManager`) and again
  server-side right before insert. `Students.Email` also has a `UNIQUE`
  index as a last-resort safety net against race conditions; a
  `SqlException` with error 2601/2627 is caught and shown as the same
  friendly "already registered" message.
- **Dashboard Enhancement**: Added Registration Date and Last Login
  (captured *before* the current login overwrites it, so it shows the
  previous session, not "now") to the profile view, plus **Edit Profile**
  (jumps to the update form), **Change Password**, and **Logout** as quick
  actions.
- **Change Password (`ChangePassword.aspx`)**: Since this system's
  password *is* the registered mobile number (from Task 5), "changing
  password" means replacing that number — the form verifies the current
  one, requires a new 10-digit number + confirmation, and preserves the
  existing country-code prefix when saving.

## 🌐 Third-Party Libraries (CDN, no install needed)
- Bootstrap 5.3.3
- Bootstrap Icons 1.11.3
- intl-tel-input 23.8.0 (phone flags, dial codes, validation)

## ✅ Task 7 — Admin Panel, Admin Dashboard, Candidate Approval & Login Management

Adds a full admin workflow on top of the existing student system, without changing any student-facing screen other than `Login.aspx` (which now enforces approval/account-status gates).

**New files**
- `AdminLogin.aspx` / `.cs` — dedicated Admin Login (CAPTCHA-protected, session-based, styled distinctly from the student portal so the two roles are visually unmistakable).
- `AdminDashboard.aspx` / `.cs` — the Admin Panel: 6 live stat cards (Total / Active / Inactive / Pending / Approved / Rejected), a tabbed + searchable candidate table (Pending, Approved, Rejected, All), and per-row actions.
- `App_Code/AdminAuth.cs` — SHA-256 password hashing + admin credential validation, kept separate from `DBHelper`.
- `Templates/ApprovalNotification.html`, `Templates/RejectionNotification.html` — HTML emails sent on approve/reject (reuses `EmailHelper.SendHtmlEmail`).
- `Database.sql` — migration: adds `ApprovalStatus`, `AccountStatus`, `RejectionRemark`, `ApprovedBy`, `ApprovedDate`, `RejectedBy`, `RejectedDate`, `CreatedDate`, `LastModifiedDate` to `Students` (all with `DEFAULT` constraints, so the existing `Register.aspx.cs` / `BulkRegister.aspx.cs` INSERT statements needed **no changes**), plus a new `Admins` table seeded with a default account.

**Default admin login:** `admin` / `Admin@123` — change this in the `Admins` table before deploying anywhere real.

**Workflow implemented**
1. New registrations default to `ApprovalStatus = Pending`, `AccountStatus = Active`.
2. `Login.aspx` now blocks sign-in and shows a specific message for: pending applications, rejected applications (remark included), and deactivated accounts — approved **and** active is required to log in, on top of the existing email/password check.
3. Admin can **Approve** (enables login, emails the student) or **Reject** (mandatory remark via a modal, saved to `RejectionRemark`, emailed to the student, login blocked).
4. Admin can **Activate/Deactivate** any approved student at any time; an inactive account can't log in even if approved.
5. Admin can **Reset** a rejected application back to `Pending` (clears the remark) for re-review.
6. All admin actions are stamped with `ApprovedBy`/`RejectedBy` (the admin's username) and timestamps, and update `LastModifiedDate`.

**Note on email:** approval/rejection emails are sent best-effort — if SMTP isn't configured (see `Web.config`), the admin action still completes and a success message is shown; the email send is wrapped in try/catch so it never blocks the workflow.

## ✅ Task 8 — Friendlier UI + Import Students from Excel/CSV

**UI refresh (all pages)**
- Added `Content/site.css`, a shared stylesheet loaded by every page (linked right after Bootstrap Icons) so the whole app now has one consistent, softer visual language: rounder cards, gentler shadows, a gradient sticky navbar, nicer focus rings on inputs, hover states on buttons/table rows, and reusable "empty state" / drag-and-drop styling — instead of each page repeating its own inline `<style>` block from scratch.
- No existing page structure, control IDs, or code-behind logic was changed by the CSS refresh — it's a visual layer only, so it's safe to drop into the existing project.

**New: Import from Excel / CSV on the Bulk Registration page**
- `BulkRegister.aspx` now has two ways to build the "pending" list, switched with a pill-style tab control: **Add Manually** (the original one-record-at-a-time form) and **Import from Excel / CSV** (new).
- The Import tab lets you drag-and-drop or browse for a `.xlsx` or `.csv` file, download a ready-made CSV template, and click **Preview File** to see every row validated *before* anything is staged — each row is marked "Ready" or "Skipped" with a plain-English reason (missing field, bad email, unrecognized Country/State/District, duplicate email, etc.).
- Clicking **Add Valid Rows to Pending List** stages only the rows that passed validation into the same "Pending Records" table used by manual entry — so the existing duplicate-checking, **Save All** transactional insert, and per-row **Remove** controls all work unchanged, regardless of which tab was used to add a record.
- Expected file columns: `FullName, Email, Mobile, Gender, DOB, Country, State, District, Address` (Address optional; header names are matched flexibly — e.g. "Full Name", "Date of Birth", "Phone" are all recognized).
- **New files:**
  - `App_Code/FileImportHelper.cs` — a small, dependency-free CSV/XLSX reader. XLSX files are just ZIP+XML under the hood, so this reads the first worksheet directly via `System.IO.Compression` + `System.Xml.Linq` — **no NuGet package (EPPlus/ExcelDataReader/etc.) needs to be installed** to build or run this project.
  - `Content/site.css` — shared styles described above.
- **Project file changes:** `StudentRegistrationSystem.csproj` now references the framework assemblies `System.IO.Compression` and `System.IO.Compression.FileSystem` (both ship with .NET Framework 4.8, nothing to download) and includes the two new files above.
- This feature only stages records in the same Session-held pending list Task 6 already built — it does **not** write to the database until the admin/user clicks **Save All**, so all existing duplicate-prevention and transactional-insert guarantees still apply to imported rows too.
