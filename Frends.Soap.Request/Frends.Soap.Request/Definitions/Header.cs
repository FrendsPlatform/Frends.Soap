using System.ComponentModel;

namespace Frends.Soap.Request.Definitions;

/// <summary>
/// Custom HTTP header.
/// </summary>
public class Header
{
    /// <summary>
    /// Header name.
    /// </summary>
    /// <example>X-Custom-Header</example>
    public string Name { get; set; }

    /// <summary>
    /// Header value.
    /// </summary>
    /// <example>Some value</example>
    public string Value { get; set; }
}

