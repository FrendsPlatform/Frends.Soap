namespace Frends.Soap.Request.Definitions;

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
    /// Response to the message.
    /// </summary>
    /// <example>
    ///     &lt;?xml version="1.0" encoding="UTF-8"?&gt;
    ///     &lt;soap:Envelope xmlns:soap="https://schemas.xmlsoap.org/soap/envelope/"&gt;
    ///     &lt;soap:Body&gt;
    ///         &lt;GetWeatherResponse xmlns="https://example.com/weatherservice"&gt;
    ///             &lt;Temperature&gt;20&lt;/Temperature&gt;
    ///             &lt;Condition&gt;Sunny&lt;/Condition&gt;
    ///         &lt;/GetWeatherResponse&gt;
    ///     &lt;/soap:Body&gt;
    ///     &lt;/soap:Envelope&gt;
    /// </example>
    public string XmlResponse { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
