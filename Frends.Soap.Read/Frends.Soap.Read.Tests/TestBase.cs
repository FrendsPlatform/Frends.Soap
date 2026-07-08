using System;
using Frends.Soap.Read.Definitions;

namespace Frends.Soap.Read.Tests;

internal abstract class TestBase
{
    protected static Input DefaultInput() => new();

    protected static Options DefaultOptions() => new();
}
