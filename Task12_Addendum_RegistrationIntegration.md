# Task 12 Addendum — Registration Form &amp; Student Dashboard Fee Integration

### Implementation Notes

Task 12's original scope (see `Task12_Implementation_Notes.md`) covered the Admin Panel side of
Registration Fee Management only, and explicitly called out two things as deferred follow-ons:

1. `Register.aspx` never collected Course/Year/Category/Institute, so every student's
   `StudentAcademicProfile` had to be set manually by an Admin on **Student Dues** before a fee
   demand could be generated.
2. There was no student-facing view of fee dues at all — only the Admin Panel could see them.

This addendum closes both gaps.

---

## 1. Registration form: Course &amp; Academic Details

`Register.aspx` gets a new **"Course & Academic Details"** section (step 4, between Location and
Photo) with five dropdowns: **Institute**, **Course** (cascading on Institute, same
`AutoPostBack` + `UpdatePanel` pattern already used for Country → State → District),
**Year / Semester** (fixed list from `RegistrationFeeHelper.YearSemesterOptions`), **Academic
Year**, and **Student Category** — the exact five axes `StudentAcademicProfile` (and fee
matching) require. All five are required server-side, matching the validation style already used
for Country/State/District.

`Course` was the only field literally named in the brief, but Year/Semester alone isn't enough to
generate a correct fee demand — `FeeStructures` is keyed on all five axes together (e.g. an SC
student and an Open-category student in the same course/year can owe different amounts), so the
other four fields are collected in the same step rather than leaving the profile half-set.

On successful registration (`btnRegister_Click`), after the `Students` row is inserted:

- `RegistrationFeeHelper.SaveStudentAcademicProfile(...)` persists the five selections as the
  student's profile (the same call the Admin's **Student Dues** page uses).
- `RegistrationFeeHelper.GenerateFeeDemandsForStudent(...)` immediately runs, so if a matching
  fee structure is already configured, the student's fee demand exists the moment they register
  — no Admin step required in between.
- Both calls are wrapped together in `TrySaveAcademicProfileAndGenerateFees`, a best-effort helper
  (logged via `AppLogger` on failure) so a fee-side hiccup can never roll back an otherwise-
  successful registration — same pattern as the Task 9 email sends right above it.
- The success message adapts based on whether any fee heads were actually generated (i.e.
  whether a structure has been configured yet for that combination), so the student isn't told to
  "check their fees" when there's nothing there yet.

## 2. Student Dashboard: My Fees

`Dashboard.aspx` gets a new **My Fees** card, shown right after the profile overview (with a
navbar shortcut next to "Edit Profile"), read-only and scoped to `Session["StudentID"]` only —
students can view but never modify their own fee data.

- `LoadMyFees()` (called on first load, alongside the existing `LoadProfile()`) calls
  `RegistrationFeeHelper.GetStudentFeeSummary()` for the same six figures + status the Admin sees
  on **Student Dues**: Total Payable, Amount Paid, Outstanding, Late Fee, Scholarship/Discount,
  Net Payable, Payment Status — exactly the seven items listed in the Task 12 brief.
- `GetStudentFeeDemands()` backs a read-only fee-head breakdown table (Fee Type, Gross, Discount,
  Paid, Outstanding, Late Fee, Due Date, Status), reusing the same `badge-status`/`summary-tile`
  styling as the Admin's Student Dues page for visual consistency.
- `GetTransactionsForStudent()` backs a Payment History table (Receipt No., Date, Mode, Amount,
  Status) so a student can see what they've already paid.
- If no demand has been generated yet (`PaymentStatus == "No Dues Generated"`), the card shows a
  friendly explanatory message instead of an empty table.
- Wrapped in try/catch so a fee-module issue never breaks the rest of the dashboard (profile
  view/edit keeps working regardless).

## New / changed files

**Changed**
- `Register.aspx` / `.aspx.designer.cs` — new Course & Academic Details section
- `Register.aspx.cs` — lookup loading, cascading Institute→Course dropdown, validation, profile
  save + fee generation on successful registration
- `Dashboard.aspx` / `.aspx.designer.cs` — new My Fees card + navbar link
- `Dashboard.aspx.cs` — `LoadMyFees()`

No database changes — this reuses the `StudentAcademicProfile` / fee-demand schema and
`RegistrationFeeHelper` methods already shipped with Task 12.

## Manual test checklist

- [ ] Register a new student, filling Institute → Course (list refreshes for the chosen
      institute) → Year/Semester → Academic Year → Student Category → submit with one of the five
      left blank → rejected with "Please select Institute, Course, Year/Semester..."
- [ ] With a matching active `FeeStructure` already configured (Admin > Registration Fees) for
      the exact combination picked, register a student → success message says fee details are
      ready → log in as that student → **My Fees** shows the correct Total Payable/Due Date
      immediately, no Admin step needed
- [ ] Register a student for a combination with **no** configured fee structure → success message
      says fee details will appear once configured → **My Fees** shows the "not available yet"
      message, not an error
- [ ] As Admin, configure a fee structure for a combination matching an *existing* student (one
      registered before this change, no profile) → generate their demand from **Student Dues** as
      before → that student's **My Fees** now shows it too (dashboard reads live data)
- [ ] Record a payment for a student (Admin > Record Payment) → student's **My Fees** → Amount
      Paid/Outstanding update and Payment History shows the new receipt
- [ ] Confirm a student can only ever see their own fees (no student ID parameter is accepted —
      the page always uses `Session["StudentID"]`)
