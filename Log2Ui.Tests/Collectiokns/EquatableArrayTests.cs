using System.Collections.Immutable;
using AwesomeAssertions;
using Log2ui.Collections;
using Xunit;

namespace Log2Ui.Tests.Collectiokns;

public class EquatableArrayTests
{
    [Fact]
    public void FromImmutableArray__ShouldBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");
        var array = ImmutableArray.Create(["foo", "bar", "baz"]);

        // Act
        object result = EquatableArray<string>.FromImmutableArray(array);

        // Assert
        result.Should().Be(testee);
    }

    [Fact]
    public void AsImmutableArray__ShouldNotBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");
        var array = ImmutableArray.Create(["foo", "bar", "baz"]);

        // Act
        object result = testee.AsImmutableArray();

        // Assert
        result.Should().NotBe(array);
    }

    [Fact]
    public void AsImmutableArray_DoubleCast_ShouldBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");

        // Act
        object result1 = testee.AsImmutableArray();
        object result2 = testee.AsImmutableArray();

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Cast_AsImmutableArray_ShouldNotBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");

        // Act
        ImmutableArray<string> result = testee;

        // Assert
        ((object)result).Should().NotBe(testee);
    }

    [Fact]
    public void DoubleCast_AsImmutableArray_ShouldBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");

        // Act
        ImmutableArray<string> result1 = testee;
        ImmutableArray<string> result2 = testee;

        // Assert
        ((object)result1).Should().Be(result2);
    }

    [Fact]
    public void Cast_AsEquatableArray_ShouldBeEqual()
    {
        // Arrange
        var testee = ImmutableArray.Create(["foo", "bar", "baz"]);

        // Act
        EquatableArray<string> result = testee;

        // Assert
        ((object)result).Should().Be(testee);
    }

    [Fact]
    public void Append__ShouldBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>();
        var expected = new EquatableArray<string>("foo", "bar", "baz");

        // Act
        object result = testee.Append("foo")
                              .Append("bar")
                              .Append("baz");

        // Assert
        result.Should().Be(expected);
        testee.Should().BeEmpty();
    }

    [Fact]
    public void Range__ShouldBeEqual()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");
        var expected = new EquatableArray<string>("bar", "baz");

        // Act
        object result = testee[1..];

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Equals_NonEqual_ShouldBeFalse()
    {
        // Arrange
        var testee = new EquatableArray<string>("foo", "bar", "baz");
        var expected = new EquatableArray<string>("bar", "baz");

        // Act
        var result = testee.Equals(expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ArrayInit__ShouldBeEqual()
    {
        // Arrange
        var expected = new EquatableArray<string>("foo", "bar", "baz");

        // Act
        EquatableArray<string> testee = ["foo", "bar", "baz"];

        // Assert
        ((object)testee).Should().Be(expected);
    }
}
