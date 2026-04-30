using Microsoft.Extensions.Configuration;

namespace Tw.DependencyInjection.Registration;

internal sealed record AutoRegistrationState(
    IConfiguration Configuration,
    AutoRegistrationOptions Options);
