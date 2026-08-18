# Task 15 — Automated Timetable Management System

Implementation notes for the interns' review, following the format of `Task12_Implementation_Notes.md` / `Task13_Implementation_Notes.md` / `Task14_Implementation_Notes.md`.

## What this task actually asked for

The Task 15 brief (email, 13 Aug 2026) is a **new, self-contained module** — an automated
timetable/scheduling engine for students, faculty, classrooms and labs — layered on top of the
existing Advanced Student Registration System. It reuses the Task 12 `AcademicYears` /
`Institutes` / `Courses` lookup tables but otherwise introduces its own data model, since nothing
in the registration/fee/banner work covered scheduling.

> **Note on naming:** this deliverable is filed as **Task 15** because that's what the attached
> brief is titled (subject line "Task 15 – Automated Timetable Management System", commencing
> 13 Aug 2026). Task 14 (candidate carousel on the Home page) was already completed in the zip
> you uploaded — this build starts from that state and adds Task 15 on top of it.

## New database objects — `Database_Task15_Timetable.sql`

Run after `Database.sql` and `Database_Task12_RegistrationFee.sql`. Guarded with
`IF OBJECT_ID(...) IS NULL` like every other migration file, so it's safe to re-run.

| Table | Purpose |
|---|---|
| `TT_WorkingDays` | Which days of the week are teaching days (Section B) |
| `TT_Periods` | Period numbers, start/end times, break flag (Section B) |
| `TT_ScheduleConfig` | Soft cap: max classes/day per division/faculty used by the generator |
| `TT_Divisions` | Classes/divisions per course + year/sem + academic year, with strength (Section A) |
| `TT_Subjects` | Subjects with type (Theory/Practical/Tutorial) and weekly hours (Section A) |
| `TT_Faculty` | Teaching staff (separate from the system `Admins` table) |
| `TT_FacultySubjects` | Which faculty teaches which subject for which division |
| `TT_FacultyAvailability` | Explicit available (day, period) slots — **absence of rows = available every period** (default-open model, so setup stays optional) |
| `TT_Rooms` | Classrooms/labs with type, capacity, building (Section D) |
| `TT_RoomAvailability` | Same default-open pattern as faculty availability |
| `TT_Timetable` | The generated/edited schedule: one row per (division, day, period[, batch]) |
| `TT_UnresolvedRequirements` | What the auto-generator couldn't place, and why (Section F) |

## App_Code

- **`TimetableSetupHelper.cs`** — CRUD for working days/periods, divisions, subjects, faculty
  (+ assignments + availability), rooms (+ availability). Same ADO.NET/DataTable style as
  `DBHelper` / `RegistrationFeeHelper`.
- **`TimetableConflictDetector.cs`** — the single source of truth for "is this placement valid?"
  Checks break/working-day validity, room capacity vs class strength, Practical-subject-needs-lab,
  faculty/room availability, and the three clash types (Faculty/Room/Class conflict). Used by
  *both* the generator and the manual editor/swap, so generated and hand-edited slots are held to
  the same standard.
- **`TimetableGenerationEngine.cs`** — Section E/F. A constructive placer: for each division it
  orders subjects (most weekly-hours and Practical subjects first, since labs are the scarcer
  resource), then searches day/period/faculty/room combinations and commits the first
  conflict-free one. Anything it can't place after exhausting options is logged to
  `TT_UnresolvedRequirements` rather than silently dropped — Section F: *"Even if automatic
  generation is not able to resolve all constraints, the system should identify conflicts."*
- **`TimetableHelper.cs`** — reading a division/faculty/room's timetable, rendering the shared
  HTML grid, `TryMoveEntry` (Section G — validates before writing, nothing changes on conflict),
  and `TrySwapEntries` (Section H — validates both halves of the swap, reports which side failed
  and why, and only writes if the whole swap is clean).

## Admin Panel pages

| Page | What it covers |
|---|---|
| `TimetableSetup.aspx` | Working days, periods, generation limits, Divisions, Subjects (Sections A/B) |
| `TimetableFacultyManagement.aspx` / `TimetableFacultyEdit.aspx` | Faculty CRUD, subject/class assignment, availability grid (Section C) |
| `TimetableRoomManagement.aspx` / `TimetableRoomEdit.aspx` | Room/lab CRUD, availability grid (Section D) |
| `TimetableGenerate.aspx` | Pick academic year + division, auto-generate, view the grid and any unresolved conflicts (Section E/F) |
| `TimetableEditor.aspx` | Move a class (re-validated live) and swap two classes (Section G/H) |
| `RoomTimetableView.aspx` | Per-room utilization, to spot unused rooms and clashes (Section I-c) |

## Public pages (no login, linked from `Home.aspx`)

- **`ClassTimetableView.aspx`** — a student picks their division and views its published grid (Section I-b).
- **`FacultyTimetableView.aspx`** — a faculty member picks their own name and sees their weekly schedule across every division they teach (Section I-a).

## Deliberate simplifications (documented, not hidden)

- **Availability model:** a faculty/room with *no* explicit availability rows is treated as
  available every non-break period. This keeps setup optional for the common case; admins only
  need to configure availability for people/rooms with real restrictions.
- **Lab batch splitting:** the schema supports a `BatchLabel` column (e.g. `B1`/`B2`) so a
  division can be split across parallel lab sessions, but the auto-generator itself always places
  whole-division entries (`BatchLabel = ''`) — batch splits are something an admin creates via the
  manual editor today. Extending the generator to auto-split batches is a natural next step.
- **Generation heuristic:** the engine is a greedy constructive placer (longest/practical subjects
  first, first conflict-free slot wins), not a full constraint solver — it's deliberately simple so
  its behavior is easy to reason about and its failures are always explained in
  `TT_UnresolvedRequirements` rather than hidden.
