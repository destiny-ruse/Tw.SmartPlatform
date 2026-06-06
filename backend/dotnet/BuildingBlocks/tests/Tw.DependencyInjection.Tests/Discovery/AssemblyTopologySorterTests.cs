using FluentAssertions;
using Tw.DependencyInjection;
using Tw.Exceptions;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyTopologySorterTests
{
    [Fact]
    public void ServiceRegistrationException_DerivesFromTwException()
    {
        var exception = new ServiceRegistrationException("boom");

        exception.Should().BeAssignableTo<TwException>();
        exception.Message.Should().Be("boom");
    }
}
