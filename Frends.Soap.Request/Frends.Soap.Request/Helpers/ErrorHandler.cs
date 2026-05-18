using System;
using Frends.Soap.Request.Definitions;

namespace Frends.Soap.Request.Helpers;

internal static class ErrorHandler
{
    /// <summary>
    /// Converts an exception into a failed Result object or rethrows based on task options.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="options"> Task options that control whether failures are returned as a Result object or thrown. </param>
    /// <param name="throwCanceled">
    /// When true, an OperationCanceledException is rethrown immediately.
    /// When false, cancellation is handled like any other failure.
    /// </param>
    /// <returns> A failed Result object when the exception is handled instead of rethrown. </returns>
    internal static Result Handle(this Exception exception, Options options, bool throwCanceled = true)
    {
        ThrowIfCanceled(exception, throwCanceled);
        if (options.ThrowErrorOnFailure) ThrowBaseException(exception, options.ErrorMessageOnFailure);

        return ReturnResult(exception, options.ErrorMessageOnFailure);
    }

    internal static Error CreateError(string message, string additionalInfoMessage)
    {
        return new Error
        {
            Message = message,
            AdditionalInfo = new Exception(additionalInfoMessage),
        };
    }

    private static void ThrowIfCanceled(Exception exception, bool throwCanceled = true)
    {
        if (throwCanceled && exception is OperationCanceledException) throw exception;
    }

    private static void ThrowBaseException(Exception exception, string customMessage = null)
    {
        if (string.IsNullOrEmpty(customMessage))
            throw new Exception(exception.Message, exception);

        throw new Exception(customMessage, exception);
    }

    private static Result ReturnResult(Exception exception, string customMessage = null)
    {
        var errorMessage = string.IsNullOrEmpty(customMessage)
            ? exception.Message
            : $"{customMessage}: {exception.Message}";

        return new Result
        {
            Success = false,
            Error = new Error
            {
                Message = errorMessage,
                AdditionalInfo = exception,
            },
        };
    }
}
