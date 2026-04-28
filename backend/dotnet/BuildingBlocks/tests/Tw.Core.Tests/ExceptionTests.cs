using FluentAssertions;
using Tw.Core.Exceptions;
using Xunit;

namespace Tw.Core.Tests;

public class ExceptionTests
{
    [Fact]
    public void TwException_Is_Concrete_Exception_Base()
    {
        var exception = new TwException("failure");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("failure");
    }

    [Fact]
    public void TwConfigurationException_Derives_From_TwException()
    {
        var exception = new TwConfigurationException("bad config");

        exception.Should().BeAssignableTo<TwException>();
    }
}
