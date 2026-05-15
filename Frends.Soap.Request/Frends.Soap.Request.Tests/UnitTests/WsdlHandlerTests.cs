using System.IO;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.UnitTests;

[TestFixture]
public class WsdlHandlerTests
{
    [Test]
    public void ValidateBodyAgainstWsdl_WithValidBody_ReturnsTrue()
    {
        // Arrange
        var wsdlContent = GetTestFile("sample.wsdl");
        var validBody = GetTestFile("valid_body.xml");

        // Act
        var (isValid, error) = WsdlHandler.ValidateBodyAgainstWsdl(validBody, wsdlContent);

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(error, Is.Null.Or.Empty);
    }

    [Test]
    public void ValidateBodyAgainstWsdl_WithNullWsdl_ReturnsTrue()
    {
        // Act
        var (isValid, error) = WsdlHandler.ValidateBodyAgainstWsdl("<test/>", null);

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(error, Is.Null.Or.Empty);
    }

    [Test]
    public void ValidateBodyAgainstWsdl_WithEmptyWsdl_ReturnsTrue()
    {
        // Act
        var (isValid, error) = WsdlHandler.ValidateBodyAgainstWsdl("<test/>", string.Empty);

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(error, Is.Null.Or.Empty);
    }

    [Test]
    public void GetTargetNamespace_WithValidWsdl_ReturnsTargetNamespace()
    {
        // Arrange
        var wsdlContent = GetTestFile("sample.wsdl");

        // Act
        var targetNamespace = WsdlHandler.GetTargetNamespace(wsdlContent);

        // Assert
        Assert.That(targetNamespace, Is.EqualTo("https://example.com/weatherservice"));
    }

    [Test]
    public void GetTargetNamespace_WithNullWsdl_ReturnsNull()
    {
        // Act
        var targetNamespace = WsdlHandler.GetTargetNamespace(null);

        // Assert
        Assert.That(targetNamespace, Is.Null);
    }

    [Test]
    public void GetTargetNamespace_WithEmptyWsdl_ReturnsNull()
    {
        // Act
        var targetNamespace = WsdlHandler.GetTargetNamespace(string.Empty);

        // Assert
        Assert.That(targetNamespace, Is.Null);
    }

    private static string GetTestFile(string filename)
    {
        var testFilesPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles");
        var filePath = Path.Combine(testFilesPath, filename);

        return File.ReadAllText(filePath);
    }
}
