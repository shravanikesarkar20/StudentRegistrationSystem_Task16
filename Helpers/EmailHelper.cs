using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;

/// <summary>
/// Task 9: Email API Integration.
///
/// Every outbound email in the system (OTP, Registration Confirmation, Admin Notification,
/// Approval, Rejection) is sent through the SendGrid Web API v3 (https://api.sendgrid.com/v3/mail/send)
/// over HTTPS using HttpClient + a JSON payload — a genuine third-party Email API call, not SMTP.
///
/// Responsibilities:
///   1. Loading and populating reusable HTML templates from /Templates.
///   2. Validating inputs (and API configuration) before any API call is made.
///   3. Calling the SendGrid API with a short automatic retry for transient failures
///      (network errors / HTTP 5xx), so one dropped connection doesn't fail the operation.
///   4. Logging every attempt (success or failure) via AppLogger for debugging/monitoring.
///   5. Surfacing failures as a single, well-known EmailDeliveryException type instead of
///      letting raw HttpRequestException / API error bodies leak to callers.
/// </summary>
public static class EmailHelper
{
    private const string LogCategory = "EmailAPI";
    private const string SendGridEndpoint = "https://api.sendgrid.com/v3/mail/send";

    private static readonly Regex EmailRegex =
        new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    // A single shared HttpClient is reused for the app's lifetime (creating a new one per
    // call can exhaust sockets under load) and pointed permanently at the SendGrid API.
    private static readonly HttpClient ApiClient = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    /// <summary>Generates a random 6 digit OTP.</summary>
    public static string GenerateOTP()
    {
        Random rnd = new Random(Guid.NewGuid().GetHashCode());
        return rnd.Next(100000, 999999).ToString();
    }

    #region ---- Template Loading ----

    /// <summary>Loads an HTML template from /Templates and replaces {{Placeholders}}.</summary>
    public static string LoadTemplate(string templateFileName, Dictionary<string, string> placeholders)
    {
        string path;
        try
        {
            path = HttpContext.Current.Server.MapPath("~/Templates/" + templateFileName);
        }
        catch (Exception ex)
        {
            throw new EmailDeliveryException("Could not resolve path for template '" + templateFileName + "'.", ex);
        }

        if (!File.Exists(path))
        {
            throw new EmailDeliveryException("Email template not found: " + templateFileName);
        }

        string body;
        try
        {
            body = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new EmailDeliveryException("Could not read email template: " + templateFileName, ex);
        }

        if (placeholders != null)
        {
            foreach (var kvp in placeholders)
            {
                string value = kvp.Value ?? string.Empty;
                body = body.Replace("{{" + kvp.Key + "}}", HttpUtility.HtmlEncode(value));
            }
        }
        return body;
    }

    #endregion

    #region ---- Validation (Requirement 8) ----

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
    }

    /// <summary>
    /// Validates the minimum required data before any Email API call is made. Throws
    /// EmailDeliveryException with a clear reason on failure so the caller's logs explain
    /// exactly what was missing instead of surfacing an opaque API error later.
    /// </summary>
    private static void ValidateSendRequest(string toEmail, string subject, string htmlBody)
    {
        List<string> problems = new List<string>();

        if (!IsValidEmail(toEmail)) problems.Add("recipient email is missing or invalid");
        if (string.IsNullOrWhiteSpace(subject)) problems.Add("subject is empty");
        if (string.IsNullOrWhiteSpace(htmlBody)) problems.Add("email body is empty");

        string apiKey = ConfigurationManager.AppSettings["SendGridApiKey"];
        string fromAddress = ConfigurationManager.AppSettings["EmailFromAddress"];
        if (string.IsNullOrWhiteSpace(apiKey)) problems.Add("SendGridApiKey is not configured");
        if (!IsValidEmail(fromAddress)) problems.Add("EmailFromAddress is not configured or invalid");

        if (problems.Count > 0)
        {
            string reason = string.Join("; ", problems);
            AppLogger.Warn(LogCategory, "Validation failed before send to '" + toEmail + "': " + reason);
            throw new EmailDeliveryException("Email could not be sent - " + reason + ".");
        }
    }

    #endregion

    #region ---- SendGrid API payload shape ----
    // Minimal POCOs mirroring the subset of the SendGrid Web API v3 /mail/send JSON schema
    // this project needs. Serialized with JavaScriptSerializer (System.Web.Extensions),
    // which is already referenced by the project, so no extra NuGet package is required.

    private class SgAddress
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    private class SgPersonalization
    {
        public List<SgAddress> to { get; set; }
    }

    private class SgContent
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    private class SgMailRequest
    {
        public List<SgPersonalization> personalizations { get; set; }
        public SgAddress from { get; set; }
        public string subject { get; set; }
        public List<SgContent> content { get; set; }
    }

    #endregion

    #region ---- Core Send (Requirement 1 & 8: real API call, reliability, logging) ----

    /// <summary>
    /// Sends an HTML email via the SendGrid Web API v3 (https://api.sendgrid.com/v3/mail/send).
    /// Validates inputs first, retries on transient failures (network errors / HTTP 5xx),
    /// and logs every outcome. Runs synchronously from the caller's point of view (WebForms
    /// button click handlers are synchronous) via ConfigureAwait(false) to avoid deadlocking
    /// on the ASP.NET request context.
    /// </summary>
    public static void SendHtmlEmail(string toEmail, string subject, string htmlBody)
    {
        ValidateSendRequest(toEmail, subject, htmlBody);

        string apiKey = ConfigurationManager.AppSettings["SendGridApiKey"];
        string fromAddress = ConfigurationManager.AppSettings["EmailFromAddress"];
        string fromName = ConfigurationManager.AppSettings["EmailFromName"];

        var payload = new SgMailRequest
        {
            personalizations = new List<SgPersonalization>
            {
                new SgPersonalization { to = new List<SgAddress> { new SgAddress { email = toEmail.Trim() } } }
            },
            from = new SgAddress { email = fromAddress, name = fromName },
            subject = subject,
            content = new List<SgContent> { new SgContent { type = "text/html", value = htmlBody } }
        };

        string json = new JavaScriptSerializer().Serialize(payload);

        int maxAttempts;
        if (!int.TryParse(ConfigurationManager.AppSettings["EmailMaxRetryAttempts"], out maxAttempts) || maxAttempts < 1)
        {
            maxAttempts = 2; // one retry by default
        }

        Exception lastError = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                int statusCode;
                string responseBody;
                CallSendGridApi(apiKey, json, out statusCode, out responseBody);

                // SendGrid returns 202 Accepted on a successful queue-for-delivery.
                if (statusCode == 202)
                {
                    AppLogger.Info(LogCategory, string.Format(
                        "Sent OK -> To='{0}' Subject='{1}' Attempt={2} Status={3}", toEmail, subject, attempt, statusCode));
                    return; // success
                }

                bool isTransient = statusCode == 0 || statusCode == 429 || statusCode >= 500;
                string errMsg = string.Format("SendGrid API returned HTTP {0}: {1}", statusCode, responseBody);
                AppLogger.Error(LogCategory, string.Format(
                    "Send FAILED -> To='{0}' Subject='{1}' Attempt={2}/{3} : {4}",
                    toEmail, subject, attempt, maxAttempts, errMsg));

                lastError = new EmailDeliveryException(errMsg);

                if (attempt == maxAttempts || !isTransient)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
                AppLogger.Error(LogCategory, string.Format(
                    "Send FAILED (transport) -> To='{0}' Subject='{1}' Attempt={2}/{3}",
                    toEmail, subject, attempt, maxAttempts), ex);

                if (attempt == maxAttempts)
                {
                    break;
                }
            }

            Thread.Sleep(500 * attempt); // brief backoff before retrying
        }

        throw new EmailDeliveryException(
            "Failed to send email to " + toEmail + " via the SendGrid API after " + maxAttempts + " attempt(s). " +
            "Please verify SendGridApiKey / EmailFromAddress in Web.config.", lastError);
    }

    /// <summary>
    /// Makes the actual HTTPS call to the SendGrid API. Kept isolated so the retry loop above
    /// stays simple. statusCode is 0 if the request could not complete at all (DNS/timeout/etc).
    /// </summary>
    private static void CallSendGridApi(string apiKey, string jsonPayload, out int statusCode, out string responseBody)
    {
        statusCode = 0;
        responseBody = string.Empty;

        using (var request = new HttpRequestMessage(HttpMethod.Post, SendGridEndpoint))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // ConfigureAwait(false) on every await avoids resuming on the captured ASP.NET
            // SynchronizationContext, which is what prevents this sync-over-async call from
            // deadlocking a WebForms request thread.
            using (HttpResponseMessage response = ApiClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult())
            {
                statusCode = (int)response.StatusCode;
                responseBody = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }

    #endregion

    #region ---- Convenience Wrappers (Requirements 2, 3, 5, 7) ----

    /// <summary>Sends the Student OTP verification email.</summary>
    public static void SendStudentOTP(string studentEmail, string studentName, string otpCode)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "StudentName", studentName },
            { "OTPCode", otpCode }
        };
        string body = LoadTemplate("StudentOTP.html", placeholders);
        SendHtmlEmail(studentEmail, "Your Email Verification OTP", body);
    }

    /// <summary>
    /// Task 9, Requirement 2: sends the student a Registration Confirmation email immediately
    /// after their registration is saved, letting them know their application was received
    /// and is now awaiting Admin review.
    /// </summary>
    public static void SendRegistrationConfirmation(Dictionary<string, string> studentDetails)
    {
        if (studentDetails == null || !studentDetails.ContainsKey("Email"))
        {
            throw new EmailDeliveryException("Registration confirmation email requires student details including Email.");
        }

        string body = LoadTemplate("RegistrationConfirmation.html", studentDetails);
        string studentName = studentDetails.ContainsKey("StudentName") ? studentDetails["StudentName"] : "Student";
        SendHtmlEmail(studentDetails["Email"], "Registration Received - " + studentName, body);
    }

    /// <summary>Sends the Admin notification email after a successful registration.</summary>
    public static void SendAdminNotification(Dictionary<string, string> studentDetails)
    {
        string adminEmail = ConfigurationManager.AppSettings["AdminEmail"];
        string body = LoadTemplate("AdminNotification.html", studentDetails);
        SendHtmlEmail(adminEmail, "New Student Registration - " + studentDetails["StudentName"], body);
    }

    /// <summary>Notifies the student that the Admin approved their application.</summary>
    public static void SendApprovalEmail(string studentEmail, string studentName, int studentId)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "StudentName", studentName },
            { "StudentID", studentId.ToString() }
        };
        string body = LoadTemplate("ApprovalNotification.html", placeholders);
        SendHtmlEmail(studentEmail, "Your Application Has Been Approved", body);
    }

    /// <summary>Notifies the student that the Admin rejected their application, with the reason.</summary>
    public static void SendRejectionEmail(string studentEmail, string studentName, int studentId, string rejectionRemark)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "StudentName", studentName },
            { "StudentID", studentId.ToString() },
            { "RejectionRemark", rejectionRemark }
        };
        string body = LoadTemplate("RejectionNotification.html", placeholders);
        SendHtmlEmail(studentEmail, "Update on Your Application Status", body);
    }

    #endregion
}
