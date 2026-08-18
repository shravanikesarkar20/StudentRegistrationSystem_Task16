using System;

/// <summary>
/// Task 9, Requirement 8: thrown by <see cref="EmailHelper"/> when an Email API operation
/// (template load, validation, or SMTP send) fails, after the failure has already been logged.
/// Lets callers decide per-call whether a failed notification should block the surrounding
/// business operation (e.g. registration) or be treated as best-effort.
/// </summary>
public class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message) : base(message) { }
    public EmailDeliveryException(string message, Exception inner) : base(message, inner) { }
}
