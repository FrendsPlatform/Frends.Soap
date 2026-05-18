using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;

namespace Frends.Soap.Request;

/// <summary>
/// SOAP request task.
/// [Documentation] https://github.com/FrendsPlatform/Frends.Soap/tree/main/Frends.Soap.Request#readme
/// </summary>
public static class Soap
{
    /// <summary>
    /// Sends a SOAP request using the provided connection, input, and options.
    /// [Documentation] https://github.com/FrendsPlatform/Frends.Soap/tree/main/Frends.Soap.Request#readme
    /// </summary>
    /// <param name="input">Input parameters provided by Frends Platform.</param>
    /// <param name="connection">Connection parameters provided by Frends Platform.</param>
    /// <param name="options">Additional options provided by Frends Platform.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string XmlResponse, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> Request(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection, options);
            cancellationToken.ThrowIfCancellationRequested();

            var (httpClient, shouldDisposeHttpClient) = HttpHandler.GetHttpClient(connection);
            try
            {
                string targetNamespace = null;
                if (options.WsdlSource != WsdlSource.None)
                {
                    var wsdlContent = await WsdlHandler.LoadWsdlContentAsync(options, httpClient, cancellationToken);
                    targetNamespace = WsdlHandler.GetTargetNamespace(wsdlContent);

                    var validationResult = WsdlHandler.ValidateBodyAgainstWsdl(input.MessageBody, wsdlContent);
                    if (!validationResult.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"SOAP body validation against WSDL failed: {validationResult.Error}");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                var soapEnvelope = SoapMessageBuilder.BuildEnvelope(
                    input.MessageBody,
                    options.SoapVersion,
                    options,
                    targetNamespace,
                    connection.Url,
                    input.SoapAction);

                using var httpRequest = HttpHandler.BuildHttpRequest(connection, input, options, soapEnvelope);
                using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var isSoapFault = SoapMessageBuilder.IsSoapFault(responseBody);

                if (response.IsSuccessStatusCode && !isSoapFault)
                {
                    return new Result
                    {
                        Success = true,
                        XmlResponse = responseBody,
                    };
                }

                var errorMessage = isSoapFault
                    ? "SOAP Fault received from the endpoint"
                    : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                var resultErrorMessage = string.IsNullOrEmpty(options.ErrorMessageOnFailure)
                    ? errorMessage
                    : options.ErrorMessageOnFailure;

                if (options.ThrowErrorOnFailure)
                {
                    var messageToThrow = $"{resultErrorMessage}\n{responseBody}";
                    throw new InvalidOperationException(messageToThrow);
                }

                var faultXml = isSoapFault
                    ? responseBody
                    : SoapMessageBuilder.BuildFaultEnvelope(
                        options.SoapVersion == SoapVersion.Soap11 ? "soap:Server" : "soap:Receiver",
                        errorMessage,
                        options.SoapVersion);

                return new Result
                {
                    Success = false,
                    XmlResponse = faultXml,
                    Error = ErrorHandler.CreateError(resultErrorMessage, responseBody),
                };
            }
            finally
            {
                if (shouldDisposeHttpClient)
                    httpClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            if (options.ThrowErrorOnFailure)
            {
                ex.Handle(options);
            }

            var faultXml = SoapMessageBuilder.BuildFaultEnvelope(
                options.SoapVersion == SoapVersion.Soap11 ? "soap:Client" : "soap:Sender",
                ex.Message,
                options.SoapVersion);

            return new Result
            {
                Success = false,
                XmlResponse = faultXml,
                Error = new Error
                {
                    Message = string.IsNullOrEmpty(options.ErrorMessageOnFailure)
                        ? ex.Message
                        : $"{options.ErrorMessageOnFailure}: {ex.Message}",
                    AdditionalInfo = ex,
                },
            };
        }
    }
}
