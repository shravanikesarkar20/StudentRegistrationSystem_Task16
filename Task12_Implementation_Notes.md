# Task 12 — Registration Fee Management (Admin Panel)
### Implementation Notes

Built on top of the Task 11 codebase. Adds a full Registration Fee Management module: admins
configure fee structures, the system auto-generates each student's fee demand, admins record
online/offline payments (allocated across fee heads with transaction integrity), and reconcile
those payments against bank/gateway records.

---

## 1. Architecture decision: Student Academic Profile

The base `Students` table (Task 1–11) has no Course/Year/Category/Institute columns — Register.aspx
only ever collected identity + contact + geography. Fee structures are configured against exactly
those academic axes, so a new `dbo.StudentAcademicProfile` table (one row per student, admin-set)
supplies them. It's set once from **Student Dues** before generating a demand, and is what the
matching engine compares against `FeeStructures`. Extending `Register.aspx` itself to collect this
at signup is a natural follow-on but was out of scope here — every existing student (including all
50 seed records) still works, just with the profile added by the admin the first time it's needed.

## 2. Database Schema (Requirement: Database Schema)

`Database_Task12_RegistrationFee.sql` (idempotent, same `IF OBJECT_ID(...) IS NULL` guard as Task
10/11) adds:

- **Lookups**: `AcademicYears`, `Institutes`, `Courses`, `StudentCategories`, `FeeTypes` — seeded
  with sensible starting data; extend via SQL as the academic catalogue changes.
- **`StudentAcademicProfile`** — per-student Academic Year/Institute/Course/Year-Semester/Category.
- **`FeeStructures`** — one configured fee head per unique combination of the six axes, with
  `FeeAmount`, `DueDate`, `InstallmentsAllowed`/`NumberOfInstallments`, `LateFeeType`
  (Flat/PerDay/Percentage) + value + grace days + optional cap, and `IsActive`. A unique index on
  the six axes prevents duplicate configuration.
- **`FeeStructureInstallments`** — the installment schedule (due date + % share) for a structure.
- **`StudentFeeDemands`** — one row per student per applicable fee structure, auto-generated;
  snapshots `GrossAmount` at generation time so later structure edits never rewrite an issued demand.
- **`FeeInstallmentDemands`** — per-installment breakdown of each demand (due date, amount due,
  amount paid, late fee charged, status).
- **`FeeTransactions`** — one row per payment received (online or offline), with gateway/bank
  reference fields, `Status`, and `ReconciliationStatus`.
- **`FeeTransactionAllocations`** — the audit trail of exactly how each transaction's money was
  split across fee heads/installments (Principal vs LateFee), so "amount paid per fee head" is
  always reconstructible from the ledger, never just a mutable counter.
- **`FeeReconciliationBatches` / `FeeReconciliationRecords`** — one row per uploaded bank/gateway
  statement and per matched/unmatched line within it.

## 3. Backend Logic (Requirement: Backend Logic)

`App_Code/RegistrationFeeHelper.cs` is the single data-access + business-logic point, following the
project's `DBHelper`-parameterized-SQL convention:

- **Configuration** — `SaveFeeStructure` validates (amount > 0, 1–12 installments, valid late fee
  type) and writes the structure + its installment schedule inside one `SqlTransaction`; a unique-
  constraint violation is caught and turned into a friendly "already configured" message instead of
  a raw SQL exception.
- **Automated Generation** — `GenerateFeeDemandsForStudent` finds every *active* `FeeStructure`
  matching the student's academic profile that doesn't already have a demand for them, and creates
  the demand + installment rows inside a transaction. It's idempotent: safe to call repeatedly (a
  "Refresh" button, or a scheduled job later) — it only ever adds newly-configured heads.
- **Late fee** — `ComputeLateFee` implements Flat / PerDay / Percentage rules with a grace period
  and optional cap, computed live as-of "today" for display, and locked in at the moment of payment
  (see below) so the amount actually charged is a stable historical fact, not a moving target.
- **Financial calculations** — `GetStudentFeeSummary` aggregates Total Payable, Amount Paid,
  Outstanding, Late Fee (outstanding + already charged), Discount, Net Payable, and Payment Status
  across every fee head for a student — exactly the six figures the UI requirement asks for.
- **Transaction integrity** — `RecordPayment` is the core of the module: insert the `FeeTransaction`,
  then FIFO-allocate the money across the student's open installments (oldest due date first, late
  fee before principal on each), writing `FeeTransactionAllocations` and rolling the totals back up
  into `FeeInstallmentDemands` and `StudentFeeDemands` — all inside one `SqlTransaction`. If anything
  fails partway through, the whole payment rolls back; a demand can never show money that doesn't
  have a matching, fully-allocated transaction behind it.
- **Reconciliation** — `MatchBankStatement` parses uploaded statement rows and matches each to a
  `FeeTransaction` by Gateway Transaction ID / Bank Reference Number: exact reference + amount match
  auto-reconciles; a reference match with a different amount is flagged `Mismatch` for manual
  review; no reference match is `Unmatched`. `ReconcileTransaction` supports manual
  Reconcile/Dispute for one-off review.

## 4. Frontend UI (Requirement: Frontend UI)

Five new Admin Panel pages, matching the project's Bootstrap 5 + `site.css` + `DBHelper`-session
conventions (`Session["AdminName"]` guard, no-cache headers, redirect to `AdminLogin.aspx`):

| Page | Purpose |
|---|---|
| `FeeStructureManagement.aspx` | List/search configured fee structures, toggle active/inactive, delete (only if unused) |
| `FeeStructureEdit.aspx` | Configure one fee structure: all six axes, amount, due date, installment schedule (auto-split, editable), late fee rule |
| `StudentFeeDemand.aspx` | Search a student, set/edit their academic profile, generate/refresh their fee demand, view the six-figure summary + per-fee-head breakdown + payment history, apply discounts inline |
| `RecordPayment.aspx` | Record one online or offline payment for a student; shows the resulting allocation breakdown per fee head/installment |
| `FeeReconciliation.aspx` | Manually mark transactions Reconciled/Disputed; upload a bank/gateway statement CSV for automatic matching |

`AdminDashboard.aspx` navbar got a new **Registration Fees** entry (same pattern as the Task 10/11
entries) linking into the module; the other admin pages should get the same link added — see
"Integration Steps" in the covering message.

## 5. Validation & error handling

- Fee amount, installment count (1–12), late fee type, and Year/Semester are validated server-side
  before any write; a duplicate axis combination is caught at the database (unique index) and
  surfaced as a friendly message.
- A discount can never exceed what's still owed on a fee head (validated against `Gross - Paid`).
- Payment amount must be > 0 and payment mode must be one of the recognized values.
- Every write path is wrapped in try/catch, logged via `AppLogger`, and shows a Bootstrap alert
  instead of a raw exception — matching the Task 8/10/11 error-handling pattern.
- `RecordPayment`, `SaveFeeStructure`, and `GenerateFeeDemandsForStudent` each use a single
  `SqlTransaction` so a failure partway through never leaves a partial/inconsistent financial state.

## New / changed files

**New**
- `App_Code/RegistrationFeeHelper.cs` — all fee configuration, generation, payment, and
  reconciliation logic
- `FeeStructureManagement.aspx` / `.aspx.cs` / `.aspx.designer.cs`
- `FeeStructureEdit.aspx` / `.aspx.cs` / `.aspx.designer.cs`
- `StudentFeeDemand.aspx` / `.aspx.cs` / `.aspx.designer.cs`
- `RecordPayment.aspx` / `.aspx.cs` / `.aspx.designer.cs`
- `FeeReconciliation.aspx` / `.aspx.cs` / `.aspx.designer.cs`
- `Database_Task12_RegistrationFee.sql` — schema migration
- `Uploads/Reconciliation/` — placeholder folder (statement files are parsed in-memory and not
  persisted to disk; the folder is reserved for a future "keep original upload" enhancement)

**Changed**
- `AdminDashboard.aspx` — added the "Registration Fees" navbar entry
- `StudentRegistrationSystem.csproj` — registered all new files so they're included in the build

## Setup

1. Run `Database_Task12_RegistrationFee.sql` against `StudentRegistrationDB` (after the base
   script and Task 7/10/11 blocks). It's idempotent — safe on a fresh or already-running database.
2. No `Web.config` changes are required — the existing `maxRequestLength="10240"` (10 MB) from
   Task 10 comfortably covers a reconciliation CSV upload.
3. No new NuGet packages — everything reuses `System.Data.SqlClient` / `System.Web`, already
   referenced by the project.

## Manual test checklist

- [ ] Configure a fee structure (e.g. Tuition Fee, 2025-26, GCOE Kolhapur, B.Tech CSE, Year 1, Open)
      with 2 installments and a Flat ₹100 late fee, 7-day grace → appears in the list, Active
- [ ] Try configuring the exact same six axes again → rejected with a friendly duplicate message
- [ ] Open **Student Dues**, search and select a student, set their Academic Profile to match →
      Save
- [ ] Click **Generate / Refresh Fee Demand** → the fee head appears with correct Gross/Due Date;
      clicking again produces "No new fee heads to generate"
- [ ] Apply a discount smaller than the outstanding amount → Net Payable drops accordingly; try a
      discount larger than owed → rejected
- [ ] Click **Record Payment**, pay an amount covering only the first installment → allocation
      breakdown shows it applied to Installment 1 Principal; Student Dues now shows PartiallyPaid
- [ ] Back-date a fee structure's due date (or wait past it) and pay after the grace period →
      allocation shows a LateFee line charged before Principal
- [ ] Pay the remaining balance → fee head status becomes Paid, Payment Status becomes Paid
- [ ] Open **Fee Reconciliation**, manually mark a transaction Reconciled → status badge updates
- [ ] Upload a CSV (`GatewayTxnRefFromAbove,<exact amount>`) → transaction auto-reconciles;
      upload one with a mismatched amount → flagged for review, transaction stays Unreconciled
- [ ] Load any of the five new pages directly with no admin session → redirected to
      `AdminLogin.aspx`
