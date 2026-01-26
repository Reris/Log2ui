using AwesomeAssertions;
using Log2ui.Extensions;
using Xunit;

namespace Log2Ui.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Foo.txt", "foo.txt", true)]
    [InlineData("Foo.txt", "*.txt", true)]
    [InlineData("Foo.txt", "foo.*", true)]
    [InlineData("Foo.txt", "*", true)]
    [InlineData("Foo.txt", "fo*.txt", true)]
    [InlineData("Foo.txt", "f*.t*t", true)]
    [InlineData("Foo.txt", "fo?.txt", true)]
    [InlineData("Foo.txt", "f??.t?t", true)]
    [InlineData("Far.txt", "foo.txt", false)]
    [InlineData("Far.txt", "foo.*", false)]
    [InlineData("Foo.txt", "f?.txt", false)]
    [InlineData("Foo.txt", "f??.t?", false)]
    public void IsGlob__ReturnsExpected(string value, string glob, bool expected)
    {
        // Arrange

        // Act
        var result = value.IsGlob(glob);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Foo.txt", "Foo.txt", true)]
    [InlineData("Foo.txt", "*.txt", true)]
    [InlineData("Foo.txt", "Foo.*", true)]
    [InlineData("Foo.txt", "*", true)]
    [InlineData("Foo.txt", "Fo*.txt", true)]
    [InlineData("Foo.txt", "F*.t*t", true)]
    [InlineData("Foo.txt", "Fo?.txt", true)]
    [InlineData("Foo.txt", "??o.t?t", true)]
    [InlineData("Bar.txt", "Foo.txt", false)]
    [InlineData("Bar.txt", "Foo.*", false)]
    [InlineData("Foo.txt", "foo.*", false)]
    [InlineData("Foo.txt", "F?.txt", false)]
    [InlineData("Foo.txt", "F??.t?", false)]
    [InlineData("foo.txt", "Foo.txt", false)]
    [InlineData("foo.txt", "Foo.*", false)]
    public void IsGlob_CaseSensitive_ReturnsExpected(string value, string glob, bool expected)
    {
        // Arrange

        // Act
        var result = value.IsGlob(glob, true);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "*.txt", false)]
    [InlineData("foo.txt", null, false)]
    public void IsGlob_AnyNull_ReturnsExpected(string value, string glob, bool expected)
    {
        // Arrange

        // Act
        var result = value.IsGlob(glob);

        // Assert
        result.Should().Be(expected);
    }
}
