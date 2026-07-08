using System;
using System.Text;
using System.Threading;
using Frends.Soap.Read.Definitions;
using Frends.Soap.Read.Definitions.Enums;
using NUnit.Framework;

namespace Frends.Soap.Read.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private const string Soap11Payload =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                       xmlns:wsa="http://www.w3.org/2005/08/addressing">
          <soap:Header>
            <wsa:Action>https://tempuri.org/IService/GetPrice</wsa:Action>
            <wsa:MessageID>urn:uuid:1234</wsa:MessageID>
          </soap:Header>
          <soap:Body>
            <GetPriceResponse xmlns="https://tempuri.org/">
              <Price>1.99</Price>
            </GetPriceResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private const string Soap11Fault =
        """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <soap:Fault>
              <faultcode>soap:Server</faultcode>
              <faultstring>Something went wrong.</faultstring>
              <faultactor>https://example.com/service</faultactor>
              <detail><err:info xmlns:err="urn:err">bad</err:info></detail>
            </soap:Fault>
          </soap:Body>
        </soap:Envelope>
        """;

    private const string Soap12Fault =
        """
        <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope">
          <soap:Body>
            <soap:Fault>
              <soap:Code><soap:Value>soap:Sender</soap:Value></soap:Code>
              <soap:Reason><soap:Text xml:lang="en">Invalid request.</soap:Text></soap:Reason>
              <soap:Node>https://example.com/node</soap:Node>
              <soap:Detail><err:info xmlns:err="urn:err">bad</err:info></soap:Detail>
            </soap:Fault>
          </soap:Body>
        </soap:Envelope>
        """;

    [Test]
    public void Should_Read_Soap11_Body_And_Headers()
    {
        var result = Soap.Read(InputWith(Soap11Payload), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.SoapVersion, Is.EqualTo(SoapVersion.Soap11));
        Assert.That(result.Fault, Is.Null);
        Assert.That(result.Body, Does.Contain("<Price>1.99</Price>"));

        Assert.That(result.Headers, Has.Count.EqualTo(2));
        Assert.That(result.Headers[0].Name, Is.EqualTo("Action"));
        Assert.That(result.Headers[0].Namespace, Is.EqualTo("http://www.w3.org/2005/08/addressing"));
        Assert.That(result.Headers[0].Value, Is.EqualTo("https://tempuri.org/IService/GetPrice"));
        Assert.That(result.Headers[1].Name, Is.EqualTo("MessageID"));
    }

    [Test]
    public void Should_Return_Empty_Headers_When_No_Header_Element()
    {
        const string payload = """
                               <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                                 <soap:Body><Ping xmlns="urn:test"/></soap:Body>
                               </soap:Envelope>
                               """;

        var result = Soap.Read(InputWith(payload), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Headers, Is.Not.Null);
        Assert.That(result.Headers, Is.Empty);
        Assert.That(result.Body, Does.Contain("Ping"));
    }

    [Test]
    public void Should_Read_Soap11_Fault()
    {
        var result = Soap.Read(InputWith(Soap11Fault), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SoapVersion, Is.EqualTo(SoapVersion.Soap11));
        Assert.That(result.Fault, Is.Not.Null);
        Assert.That(result.Fault.Code, Is.EqualTo("soap:Server"));
        Assert.That(result.Fault.Reason, Is.EqualTo("Something went wrong."));
        Assert.That(result.Fault.Actor, Is.EqualTo("https://example.com/service"));
        Assert.That(result.Fault.Detail, Does.Contain("bad"));
        Assert.That(result.Fault.Xml, Does.Contain("faultcode"));
    }

    [Test]
    public void Should_Read_Soap12_Fault()
    {
        var result = Soap.Read(InputWith(Soap12Fault), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SoapVersion, Is.EqualTo(SoapVersion.Soap12));
        Assert.That(result.Fault, Is.Not.Null);
        Assert.That(result.Fault.Code, Is.EqualTo("soap:Sender"));
        Assert.That(result.Fault.Reason, Is.EqualTo("Invalid request."));
        Assert.That(result.Fault.Actor, Is.EqualTo("https://example.com/node"));
        Assert.That(result.Fault.Detail, Does.Contain("bad"));
    }

    [Test]
    public void Should_Decode_Base64_Payload()
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Soap11Payload));
        var options = DefaultOptions();
        options.PayloadEncoding = PayloadEncoding.Base64;
        options.CharacterEncoding = "utf-8";

        var result = Soap.Read(InputWith(base64), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SoapVersion, Is.EqualTo(SoapVersion.Soap11));
        Assert.That(result.Body, Does.Contain("<Price>1.99</Price>"));
    }

    [Test]
    public void Should_Throw_On_Invalid_Xml()
    {
        var ex = Assert.Throws<Exception>((Action)(() =>
            Soap.Read(InputWith("Lorem ipsum dolor sit amet."), DefaultOptions(), CancellationToken.None)));
        Assert.That(ex!.Message, Does.Contain("not valid XML").IgnoreCase);
    }

    [Test]
    public void Should_Return_Failed_Result_On_Non_Soap_Xml()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var result = Soap.Read(InputWith("<root><child/></root>"), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("SOAP"));
    }

    [Test]
    public void Should_Return_Failed_Result_On_Invalid_Base64()
    {
        var options = DefaultOptions();
        options.PayloadEncoding = PayloadEncoding.Base64;
        options.ThrowErrorOnFailure = false;

        var result = Soap.Read(InputWith("!!!not-base64!!!"), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Base64"));
    }

    private static Input InputWith(string payload) => new() { Payload = payload };
}
