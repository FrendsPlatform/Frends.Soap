namespace Frends.Soap.Read.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Body of the Payload
    /// </summary>
    /// <example>foobar,foobar</example>
    public string Body { get; set; }

    /// <summary>
    /// Headers from Payload if any
    /// </summary>
    /// <example>foobar,foobar</example>
    public string Headers { get; set; }

    /// <summary>
    /// Fault message from Payload if any.
    /// </summary>
    /// <example>foobar,foobar</example>
    public string Fault { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
