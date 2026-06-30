using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Frends.Soap.Read.Definitions;
using Frends.Soap.Read.Helpers;

namespace Frends.Soap.Read;

/// <summary>
/// Task Class for Soap operations.
/// </summary>
public static class Soap
{
    /// <summary>
    /// Task to read Soap payload
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Soap-Read)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string Output, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result Read(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, options);

            // Cancellation token should be provided to methods that support it
            // and checked during long-running operations, e.g., loops
            cancellationToken.ThrowIfCancellationRequested();

            if (input.Repeat < 0)
                throw new Exception("Repeat count cannot be negative.");

            var output = string.Join(options.Delimiter, Enumerable.Repeat(input.Content, input.Repeat));

            return new Result
            {
                Success = true,
                Output = output,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}
