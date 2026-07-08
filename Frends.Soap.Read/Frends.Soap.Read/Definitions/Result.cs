using System.Collections.Generic;
using Frends.Soap.Read.Definitions.Enums;

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
    /// SOAP version detected from the payload envelope.
    /// </summary>
    /// <example>Soap11</example>
    public SoapVersion SoapVersion { get; set; }

    /// <summary>
    /// Inner XML of the SOAP Body element from the payload.
    /// </summary>
    /// <example>&lt;GetPriceResponse xmlns="http://tempuri.org/"&gt;&lt;Price&gt;1.99&lt;/Price&gt;&lt;/GetPriceResponse&gt;</example>
    public string Body { get; set; }

    /// <summary>
    /// Headers found inside the SOAP Header element of the payload. Empty when there is no Header element.
    /// </summary>
    /// <example>[{ Name: "Action", Value: "urn:DoWork" }]</example>
    public List<SoapHeader> Headers { get; set; }

    /// <summary>
    /// SOAP Fault contained in the payload body, or null when the payload contains no fault.
    /// </summary>
    /// <example>object { string Code, string Reason, string Actor, string Detail, string Xml }</example>
    public SoapFault Fault { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
