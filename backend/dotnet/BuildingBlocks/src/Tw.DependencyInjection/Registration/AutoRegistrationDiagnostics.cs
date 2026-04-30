using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Tw.DependencyInjection.Registration;

internal sealed class AutoRegistrationDiagnostics
{
    public static readonly AutoRegistrationDiagnostics Disabled = new(false, _ => { });

    private readonly Action<string> _writer;

    private AutoRegistrationDiagnostics(bool enabled, Action<string> writer)
    {
        Enabled = enabled;
        _writer = writer;
    }

    public bool Enabled { get; }

    public static AutoRegistrationDiagnostics Create(
        IConfiguration configuration,
        AutoRegistrationOptions options)
    {
        var enabled = options.DiagnosticsEnabled ?? IsDevelopment(configuration);
        var writer = options.DiagnosticsWriter ?? (message => Trace.WriteLine(message));
        return new AutoRegistrationDiagnostics(enabled, writer);
    }

    public void Stage(string name, TimeSpan elapsed, string details)
    {
        if (!Enabled)
        {
            return;
        }

        _writer($"auto-registration {name}: elapsed={elapsed.TotalMilliseconds:0.###}ms; {details}");
    }

    public void Warning(string warning)
    {
        if (!Enabled)
        {
            return;
        }

        _writer($"auto-registration warning: {warning}");
    }

    private static bool IsDevelopment(IConfiguration configuration)
    {
        var environment = configuration["DOTNET_ENVIRONMENT"]
            ?? configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["environment"];

        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
