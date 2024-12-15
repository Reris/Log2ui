using System.Threading.Tasks;
using FluentAssertions;
using Log2ui.Extensions;
using Xunit;

namespace Log2Ui.Tests.Extensions;

public class TaskExtensionsTests
{
    [Fact]
    public async Task SelectAsync_Task_ReturnsValue()
    {
        // Arrange
        const string expected = "Foo";
        var testee = Task.FromResult(new { Value = expected });

        // Act
        var result = await testee.SelectAsync(a => a.Value);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SelectAsync_ValueTask_ReturnsValue()
    {
        // Arrange
        const string expected = "Foo";
        var testee = ValueTask.FromResult(new { Value = expected });

        // Act
        var result = await testee.SelectAsync(a => a.Value);

        // Assert
        result.Should().BeSameAs(expected);
    }
}
