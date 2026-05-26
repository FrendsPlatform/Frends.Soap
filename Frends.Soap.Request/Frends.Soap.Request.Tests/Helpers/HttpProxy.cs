using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.Helpers;

public sealed class HttpProxy : IAsyncDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Task acceptLoop;

    public HttpProxy()
    {
        var port = GetFreePort();
        ProxyUrl = $"http://localhost:{port}";
        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public string ProxyUrl { get; }

    public async ValueTask DisposeAsync()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();

        try
        {
            await acceptLoop;
        }
        catch
        {
            // Ignore shutdown races.
        }

        cancellationTokenSource.Dispose();
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream)
    {
        var buffer = new List<byte>();
        var lastWasCarriageReturn = false;

        while (true)
        {
            var readBuffer = new byte[1];
            var bytesRead = await stream.ReadAsync(readBuffer, 0, 1);

            if (bytesRead == 0)
                break;

            var currentByte = readBuffer[0];

            if (currentByte == '\n' && lastWasCarriageReturn)
            {
                buffer.RemoveAt(buffer.Count - 1);

                break;
            }

            buffer.Add(currentByte);
            lastWasCarriageReturn = currentByte == '\r';
        }

        return Encoding.ASCII.GetString(buffer.ToArray());
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    private async Task AcceptLoopAsync()
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token);
                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            await using var stream = client.GetStream();

            try
            {
                var requestLine = await ReadLineAsync(stream);

                if (string.IsNullOrWhiteSpace(requestLine))
                    return;

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                while (true)
                {
                    var headerLine = await ReadLineAsync(stream);

                    if (headerLine.Length == 0)
                        break;

                    var separatorIndex = headerLine.IndexOf(':');

                    if (separatorIndex <= 0)
                        continue;

                    headers[headerLine[..separatorIndex]] = headerLine[(separatorIndex + 1)..].Trim();
                }

                var contentLength = 0;
                if (headers.TryGetValue("Content-Length", out var contentLengthValue))
                    int.TryParse(contentLengthValue, out contentLength);

                if (contentLength > 0)
                    await stream.ReadExactlyAsync(new byte[contentLength]);

                var soapVersion = headers.TryGetValue("Content-Type", out var contentType) &&
                                  contentType.Contains("soap+xml", StringComparison.OrdinalIgnoreCase)
                    ? "1.2"
                    : "1.1";

                var soapNamespace = soapVersion == "1.2"
                    ? "http://www.w3.org/2003/05/soap-envelope"
                    : "http://schemas.xmlsoap.org/soap/envelope/";
                var responseXml =
                    $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<soap:Envelope xmlns:soap=\"{soapNamespace}\">\n    <soap:Body>\n        <ProxyResponse xmlns=\"https://example.com/service\"><Status>Proxy response</Status></ProxyResponse>\n    </soap:Body>\n</soap:Envelope>";
                var responseBody = Encoding.UTF8.GetBytes(responseXml);
                var reasonPhrase = "OK";

                var responseHeaders = new StringBuilder();
                responseHeaders.AppendLine($"HTTP/1.1 200 {reasonPhrase}");

                responseHeaders.AppendLine($"Content-Length: {responseBody.Length}");
                responseHeaders.AppendLine("Content-Type: application/xml");
                responseHeaders.AppendLine("Connection: close");
                responseHeaders.AppendLine();

                var responseHeaderBytes = Encoding.ASCII.GetBytes(responseHeaders.ToString());
                await stream.WriteAsync(responseHeaderBytes, cancellationTokenSource.Token);

                if (responseBody.Length > 0)
                    await stream.WriteAsync(responseBody, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Proxy request handling failed: {ex.Message}");
            }
        }
    }
}
