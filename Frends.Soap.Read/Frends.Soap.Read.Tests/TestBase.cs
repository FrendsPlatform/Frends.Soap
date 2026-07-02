using System;
using dotenv.net;
using Frends.Soap.Read.Definitions;

namespace Frends.Soap.Read.Tests;

internal abstract class TestBase
{
    internal TestBase()
    {
        DotEnv.Load();
    }

    protected static Input DefaultInput() => new();

    protected static Options DefaultOptions() => new();
}
