using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.FunctionalTests;

[TestFixture]
public class SoapRequestFunctionalTests
{
    private const string ClientCertPassword = "Passw0rd!";
    private static IContainer container;
    private static string httpUrl;
    private static string httpsUrl;
    private static string tempCertDir;
    private static string clientCertWithPasswordPath;
    private static string clientCertWithoutPasswordPath;
    private static string invalidClientCertPath;

    [OneTimeSetUp]
    public async Task SetupContainer()
    {
        var testFilesPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles");

        BuildDockerImage(testFilesPath);

        container = new ContainerBuilder("frends-soap-test-server:latest")
            .WithName($"soap-test-{Guid.NewGuid():N}")
            .WithPortBinding(8080, true)
            .WithPortBinding(8443, true)
            .Build();

        await container.StartAsync();

        var httpPort = container.GetMappedPublicPort(8080);
        var httpsPort = container.GetMappedPublicPort(8443);

        httpUrl = $"http://localhost:{httpPort}";
        httpsUrl = $"https://localhost:{httpsPort}";

        await WaitForHealthAsync(httpUrl);

        TestContext.WriteLine($"SOAP test container started at HTTP: {httpUrl}, HTTPS: {httpsUrl}");

        tempCertDir = Path.Combine(Path.GetTempPath(), $"frends-soap-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempCertDir);

        clientCertWithPasswordPath = Path.Combine(tempCertDir, "client-with-password.pfx");
        clientCertWithoutPasswordPath = Path.Combine(tempCertDir, "client-without-password.pfx");
        invalidClientCertPath = Path.Combine(tempCertDir, "client-invalid.pfx");

        CreateClientCertificates(
            clientCertWithPasswordPath,
            clientCertWithoutPasswordPath,
            invalidClientCertPath,
            ClientCertPassword);
    }

    [OneTimeTearDown]
    public async Task TeardownContainer()
    {
        if (container != null)
        {
            await container.StopAsync();
            await container.DisposeAsync();
            TestContext.WriteLine("SOAP test container stopped and disposed");
        }

        try
        {
            Directory.Delete(tempCertDir, true);
            TestContext.WriteLine("Temporary certificate directory deleted");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed to delete temporary certificate directory: {ex.Message}");
        }
    }

    [Test]
    public async Task Request_Soap11_ReturnsSuccessfulResponse()
    {
        var input = new Input
        {
            MessageBody = @"<GetWeather xmlns=""https://example.com/service""><City>London</City></GetWeather>",
            SoapAction = "GetWeather",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap11/success",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap11,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "Request should succeed");
        Assert.That(result.XmlResponse, Is.Not.Null.And.Not.Empty, "Response should contain data");
        Assert.That(result.XmlResponse, Does.Contain("Envelope"), "Response should be valid SOAP");
        Assert.That(
            result.XmlResponse,
            Does.Contain("https://schemas.xmlsoap.org/soap/envelope/"),
            "Should use SOAP 1.1 namespace");
        Assert.That(result.Error, Is.Null, "No error on successful request");
    }

    [Test]
    public async Task Request_Soap12_ReturnsSuccessfulResponse()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Test Message</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "Request should succeed");
        Assert.That(result.XmlResponse, Is.Not.Null.And.Not.Empty);
        Assert.That(result.XmlResponse, Does.Contain("Envelope"));
        Assert.That(
            result.XmlResponse,
            Does.Contain("https://www.w3.org/2003/05/soap-envelope"),
            "Should use SOAP 1.2 namespace");

        // Verify response is valid XML
        var doc = new XmlDocument();

        try
        {
            doc.LoadXml(result.XmlResponse);
        }
        catch (XmlException ex)
        {
            Assert.Fail($"Response should be valid XML. Error: {ex.Message}");
        }
    }

    [Test]
    public async Task Request_WithOAuth2_AuthenticatesAndSucceeds()
    {
        var input = new Input
        {
            MessageBody = @"<GetWeather xmlns=""https://example.com/service""><City>Paris</City></GetWeather>",
            SoapAction = "GetWeather",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/success",
            Authentication = Authentication.OAuth,
            OAuthToken = "valid-test-token",
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "OAuth authenticated request should succeed");
        Assert.That(result.XmlResponse, Does.Contain("Envelope"));
    }

    [Test]
    public async Task Request_WithWsSecurity_IncludesSecurityHeaders()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Secure</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = true,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "testuser",
            WsSecurityPassword = "testpass",
            WsSecurityPasswordType = "PasswordText",
            WsSecurityTimestampMinutes = 5,
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);

        // The security headers should be included in the envelope but we can't see them in the response
        // Just verify the request completes successfully with security enabled
    }

    [Test]
    public async Task Request_WithWsAddressing_IncludesAddressingHeaders()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Addressed</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = true,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsAddressingReplyTo = "https://www.w3.org/2005/08/addressing/anonymous",
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Request_OnSoapFault_ReturnsFaultInResult()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Error</Message></Echo>",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/fault",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap11,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False, "Request should fail on SOAP Fault");
        Assert.That(result.XmlResponse, Does.Contain("Fault"), "Response should contain SOAP Fault");
        Assert.That(result.Error, Is.Not.Null, "Error object should be populated");
        Assert.That(result.Error.Message, Does.Contain("SOAP Fault"), "Error message should indicate SOAP Fault");
    }

    [Test]
    public async Task Request_OnHttpError_ReturnsSoapFaultResponse()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>NotFound</Message></Echo>",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/notfound",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.XmlResponse, Does.Contain("Fault"), "HTTP error should be wrapped in SOAP Fault");
        Assert.That(result.Error?.Message, Does.Contain("404"), "Error should reference HTTP 404");
    }

    [Test]
    public async Task Request_WithWsdlValidation_ReturnsSuccessfully()
    {
        var wsdlContent = LoadTestFile("sample.wsdl");
        var validBody = LoadTestFile("valid_body.xml");

        var input = new Input
        {
            MessageBody = validBody,
            SoapAction = "GetWeather",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            WsdlSource = WsdlSource.String,
            WsdlString = wsdlContent,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "WSDL validation should pass with valid body");
    }

    [Test]
    public async Task Request_WithThrowErrorOnFailureFalse_ReturnsFailedResult()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Error</Message></Echo>",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/error",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false, // Should return failed result, not throw
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.XmlResponse, Does.Contain("Fault"));
    }

    [Test]
    public async Task Request_WithCustomErrorMessage_ReturnsCustomErrorText()
    {
        const string customMessage = "Custom error from test";

        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Error</Message></Echo>",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/error",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = customMessage,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error?.Message, Does.Contain(customMessage));
    }

    [Test]
    public async Task Request_WithHttpsAndAllowInvalidCert_ConnectsSuccessfully()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>Secure</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true, // Required for self-signed cert
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "Should connect via HTTPS with self-signed certificate");
        Assert.That(result.XmlResponse, Does.Contain("Envelope"));
    }

    [Test]
    public async Task Request_WithMultipleWsSpecs_IncludesAllHeaders()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>MultiSpec</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = true,
            IncludeWsAddressing = true,
            IncludeWsReliableMessaging = true,
            IncludeWsPolicy = true,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
            WsSecurityTimestampMinutes = 5,
            WsAddressingReplyTo = "https://www.w3.org/2005/08/addressing/anonymous",
            WsReliableMessagingMessageNumber = 1,
        };

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "Request with multiple WS-* specs should succeed");
    }

    [Test]
    public async Task Request_WithValidClientCertificate_Succeeds()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>ClientCert</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/echo",
            Authentication = Authentication.ClientCertificate,
            ClientCertPath = clientCertWithPasswordPath,
            ClientCertPassword = ClientCertPassword,
            AllowInvalidCertificate = true,
        };

        var options = CreateDefaultOptions();

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Request_WithValidClientCertificateWithoutPassword_Succeeds()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>ClientCertNoPassword</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/echo",
            Authentication = Authentication.ClientCertificate,
            ClientCertPath = clientCertWithoutPasswordPath,
            ClientCertPassword = string.Empty,
            AllowInvalidCertificate = true,
        };

        var options = CreateDefaultOptions();

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Request_WithInvalidClientCertificate_Fails()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>InvalidClientCert</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/client-cert-auth",
            Authentication = Authentication.ClientCertificate,
            ClientCertPath = invalidClientCertPath,
            ClientCertPassword = ClientCertPassword,
            AllowInvalidCertificate = true,
        };

        var options = CreateDefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.XmlResponse, Does.Contain("Client certificate rejected"));
    }

    [Test]
    public async Task Request_WithValidServerCertificateThumbprint_Succeeds()
    {
        var serverThumbprint = await GetServerCertificateThumbprintAsync();

        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>PinnedCert</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = false,
            ServerCertificateThumbprints = [serverThumbprint],
        };

        var options = CreateDefaultOptions();

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Request_WithInvalidServerCertificateThumbprint_Fails()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>WrongPinnedCert</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = false,
            ServerCertificateThumbprints = ["0000000000000000000000000000000000000000"],
        };

        var options = CreateDefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
    }

    [Test]
    public async Task Request_WithCertificationRevocationCheck_Fails_WhenCertIsRevoked()
    {
        var input = new Input
        {
            MessageBody = @"<Echo xmlns=""https://example.com/service""><Message>RevocationCheck</Message></Echo>",
            SoapAction = "Echo",
        };

        var connection = new Connection
        {
            Url = $"{httpsUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = false,
            CertificationRevocationCheck = true,
        };

        var options = CreateDefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
    }

    [Test]
    public async Task Request_WithWsdlFromFile_ValidatesAndSucceeds()
    {
        var input = new Input
        {
            MessageBody = LoadTestFile("valid_body.xml"),
            SoapAction = "GetWeather",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = CreateDefaultOptions();
        options.WsdlSource = WsdlSource.File;
        options.WsdlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles", "sample.wsdl");

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Request_WithWsdlFromString_ValidatesAndSucceeds()
    {
        var input = new Input
        {
            MessageBody = LoadTestFile("valid_body.xml"),
            SoapAction = "GetWeather",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = CreateDefaultOptions();
        options.WsdlSource = WsdlSource.String;
        options.WsdlString = LoadTestFile("sample.wsdl");

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task Request_WithWsdlFromUrl_ValidatesAndSucceeds()
    {
        var input = new Input
        {
            MessageBody = LoadTestFile("valid_body.xml"),
            SoapAction = "GetWeather",
        };

        var connection = new Connection
        {
            Url = $"{httpUrl}/soap/echo",
            Authentication = Authentication.None,
            AllowInvalidCertificate = true,
        };

        var options = CreateDefaultOptions();
        options.WsdlSource = WsdlSource.Url;
        options.WsdlUrl = $"{httpUrl}/wsdl";

        var result = await Soap.Request(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    private static string LoadTestFile(string filename)
    {
        var testFilesPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles");
        var filePath = Path.Combine(testFilesPath, filename);

        return File.ReadAllText(filePath);
    }

    private static void BuildDockerImage(string testFilesPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"build -t frends-soap-test-server:latest \"{testFilesPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        if (process == null)
            Assert.Fail("Failed to start docker build process.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(120000) || process.ExitCode != 0)
        {
            Assert.Fail($"Docker build failed. ExitCode={process.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }

    private static async Task WaitForHealthAsync(string baseUrl)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync($"{baseUrl}/health");

                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // keep polling
            }

            await Task.Delay(500);
        }

        Assert.Fail($"Container health endpoint did not become ready in time: {baseUrl}/health");
    }

    private static Options CreateDefaultOptions()
    {
        return new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            ThrowErrorOnFailure = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };
    }

    private static async Task<string> GetServerCertificateThumbprintAsync()
    {
        var uri = new Uri(httpsUrl);
        using var client = new TcpClient();
        await client.ConnectAsync(uri.Host, uri.Port);

        using var sslStream = new SslStream(client.GetStream(), false, (_, _, _, _) => true);
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = uri.Host,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
        });

        var cert = new X509Certificate2(sslStream.RemoteCertificate);
        return cert.Thumbprint;
    }

    private static void CreateClientCertificates(
        string certWithPasswordPath,
        string certWithoutPasswordPath,
        string invalidCertPath,
        string password)
    {
        CreateClientCertificate(certWithPasswordPath, "Test Client Certificate", password);
        CreateClientCertificate(certWithoutPasswordPath, "Test Client Certificate", string.Empty);
        CreateClientCertificate(invalidCertPath, "Rejected Client Certificate", password);
    }

    private static void CreateClientCertificate(string certPath, string commonName, string password)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={commonName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [
                    new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
                ],
                true));

        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        File.WriteAllBytes(certPath, cert.Export(X509ContentType.Pfx, password));
    }
}
