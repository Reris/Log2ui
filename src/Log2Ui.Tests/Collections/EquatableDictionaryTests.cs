using System.Collections.Generic;
using System.Collections.Immutable;
using AwesomeAssertions;
using Log2ui.Collections;
using Xunit;

namespace Log2Ui.Tests.Collections;

public class EquatableDictionaryTests
{
    [Fact]
    public void FromImmutableDictionary__ShouldBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));
        var dictionary = ImmutableDictionary.CreateRange(
        [
            new KeyValuePair<string, int>("foo", 42),
            new KeyValuePair<string, int>("bar", 35),
            new KeyValuePair<string, int>("baz", 1337),
        ]);

        // Act
        object result = EquatableDictionary<string, int>.FromImmutableDictionary(dictionary);

        // Assert
        result.Should().Be(testee);
    }

    [Fact]
    public void AsImmutableDictionary__ShouldNotBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));
        var dictionary = ImmutableDictionary.CreateRange(
        [
            new KeyValuePair<string, int>("foo", 42),
            new KeyValuePair<string, int>("bar", 35),
            new KeyValuePair<string, int>("baz", 1337),
        ]);

        // Act
        object result = testee.AsImmutableDictionary();

        // Assert
        result.Should().NotBe(dictionary);
    }

    [Fact]
    public void AsImmutableDictionary_DoubleCast_ShouldBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));

        // Act
        object result1 = testee.AsImmutableDictionary();
        object result2 = testee.AsImmutableDictionary();

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Cast_AsImmutableDictionary_ShouldNotBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));

        // Act
        ImmutableDictionary<string, int> result = testee;

        // Assert
        ((object)result).Should().NotBe(testee);
    }

    [Fact]
    public void DoubleCast_AsImmutableDictionary_ShouldBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));

        // Act
        ImmutableDictionary<string, int> result1 = testee;
        ImmutableDictionary<string, int> result2 = testee;

        // Assert
        ((object)result1).Should().Be(result2);
    }

    [Fact]
    public void Cast_AsEquatableDictionary_ShouldBeEqual()
    {
        // Arrange
        var testee = ImmutableDictionary.CreateRange(
        [
            new KeyValuePair<string, int>("foo", 42),
            new KeyValuePair<string, int>("bar", 35),
            new KeyValuePair<string, int>("baz", 1337),
        ]);

        // Act
        EquatableDictionary<string, int> result = testee;

        // Assert
        ((object)result).Should().Be(testee);
    }

    [Fact]
    public void Append__ShouldBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create<string, int>();
        var expected = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));

        // Act
        object result = testee.Append("foo", 42)
                              .Append("bar", 35)
                              .Append("baz", 1337);

        // Assert
        result.Should().Be(expected);
        testee.Should().BeEmpty();
    }

    [Fact]
    public void Append_Range_ShouldBeEqual()
    {
        // Arrange
        var testee = EquatableDictionary.Create<string, int>();
        var expected = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));

        // Act
        object result = testee.Append(expected);

        // Assert
        result.Should().Be(expected);
        testee.Should().BeEmpty();
    }

    [Fact]
    public void Equals_NonEqual_ShouldBeFalse()
    {
        // Arrange
        var testee = EquatableDictionary.Create(("foo", 42), ("bar", 35), ("baz", 1337));
        var expected = EquatableDictionary.Create(("bar", 35), ("baz", 1337));

        // Act
        var result = testee.Equals(expected);

        // Assert
        result.Should().BeFalse();
    }
}
