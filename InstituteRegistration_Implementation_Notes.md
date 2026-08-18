# Institute Self-Registration, Admin Approval + Duplicate Country/State Fix

Follow-up work requested after Task 16, in the same format as the numbered task notes.

## Part 1 — Institute Self-Registration + Admin Approval

### What was asked

An institute registration form capturing essential details (capacity, courses, etc.), with the
registered institute appearing in the student Registration form's cascading Institute dropdown
only after an admin approves it.

### Database — `Database_InstituteRegistration.sql`

Extends the existing `dbo.Institutes` table (introduced in Task 12 as a simple lookup) rather than
creating a parallel table, so every part of the app that already reads `dbo.Institutes` (Register.aspx,
FeeStructureEdit.aspx, StudentFeeDemand.aspx, the Task 16 dashboard) keeps working unchanged. New
columns: `Capacity`, `Address`, `City`, `ContactEmail`, `ContactPhone`, `Website`, `CoursesOffered`
(free text), plus the approval workflow columns `ApprovalStatus` (Pending/Approved/Rejected),
`RejectionRemark`, `SubmittedDate`, `ApprovedBy`/`ApprovedDate`, `RejectedBy`/`RejectedDate`.
Institutes already in the table (the two seeded by Task 12) are grandfathered as `Approved`, the
same pattern used for the 50 mock students in `Database.sql`.

Run this after `Database.sql` and `Database_Task12_RegistrationFee.sql`.

### `App_Code/InstituteRegistrationHelper.cs`

- `RegisterInstitute(...)` — inserts a new institute as `Pending` / `IsActive = 0`. It cannot be
  selected anywhere in the app until approved.
- `IsInstituteNameTaken(name)` — case/whitespace-insensitive uniqueness check used by the
  registration form.
- `GetInstitutesByStatus(status, search)` — backs the admin's Pending/Approved/Rejected/All tabs.
- `ApproveInstitute(id, approvedBy)` — sets `Approved` + `IsActive = 1`, **and** copies every
  non-blank line/comma-item of `CoursesOffered` into `dbo.Courses` (skipping anything that already
  exists for that institute), so the Course dropdown that cascades from Institute in
  `Register.aspx` has real rows the moment the institute is approved — no separate "add courses"
  step for the admin.
- `RejectInstitute(id, rejectedBy, remark)` — sets `Rejected` + `IsActive = 0`.

### Pages

| Page | What it covers |
|---|---|
| `InstituteRegister.aspx` | Public, no-login form: Institute Name, Capacity, Address/City, Website, Contact Email/Phone, Courses Offered (one per line). Submits as Pending; shows a "submitted for review" confirmation, not a login or dashboard. |
| `InstituteManagement.aspx` | New Admin Panel page (linked from `AdminDashboard.aspx`'s nav). Pending/Approved/Rejected/All tabs, search, Approve / Reject (with a required remark, same modal pattern as the student-candidate approval screen) / Activate / Deactivate. Session-gated exactly like `AdminDashboard.aspx`. |

### How approval reaches the Registration form

`RegistrationFeeHelper.GetInstitutes()` — the single method every Institute dropdown in the app
already calls (`Register.aspx`, `FeeStructureEdit.aspx`, `StudentFeeDemand.aspx`) — now filters on
`ApprovalStatus = 'Approved'` in addition to the existing `IsActive` filter. Pending and Rejected
institutes are excluded at the SQL level, not hidden in the UI, so there's no path that could leak
an unapproved institute into the cascading dropdown.

### Deliberate simplifications

- **Courses Offered is free text, not a structured multi-row form.** Keeps the registration form to
  one page and one save; the text is parsed into real `dbo.Courses` rows automatically on approval
  (see `ApproveInstitute` above), so the eventual data shape is the same either way.
- **No institute dashboard login is created by this form.** This form only creates a catalog entry
  in `dbo.Institutes` (what shows up in the student Registration form). The Task 16 Centralised
  Dashboard login (`dbo.instreg`) is a separate credential, currently still provisioned by an admin
  directly in SQL. Auto-creating an `instreg` row on approval (so a newly approved institute gets
  dashboard access immediately) is a natural next step if that's wanted.

## Part 2 — Fix: Country/State appearing twice in Registration form

### Root cause

`Database.sql`'s first block unconditionally runs:

```sql
IF OBJECT_ID('dbo.Students', 'U') IS NOT NULL DROP TABLE dbo.Students;
IF OBJECT_ID('dbo.Districts', 'U') IS NOT NULL DROP TABLE dbo.Districts;
IF OBJECT_ID('dbo.States', 'U') IS NOT NULL DROP TABLE dbo.States;
IF OBJECT_ID('dbo.Countries', 'U') IS NOT NULL DROP TABLE dbo.Countries;
```

...then unconditionally re-`CREATE`s and re-`INSERT`s all the seed data. That's correct the first
time the database is built. Once `Database_Task12_RegistrationFee.sql` has been applied, though,
`dbo.StudentAcademicProfile`, `dbo.StudentFeeDemands` and `dbo.FeeTransactions` all hold foreign
keys into `dbo.Students`. If `Database.sql` is ever re-run after that point, `DROP TABLE
dbo.Students` fails with a foreign-key error - and because a runtime error doesn't stop the rest of
a T-SQL batch, the script carries on: `Districts`/`States`/`Countries` fail to drop too (Students
still references them), the `CREATE TABLE` statements fail (already exist), and the `INSERT`
statements underneath them succeed anyway - silently appending a second copy of every country,
state and district row on top of the existing ones. That's the "Country/State appearing double"
symptom in the Registration form's dropdowns (and would eventually duplicate the 50 mock students
too, on a further re-run).

### The fix

- **`Database_Fix_DuplicateLocations.sql`** — a one-time, idempotent cleanup script. Dedupes
  Countries (by name) → States (by name + country) → Districts (by name + state), in that order,
  re-pointing every referencing row (`States`, `Districts`, `Students`) to the lowest-ID "kept" copy
  before deleting the duplicates. Safe to run multiple times - if there are no duplicates, every
  step is a no-op. **Run this once against the affected database** to clear out the current
  duplicates.
- **`Database.sql`** now has a warning comment at the top making clear it's a first-time-setup-only
  script and must not be re-run once Task 12+ migrations are applied - pointing here if it ever
  happens again.

No application code needed to change for this part; `Register.aspx.cs`'s cascading-dropdown binding
(`LoadCountries`/`LoadStates`/`LoadDistricts`) was already correct - the duplication was purely in
the underlying data.
