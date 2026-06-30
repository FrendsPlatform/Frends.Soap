using System;
using dotenv.net;
using Frends.Soap.Read.Definitions;

namespace Frends.Soap.Read.Tests;

internal abstract class TestBase
{
    internal TestBase()
    {
        DotEnv.Load();
        SecretKey = GetEnvVar("FRENDS_SECRET_KEY");
    }

    protected string SecretKey { get; set; }

    protected static Input DefaultInput() => new();

    protected static Options DefaultOptions() => new();

    private static string GetEnvVar(string name) => Environment.GetEnvironmentVariable(name) ??
                                                    throw new InvalidOperationException(
                                                        $"Missing required env var: {name}");
}
