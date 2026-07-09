using AwesomeAssertions;
using Tw.Validation.Abstractions;
using Xunit;

namespace Tw.Validation.Abstractions.Tests;

public sealed class ValidationExceptionTests
{
    [Fact]
    public void Constructor_StoresValidationErrors()
    {
        var errors = new[] { new ValidationError("name", "VALIDATION:000001", "名称不能为空") };

        var exception = new ValidationException(errors);

        exception.Errors.Should().ContainSingle().Which.FieldPath.Should().Be("name");
    }
}
