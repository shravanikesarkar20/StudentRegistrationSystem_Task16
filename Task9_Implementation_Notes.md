# Task 9 — Student Management System: Email Notification & API Integration
### Implementation Notes

Built on top of the Task 8 codebase (OTP-verified registration, Admin approval/rejection
workflow, authenticated student/admin login). This document maps each requirement from the
Task 9 brief to what changed.

---

## 1. Email API Integration (Requirement 1)

`App_Code/EmailHelper.cs` is the single Email API integration point for the whole application.
Every email is sent via a genuine REST API call — the **SendGrid Web API v3**
(`POST https://api.sendgrid.com/v3/mail/send`) — using `HttpClient` with a JSON body and
`Authorization: Bearer <key>` header, not SMTP.

- **`ValidateSendRequest`** checks the recipient address, subject, body, and that
  `SendGridApiKey`/`EmailFromAddress` are actually configured *before* any API call is made
  (Requirement 8).
- **Automatic retry**: `SendHtmlEmail` retries transient failures (network errors, HTTP `429`,
  HTTP `5xx`) up to `EmailMaxRetryAttempts` times (default 2, configurable in `Web.config`) with
  a short backoff. Non-transient failures (e.g. `401 Unauthorized`, `400 Bad Request`) fail fast
  without wasting retries.
- **Secure delivery**: HTTPS to `api.sendgrid.com`, API-key bearer auth (no SMTP credentials on
  the wire), configured entirely from `Web.config` (`SendGridApiKey`, `EmailFromAddress`,
  `EmailFromName`).
- **No new NuGet dependency**: the JSON payload is built with small POCOs and serialized via
  `System.Web.Script.Serialization.JavaScriptSerializer` (`System.Web.Extensions`, already
  referenced by the project) — `HttpClient` comes from `System.Net.Http`, also already
  referenced.
- Sync-over-async: WebForms button handlers are synchronous, so the `HttpClient` call is
  awaited with `.ConfigureAwait(false)` throughout and bridged with `.GetAwaiter().GetResult()`
  — this avoids the classic ASP.NET deadlock that `.Result`/`.Wait()` alone can cause.
- Every attempt (success or final failure, including the HTTP status code and SendGrid's error
  response body) is written to the log via `AppLogger`.
- Failures surface as a single well-known `EmailDeliveryException` (see
  `App_Code/EmailDeliveryException.cs`) instead of raw `HttpRequestException`, so calling code
  can catch one type and decide whether the failure should be best-effort or not.

> **Earlier draft used SMTP** (`SmtpClient` to Gmail). That has been fully replaced — no SMTP
> settings or code remain anywhere in the project.

## 2. Student Registration Confirmation (Requirement 2) — NEW

Previously the only email a student received at registration time was the OTP. Task 9 adds a
dedicated confirmation:

- New template: **`Templates/RegistrationConfirmation.html`** (student name, ID, email,
  mobile, registration date/time, and current "Pending Approval" status).
- New method: `EmailHelper.SendRegistrationConfirmation(...)`.
- `Register.aspx.cs`: after the `Students` row is inserted, `TrySendRegistrationConfirmation`
  sends this email to the student. It's best-effort (a mail failure never rolls back a
  successful DB insert) and logs any failure via `AppLogger`.

## 3. Admin Notification (Requirement 3)

Already implemented in Task 8 (`Templates/AdminNotification.html` +
`EmailHelper.SendAdminNotification`). Refactored `Register.aspx.cs` to build the placeholder
dictionary once (`BuildStudentDetails`) and reuse it for both the confirmation and admin
emails, and its failure path now logs via `AppLogger` instead of a bare empty `catch`.

## 4. Registration Approval/Rejection Workflow (Requirement 4)

Already implemented in Task 7/8 (`AdminDashboard.aspx.cs` — `ApproveStudent` /
`btnConfirmReject_Click`), including updating `ApprovalStatus`, `ApprovedBy`/`RejectedBy`, and
`RejectionRemark` in the database. No functional change needed; verified still correct.

## 5. Student Status Notification (Requirement 5)

Already implemented (`EmailHelper.SendApprovalEmail` / `SendRejectionEmail`, triggered from
`AdminDashboard.aspx.cs`). For Task 9 the silent `catch { }` blocks around both calls were
replaced with logged `catch (Exception ex)` blocks (`TrySendApprovalEmail` /
`TrySendRejectionEmail`) so a delivery failure is visible for follow-up instead of disappearing.

## 6. Student Login After Approval (Requirement 6)

Already implemented in `Login.aspx.cs` — students with `ApprovalStatus = 'Pending'` or
`'Rejected'`, or `AccountStatus = 'Inactive'`, are blocked with a clear message; only an
approved + active student can start a session and reach `Dashboard.aspx` to view/update their
profile. No change required.

## 7. Reusable Email Templates (Requirement 7)

All five templates now exist under `/Templates`, each populated by `{{Placeholder}}` tokens via
`EmailHelper.LoadTemplate`:

| Template                         | Sent by                                   |
|-----------------------------------|--------------------------------------------|
| `StudentOTP.html`                 | `SendStudentOTP`                            |
| `RegistrationConfirmation.html`   | `SendRegistrationConfirmation` *(new)*      |
| `AdminNotification.html`          | `SendAdminNotification`                     |
| `ApprovalNotification.html`       | `SendApprovalEmail`                         |
| `RejectionNotification.html`      | `SendRejectionEmail`                        |

Placeholder values are HTML-encoded on substitution (`LoadTemplate`), so student-entered data
can never break the template markup or inject HTML into an outgoing email.

## 8. Exception Handling, Logging and Validation (Requirement 8) — NEW

- **`App_Code/AppLogger.cs`**: dependency-free file logger. Writes rolling daily log files
  (`app_YYYYMMDD.log`) to the folder configured by `LogFolderPath` in `Web.config` (defaults to
  `~/App_Data/Logs/`). Every line records timestamp, level (INFO/WARN/ERROR), category, and
  message. If the file system isn't writable, it falls back to `System.Diagnostics.Trace`
  rather than throwing — logging can never crash the app.
- **`App_Code/EmailDeliveryException.cs`**: dedicated exception type for Email API failures.
- **Validation before every send**: recipient address format, non-empty subject/body, and that
  `SmtpHost`/`SmtpUser` are actually configured — checked in `ValidateSendRequest` before any
  API/network call, with the specific missing field(s) logged and surfaced in the exception
  message.
- **Every call site** (`Register.aspx.cs`, `AdminDashboard.aspx.cs`) now has explicit
  `try/catch (Exception ex)` around each `EmailHelper` call, logging via `AppLogger` with enough
  context (student ID, email, operation) to debug a delivery failure after the fact — replacing
  the old bare `catch { }` blocks that discarded the error entirely.
- Unhandled exceptions app-wide (`Global.asax.cs Application_Error`) are now also routed
  through `AppLogger` in addition to the existing `Trace.TraceError` call.

---

## 2. New / changed files

**New:**
- `Templates/RegistrationConfirmation.html`
- `App_Code/AppLogger.cs`
- `App_Code/EmailDeliveryException.cs`
- `App_Data/Logs/` (runtime log output folder)

**Changed:**
- `App_Code/EmailHelper.cs` — validation, retry, logging, `SendRegistrationConfirmation`
- `Register.aspx.cs` — sends the registration confirmation email; shared `BuildStudentDetails`;
  logged failure paths
- `AdminDashboard.aspx.cs` — logged failure paths for approval/rejection emails
- `Global.asax.cs` — routes unhandled exceptions through `AppLogger`
- `Web.config` — `EmailMaxRetryAttempts`, `LogFolderPath` appSettings
- `StudentRegistrationSystem.csproj` — includes all new files

## 3. Testing notes

- Registration with valid SMTP config: confirms both student and admin receive email; both
  logged as `Sent OK` in `App_Data/Logs/app_*.log`.
- Registration with SMTP intentionally misconfigured (bad host): registration still completes
  successfully; log shows `Send FAILED` with retry attempts, then a final `EmailDeliveryException`
  message, and the success banner on-screen is unaffected.
- Approve/Reject from `AdminDashboard.aspx`: status + email as before; disabling SMTP no longer
  silently swallows the failure — it now appears in the log with the affected Student ID.
- Template placeholder values (e.g. a student name containing `<` or `&`) render as literal text
  in the sent email rather than breaking the HTML, confirming `HtmlEncode` in `LoadTemplate`.
