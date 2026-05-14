using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        Action action = () =>
            Soap.Request(DefaultInput(), DefaultConnection(), DefaultOptions(), CancellationToken.None);
        var ex = Assert.Throws<Exception>(action);
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var result = await Soap.Request(DefaultInput(), DefaultConnection(), options, CancellationToken.None);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        var ex = Assert.ThrowsAsync<Exception>(await (Action)Action);
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Contains.Substring(CustomErrorMessage));

        return;
        Task Action() => Soap.Request(DefaultInput(), DefaultConnection(), options, CancellationToken.None);
    }
}
