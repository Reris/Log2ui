using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using FizzWare.NBuilder;
using Log2ui.Settings;
using Log2ui.Settings.Services;
using NSubstitute;
using Xunit;

namespace Log2Ui.Tests.Settings;

public class SettingsServiceTests
{
    private readonly ISettingsServiceStorage _storage = Substitute.For<ISettingsServiceStorage>();
    private readonly IValidator _validator = Substitute.For<IValidator>();

    private SettingsService CreateTestee()
    {
        return new SettingsService(this._storage, this._validator);
    }

    [Fact]
    public async Task SaveAsync_AppSettings_ShouldNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = new Builder().CreateNew<AppSettings>().Build();
        this._validator.IsValid(expected).Returns(true);
        var observer = Substitute.For<IObserver<AppSettings>>();
        using var _ = testee.AppSettings.Subscribe(observer);

        // Act
        var result = await testee.SaveAsync(expected);

        // Assert
        result.Should().BeTrue();
        observer.Received().OnNext(expected);
    }

    [Fact]
    public async Task SaveAsync_NamedLoggerSettings_ShouldNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        const string name = "foo";
        var expected = new Builder().CreateNew<NamedLoggerSettings>().Build() with { Name = name, OriginalName = name };
        this._validator.IsValid(expected).Returns(true);
        var observer = Substitute.For<IObserver<LoggerSettings>>();
        using var _ = testee.LoggerSettings(name).Subscribe(observer);
        observer.ClearReceivedCalls();

        // Act
        var result = await testee.SaveAsync(expected);

        // Assert
        result.Should().BeTrue();
        var next = (NamedLoggerSettings)observer.ReceivedCalls()
                                                .SingleOrDefault(a => a.GetMethodInfo().Name == nameof(IObserver<int>.OnNext))
                                                !.GetArguments()[0]!;
        next.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SaveAsync_IsValidFalse_ShouldReturnFalseAndNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        const string name = "foo";
        var expected = new Builder().CreateNew<NamedLoggerSettings>().Build() with { Name = name, OriginalName = name };
        this._validator.IsValid(expected).Returns(false);
        var observer = Substitute.For<IObserver<LoggerSettings>>();
        using var _ = testee.LoggerSettings(name).Subscribe(observer);
        observer.ClearReceivedCalls();

        // Act
        var result = await testee.SaveAsync(expected);

        // Assert
        result.Should().BeFalse();
        observer.DidNotReceiveWithAnyArgs().OnNext(expected);
    }

    [Fact]
    public async Task LoadAsync_AppSettings_ShouldNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = new Builder().CreateNew<AppSettings>().Build();
        var observer = Substitute.For<IObserver<AppSettings>>();
        this._storage.LoadAppSettingsAsync().Returns(new Versioned<AppSettings>(1, expected));
        this._storage.LoadLoggerSettingsAsync().Returns([]);
        using var _ = testee.AppSettings.Subscribe(observer);

        // Act
        await testee.LoadAsync();

        // Assert
        observer.Received().OnNext(expected);
    }

    [Fact]
    public async Task LoadAsync_NamedLoggerSettings_ShouldNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        const string name = "foo";
        var expected = new Builder().CreateNew<NamedLoggerSettings>().Build() with { Name = name, OriginalName = name };
        var observer = Substitute.For<IObserver<NamedLoggerSettings>>();
        this._storage.LoadLoggerSettingsAsync()
            .Returns(new Dictionary<string, Versioned<NamedLoggerSettings>> { { name, new Versioned<NamedLoggerSettings>(1, expected) } });
        using var _ = testee.LoggerSettings(name).Subscribe(observer);

        // Act
        await testee.LoadAsync();

        // Assert
        observer.Received().OnNext(expected);
    }
}
