using System.Collections.Generic;
using Log2ui;
using Log2ui.Collections;
using Log2ui.Collections.Observables;
using Log2ui.Data;
using Log2ui.Exporters;
using Log2ui.Receivers;
using Log2ui.Settings;
using Log2ui.Tools;
using Log2ui.Views;
using NSubstitute;
using Xunit;

namespace Log2Ui.Tests.Views;

public class LoggerViewModel_Tests
{
    private readonly Signal<AllReceiverSettings> _allReceiverSettings = new(new AllReceiverSettings());
    private readonly IList<ExportSettings> _exports = [];
    private readonly ICollectionView<LogMessageItem, IList<LogMessageItem>> _logCollectionView = Substitute.For<ICollectionView<LogMessageItem, IList<LogMessageItem>>>();
    private readonly ILoggerSettingsViewModel _loggerSettingsViewModel = Substitute.For<ILoggerSettingsViewModel>();
    private readonly ILogSearchViewModel _logSearchViewModel = Substitute.For<ILogSearchViewModel>();
    private readonly IMainDispatcher _mainDispatcher = Substitute.For<IMainDispatcher>();
    private readonly string _name = "Testee";
    private readonly Signal<NamedLoggerSettings> _namedLoggerSettings = new(new NamedLoggerSettings());
    private readonly IReceiverFactory _receiverFactory = Substitute.For<IReceiverFactory>();
    private readonly IReceiverManagerViewModel _receiverManagerViewModel = Substitute.For<IReceiverManagerViewModel>();
    private readonly IViewModelFactory _viewModelFactory = Substitute.For<IViewModelFactory>();

    public LoggerViewModel_Tests()
    {
        this._loggerSettingsViewModel.LoggerSettings.Returns(this._namedLoggerSettings);
        this._loggerSettingsViewModel.AllReceiverSettings.Returns(this._allReceiverSettings);
    }

    private LoggerViewModel CreateTestee()
    {
        return new LoggerViewModel(
            this._name,
            this._logCollectionView,
            this._mainDispatcher,
            this._logSearchViewModel,
            this._loggerSettingsViewModel,
            this._receiverManagerViewModel,
            this._exports,
            this._viewModelFactory,
            this._receiverFactory);
    }

    [Fact]
    public void ReceiversChanged_Added_ShouldAttach()
    {
        // Arrange
        var testee = this.CreateTestee();
        var receiver = new ObservableReceiver.Settings();

        // Act
        this._allReceiverSettings.OnNext(new AllReceiverSettings { Receivers = [receiver] });
        this._namedLoggerSettings.OnNext(new NamedLoggerSettings { ReceiverKeys = [receiver.Key] });
        this._namedLoggerSettings.OnNext(new NamedLoggerSettings { ReceiverKeys = [receiver.Key] }); // ignore multiple calls

        // Assert
        this._receiverFactory.Received(1).Attach(receiver, testee);
    }

    [Fact]
    public void ReceiversChanged_Removed_ShouldDetach()
    {
        // Arrange
        var testee = this.CreateTestee();
        var receiver = new ObservableReceiver.Settings();
        this._allReceiverSettings.OnNext(new AllReceiverSettings { Receivers = [receiver] });
        this._namedLoggerSettings.OnNext(new NamedLoggerSettings { ReceiverKeys = [receiver.Key] });

        // Act
        this._namedLoggerSettings.OnNext(new NamedLoggerSettings { ReceiverKeys = [] });

        // Assert
        this._receiverFactory.Received(1).Detach(receiver, testee);
    }

    [Fact]
    public void ReceiversChanged_Lost_ShouldDetach()
    {
        // Arrange
        var testee = this.CreateTestee();
        var receiver = new ObservableReceiver.Settings();
        this._allReceiverSettings.OnNext(new AllReceiverSettings { Receivers = [receiver] });
        this._namedLoggerSettings.OnNext(new NamedLoggerSettings { ReceiverKeys = [receiver.Key] });

        // Act
        this._allReceiverSettings.OnNext(new AllReceiverSettings { Receivers = [] });

        // Assert
        this._receiverFactory.Received(1).Detach(receiver, testee);
    }
}
