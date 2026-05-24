using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using Log2ui.Data;
using Log2ui.Receivers;
using NSubstitute;
using Testably.Abstractions.Testing;
using Xunit;
using TimeProvider = System.TimeProvider;

namespace Log2Ui.Tests.Receivers;

public class CsvFileReceiver_Tests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly CsvFileReceiver.Settings _settings = new() { FileToWatch = "data/test.csv", ShowFromBeginning = true };
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    private CsvFileReceiver CreateTestee()
    {
        return new CsvFileReceiver(this._settings, this._fileSystem, this._timeProvider);
    }

    private static void Initialize(IReceiver testee)
    {
        testee.Initialize();
    }

    [Fact]
    public async Task Attach__ShouldNotyfiyWithContent()
    {
        // Arrange
        var testee = this.CreateTestee();
        var notifiable = Substitute.For<ILogMessageNotifiable>();
        var received = new List<LogMessage>();
        notifiable.WhenForAnyArgs(a => a.Notify(Arg.Any<IReadOnlyList<LogMessage>>())).Do(a => received.AddRange(a.Arg<IReadOnlyList<LogMessage>>()));
        var expected = new List<LogMessage>
        {
            new()
            {
                Level = LogLevel.Info,
                TimeStamp = DateTime.Parse("2024-06-01T12:00:00Z"),
                LoggerName = "Foo",
                SequenceNr = 42,
                Message = "Hello",
                RootLoggerName = "RootLogger",
                CallSiteClass = "MyClass",
                CallSiteMethod = "MyMethod",
                SourceFileName = "MyFile.cs",
                SourceFileLineNr = 123,
                ThreadName = "MainThread",
                ExceptionString = "",
            },
            new()
            {
                Level = LogLevel.Error,
                TimeStamp = DateTime.Parse("2024-06-01T12:01:00Z"),
                LoggerName = "Bar",
                SequenceNr = 43,
                Message = "World",
                RootLoggerName = "RootLogger",
                CallSiteClass = "MyClass",
                CallSiteMethod = "MyMethod",
                SourceFileName = "MyFile.cs",
                SourceFileLineNr = 124,
                ThreadName = "MainThread",
                ExceptionString = "System.Exception: Something went wrong",
            },
        };
        this._fileSystem.Initialize().WithFile(this._settings.FileToWatch!).Which(a => a.HasStringContent(FileContent.Easy));
        Initialize(testee);

        // Act
        testee.Attach(notifiable);
        await testee.FinishReadingAsync();

        // Assert
        received.Should().Equal(expected);
    }

    private static class FileContent
    {
        public const string Easy =
            """
            level,time,sequence,logger,message,root,class,method,fileline,file,thread,exception
            Info,2024-06-01T12:00:00Z,42,Foo,Hello,RootLogger,MyClass,MyMethod,123,MyFile.cs,MainThread,
            Error,2024-06-01T12:01:00Z,43,Bar,World,RootLogger,MyClass,MyMethod,124,MyFile.cs,MainThread,System.Exception: Something went wrong
            """;
    }
}
