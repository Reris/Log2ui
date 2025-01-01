using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using Log2ui.Settings;
using Log2ui.Settings.Services;
using NSubstitute;
using Xunit;

namespace Log2Ui.Tests.Settings;

public class SettingsServiceTests
{
    private readonly ISettingsServiceStorage _storage = Substitute.For<ISettingsServiceStorage>();

    private SettingsService CreateTestee()
    {
        return new SettingsService(this._storage);
    }

    [Fact]
    public async Task SaveAsync_AppSettings_ShouldNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = new Builder().CreateNew<AppSettings>().Build();
        var observer = Substitute.For<IObserver<AppSettings>>();
        using var _ = testee.AppSettings.Subscribe(observer);

        // Act
        await testee.SaveAsync(expected);

        // Assert
        observer.Received().OnNext(expected);
    }

    [Fact]
    public async Task SaveAsync_NamedLoggerSettings_ShouldNext()
    {
        // Arrange
        var testee = this.CreateTestee();
        const string name = "foo";
        var expected = new Builder().CreateNew<NamedLoggerSettings>().Build() with { Name = name, OriginalName = name };
        var observer = Substitute.For<IObserver<LoggerSettings>>();
        using var _ = testee.LoggerSettings(name).Subscribe(observer);
        observer.ClearReceivedCalls();

        // Act
        await testee.SaveAsync(expected);

        // Assert
        observer.Received().OnNext(expected);
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
