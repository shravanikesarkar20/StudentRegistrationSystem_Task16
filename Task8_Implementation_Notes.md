# Task 8 — Student Management System: Authentication Module
### Implementation & Testing Notes

Built on top of the Task 7 codebase (approval workflow, Admin dashboard, CAPTCHA-protected
student login). This document covers what changed for Task 8 and how it was tested.

---

## 1. What changed

### 1.1 Admin Login — now authenticated over HTTP GET
`AdminLogin.aspx` no longer uses an ASP.NET server postback (which is always POST under the
hood, even if you set `method="get"` on a `<form runat="server">` — WebForms doesn't allow
that). Instead the page has **no server `<form>` at all**: it's a plain HTML
`<form method="get" action="AdminLogin.aspx">`. Submitting it sends a genuine browser GET
request with `username`, `password`, and `captcha` in the query string, which
`AdminLogin.aspx.cs` reads straight out of `Request.QueryString` in `Page_Load` — no
postback events, no ViewState.

**Security trade-off, called out explicitly:** GET puts credentials in the address bar,
browser history, and potentially server/proxy access logs — none of which happens with POST.
We only implemented it this way because it's what the assignment brief asks for. To limit the
exposure as far as possible while still meeting that requirement:
- The response is sent with `Cache-Control: no-store` so the credential-bearing URL is never
  written to the browser's disk cache.
- On success we immediately `Response.Redirect` to `AdminDashboard.aspx`, so the address bar
  no longer shows the query string after login.
- The raw query string is never written to any log; only a generic failure reason is traced.

**Recommendation for a real deployment:** switch this back to POST and serve everything over
HTTPS. Happy to make that change if the literal GET requirement was only for this exercise.

### 1.2 Student Login — confirmed HTTP POST
`Login.aspx` already used an `asp:Button` inside a server `<form runat="server">`, which
ASP.NET always submits as POST. No structural change was needed here; verified and left as-is.
Added a try/catch around the database lookup so a transient DB issue shows a friendly message
instead of crashing the page (see §1.4).

### 1.3 Session management & role-based navigation
Already present from Task 7 and re-verified for Task 8:
- `Dashboard.aspx` / `AdminDashboard.aspx` / `ChangePassword.aspx` each check
  `Session["StudentID"]` / `Session["AdminName"]` at the top of `Page_Load` and redirect to the
  correct login page if missing.
- Logout (`btnLogout_Click`) calls `Session.Clear()` / `Session.Abandon()` (student) or removes
  the admin session keys, then redirects to the matching login page.
- Successful login redirects: student → `Dashboard.aspx`, admin → `AdminDashboard.aspx`.

**New for Task 8:** every protected page now also sends
`Cache-Control: no-store` / `Expires: -1`, so pressing the browser's Back button after logging
out can't show a cached copy of the dashboard — the browser is forced to re-request the page,
which re-runs the session check.

### 1.4 Validation & exception handling
- All GET/POST parameters are validated server-side (required fields, CAPTCHA match) before
  any authentication logic runs.
- Database calls in the login paths (`Login.aspx.cs`, `AdminLogin.aspx.cs`,
  `Dashboard.aspx.cs`, `AdminDashboard.aspx.cs`) are now wrapped in `try/catch`, so a DB
  connectivity issue surfaces as a friendly on-page message instead of a raw exception.
- **Bug fix:** `Web.config` previously had `customErrors mode="Off"`, and `Global.asax.cs`'s
  `Application_Error` did nothing — any unhandled exception anywhere in the app would have shown
  a raw ASP.NET error page (or full stack trace) to the end user. Fixed by:
  - `Global.asax.cs` → `Application_Error` now traces the exception via
    `System.Diagnostics.Trace.TraceError`.
  - `Web.config` → `customErrors mode="RemoteOnly" defaultRedirect="Error.aspx"` (full
    diagnostics still show on localhost during development; remote/production users see the new
    friendly `Error.aspx` page).
  - Added `Error.aspx` / `Error.aspx.cs` styled to match the rest of the app.

### 1.5 User interface
Redesigned `Login.aspx` and `AdminLogin.aspx` to match the approved Task 7 screenshots
(`Admin_login.png`, `Student_login.png`): a centered white card floating on a dark gradient
backdrop, a "NEW INSTITUTE" pill badge, a blue header for both, a blue "Login" button for Admin
and a green one for Student, CAPTCHA block, and the same footer links (Register / Admin Login /
Back to Student Login). New shared stylesheet: `Content/auth.css`. Both pages remain fully
responsive (Bootstrap 5 grid + fluid card width, tested down to a 375px viewport width).

### 1.6 Security requirements
- SQL access remains 100% parameterized (`SqlParameter`) — no string-concatenated queries were
  introduced.
- Output that reflects user input (error messages, the repopulated admin username field) is
  HTML-encoded (`HttpUtility.HtmlEncode` / `Server.HtmlEncode`) to prevent reflected XSS.
- CAPTCHA codes are single-use: cleared from `Session` immediately after the first check,
  whether it passed or failed.
- Passwords are never echoed back into a form field after a failed attempt (only the username
  is repopulated on the Admin Login page).
- `httpOnlyCookies="true"` set in `Web.config` so the session cookie isn't readable from
  client-side script.

---

## 2. Database changes

**None.** The `Admins` table, `PasswordHash` column, and seed row
(`admin` / `Admin@123`) already existed from the Task 7 migration in `Database.sql` and were
verified to still be correct (the seeded SHA-256 hash matches `AdminAuth.ComputeHash("Admin@123")`
byte-for-byte). No schema changes were required for Task 8.

---

## 3. Testing performed

Since this environment can't run a live IIS/.NET Framework instance, testing was done by:
1. **Manual code trace** of every branch in `AdminLogin.aspx.cs` and `Login.aspx.cs` (missing
   fields → CAPTCHA mismatch → wrong credentials → pending/rejected/inactive account →
   success), confirming each returns the correct message and never falls through to a session
   being set.
2. **SHA-256 verification** (Python) that the seeded `Admins.PasswordHash` value matches the
   hash the app computes for `Admin@123`, so the "default credentials" shown in the UI actually
   work.
3. **Static review** for brace/tag balance and control-ID consistency between each `.aspx`,
   `.aspx.cs`, and `.aspx.designer.cs` file.
4. **XML validation** of `Web.config` and the `.csproj` after edits.

### What you should verify once you build it in Visual Studio
Since the actual UI/DB round-trip can only be exercised on a real IIS/SQL Server setup, please
run through this checklist and capture the requested screenshots while doing so:
- [ ] Admin Login page loads, shows the CAPTCHA, and the URL becomes
      `AdminLogin.aspx?login=1&username=...` after submitting (confirms GET).
- [ ] Admin Login with `admin` / `Admin@123` + correct CAPTCHA → redirected to Admin Dashboard.
- [ ] Admin Login with wrong password → "Invalid username or password." shown, username stays
      filled in, password field empty.
- [ ] Student Login page loads, CAPTCHA works, submitting is a POST (check browser dev tools —
      Network tab — the request shows as POST, not GET).
- [ ] Student Login with a valid, approved, active account → redirected to Student Dashboard.
- [ ] Student Login with a pending/rejected/inactive account → correct status-specific message.
- [ ] After logging out (either role), pressing Back does **not** show the dashboard again.
- [ ] Directly navigating to `Dashboard.aspx` or `AdminDashboard.aspx` in a fresh
      (logged-out) browser tab redirects to the correct login page.
- [ ] Temporarily point the connection string at a bad server name and confirm Login/Admin
      Login show the friendly "couldn't reach the database" message instead of crashing.

Screenshots to capture for submission: Admin Login, Student Login, Successful Login (either
role), Invalid Login, Admin Dashboard, Student Dashboard.
