# Task 16 — Centralised Institute Dashboard

Implementation notes for the interns' review, following the format of
`Task12_Implementation_Notes.md` / `Task13_Implementation_Notes.md` /
`Task14_Implementation_Notes.md` / `Task15_Implementation_Notes.md`.

## What this task actually asked for

The Task 16 brief (email, 17 Aug 2026) asks for a **Centralised Institute Dashboard**: a
registered institute logs in with an Institute Id + Password and sees its own profile plus the
active modules assigned to it. Clicking any active module opens a module-specific detail view
with real, institute-scoped information. The brief mandates two exact tables (`instreg`,
`modules`) but leaves the UI and the module-detail content entirely up to us ("You have full
freedom to design the dashboard UI... The above are examples only.").

Rather than inventing throwaway hardcoded numbers for the example modules (Student Management /
Fees Management / Attendance Management), this build wires the dashboard into data that **already
exists** in this database from earlier tasks, so every number on screen is real and
institute-specific:

| Dashboard module | Backed by |
|---|---|
| **Student Management** | `Students` + `StudentAcademicProfile` (Task 1–12) |
| **Fees Management** | `StudentFeeDemands` + `FeeTransactions` (Task 12) |
| **Timetable Management** | `TT_Divisions` / `TT_Timetable` / `TT_Subjects` (Task 15) |

An "Attendance Management" module wasn't built in any earlier task, so it isn't included in the
seed data — nothing in this codebase could back it with real numbers. Adding it later is just one
more `INSERT INTO dbo.modules` row plus one more `case` in `InstituteDashboardHelper.GetModuleDetail`
(see "Extending with a new module" below); the dashboard itself needs no changes.

## New database objects — `Database_Task16_CentralisedDashboard.sql`

Run after `Database.sql`, `Database_Task12_RegistrationFee.sql` and
`Database_Task15_Timetable.sql`. Guarded with `IF OBJECT_ID(...) IS NULL` like every other
migration file, so it's safe to re-run.

| Table | Purpose |
|---|---|
| `instreg` | Institute login credentials — `srno`, `instid`, `pwd`, `instname`, `status`, exactly as specified in the brief |
| `modules` | Per-institute module assignment — `srno`, `instid`, `modulename`, `status`, exactly as specified in the brief |

Both tables use the lowercase column names given in the brief, which is different from the
PascalCase convention used everywhere else in this schema — deliberate, to match the mandatory
spec exactly.

`instreg` is seeded with two logins that reuse the institute names already present in
`dbo.Institutes` (from Task 12), so real academic/fee/timetable data shows up immediately:

| instid | instname | password |
|---|---|---|
| `INST001` | Government College of Engineering, Kolhapur | `Inst@123` |
| `INST002` | Government Polytechnic, Kolhapur | `Inst@123` |

`modules` is seeded with all three modules Active for `INST001`, and one deliberately
`Inactive` for `INST002` (Fees Management) — proof that inactive modules never reach the
dashboard.

**Linking `instreg` to real data:** `instreg` and `modules` only need `instid`, so they satisfy the
brief's mandatory schema exactly. To reach the actual Student/Fees/Timetable numbers,
`InstituteDashboardHelper` resolves the numeric `dbo.Institutes.InstituteID` by matching
`instreg.instname` against `Institutes.InstituteName` at query time — no schema change, no
foreign key added to the mandated tables, just a name lookup. If an institute logs in whose name
doesn't match anything in `dbo.Institutes`, every module detail view degrades gracefully to
"this institute isn't linked to any academic records yet" instead of erroring out.

## App_Code

- **`InstituteAuth.cs`** — `ValidateInstitute(instid, password)` against `instreg`, reusing
  `AdminAuth.ComputeHash` (SHA-256) so the same hashing scheme is used everywhere in the app.
  Passwords are never stored in plain text, even though the brief's mandatory schema just says
  `pwd – Password`.
- **`InstituteDashboardHelper.cs`** — all data access for the dashboard:
  - `GetInstituteProfile` / `GetActiveModules` / `GetActiveModuleCount` — scoped by `instid`.
  - `IsActiveModuleForInstitute` — re-checked on **every** module click, server-side, before any
    detail is loaded. This is what stops institute A from viewing institute B's module details
    (or a deactivated module's details) by guessing/replaying a module name — the mandatory
    isolation requirement.
  - `GetModuleDetail` — dispatches by module name to `GetStudentManagementDetail` /
    `GetFeesManagementDetail` / `GetTimetableManagementDetail`, each returning a small
    `ModuleDetail` (stat tiles + a "recent records" `DataTable`). Any module name not in that list
    still renders a valid page with a "no statistics configured yet" note, rather than failing.

## Pages

| Page | What it covers |
|---|---|
| `InstituteLogin.aspx` | Institute Id + Password + CAPTCHA login. Same shape as `Login.aspx` (student): server-side `RequiredFieldValidator`s, CAPTCHA burned on every attempt, no browser caching of the page. An institute whose `status` isn't `Active` gets a specific message instead of a generic "invalid credentials". |
| `InstituteDashboard.aspx` | Institute Name / Id / Status, total active module count, a clickable card per active module, and a dynamic detail panel (stat tiles + recent-records table) for whichever module was last clicked. Logout clears the session. |

### Mandatory requirements → where they're implemented

- **Institute Login using Institute Id and Password / validated from `instreg`** —
  `InstituteLogin.aspx.cs` → `InstituteAuth.ValidateInstitute`.
- **Institute-wise dashboard; display Institute Name, Id, Status** —
  `InstituteDashboard.aspx` profile cards, from `InstituteDashboardHelper.GetInstituteProfile`.
- **Display active modules, retrieved by `instid`; inactive modules never shown** —
  `InstituteDashboardHelper.GetActiveModules` filters `status = 'Active'` in SQL, not in the UI.
- **One institute must not see another's modules** —
  every query in `InstituteDashboardHelper` is parameterised on the logged-in session's `instid`;
  there is no code path that accepts an arbitrary `instid` from the request.
- **Total active module count** — `GetActiveModuleCount`, shown as a stat tile.
- **Logout** — `btnLogout_Click` calls `Session.Clear()` + `Session.Abandon()`.
- **Session management/validation** — `Page_Load` on `InstituteDashboard.aspx` redirects to
  `InstituteLogin.aspx` whenever `Session["InstId"]` is empty, on every request (not just the
  first). The page also disables browser caching, same as the other login pages in this project.
- **Each active module clickable; opens module-specific details** — the module grid is an
  `asp:Repeater` of `LinkButton`s (`CommandName="SelectModule"`); the click posts back,
  server-side re-validates ownership, then renders that module's stats + recent records.
- **Module details institute-specific, DB-driven, module belongs to logged-in institute** —
  see `IsActiveModuleForInstitute` above and the per-module queries in
  `InstituteDashboardHelper`, all scoped to the resolved `InstituteID`.

## Extending with a new module

1. `INSERT INTO dbo.modules (instid, modulename, status) VALUES ('INST00x', 'Attendance
   Management', 'Active')`.
2. Add one `case "Attendance Management": return GetAttendanceManagementDetail(instituteId);`
   in `InstituteDashboardHelper.GetModuleDetail`, plus the corresponding private method (same
   shape as `GetStudentManagementDetail`).
3. Optionally add an icon mapping in `InstituteDashboard.aspx.cs` → `GetModuleIcon` (falls back to
   a generic grid icon if skipped).

No changes to `InstituteDashboard.aspx` itself are needed — the module grid, stat tiles and
recent-records table are all rendered from whatever `GetModuleDetail` returns.

## Deliberate simplifications (documented, not hidden)

- **Institute self-registration is out of scope.** The brief only asks for *login* against
  `instreg`; there's no mention of an institute sign-up flow, so institute accounts are
  provisioned directly via SQL (`Database_Task16_CentralisedDashboard.sql`), the same way `Admins`
  rows are provisioned today. Adding a self-service registration page is a natural next task if
  it's ever requested.
- **`instreg`/`modules` → `dbo.Institutes` linkage is by name, not by foreign key.** This keeps
  the two mandated tables exactly as specified in the brief (no extra columns), at the cost of
  requiring `instreg.instname` to match `dbo.Institutes.InstituteName` verbatim (whitespace-
  trimmed) for the module details to show real data. A login whose name doesn't match still works
  correctly — it just shows an empty/zeroed detail view instead of erroring.
