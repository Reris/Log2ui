using System;
using AwesomeAssertions;
using Log2ui.Data;
using Log2ui.Receivers;
using Log2ui.Settings;
using NSubstitute;
using Xunit;

namespace Log2Ui.Tests.Receivers;

public class ReceiverFactory_Tests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private ReceiverFactory CreateTestee()
    {
        return new ReceiverFactory(this._serviceProvider);
    }

    [Fact]
    public void Attach__ShouldCreateNew()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = Substitute.For<IReceiver>();
        var settings = new TestReceiver.Settings(this.CreateFactory(expected));
        var notifiable = Substitute.For<ILogMessageNotifiable>();
        expected.Attach(notifiable).Returns((true, 1));

        // Act
        testee.Attach(settings, notifiable);

        // Assert
        settings.Factory.Received().Invoke();
        expected.Received().Initialize();
        expected.Received().Attach(notifiable);
        testee.Attached.Should().HaveCount(1);
    }

    [Fact]
    public void Attach_AlreadyExists_ShouldAttach()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = Substitute.For<IReceiver>();
        var settings = new TestReceiver.Settings(this.CreateFactory(expected));
        var notifiable1 = Substitute.For<ILogMessageNotifiable>();
        var notifiable2 = Substitute.For<ILogMessageNotifiable>();
        expected.Attach(notifiable1).Returns((true, 1));
        expected.Attach(notifiable2).Returns((true, 2));
        testee.Attach(settings, notifiable1);

        // Act
        testee.Attach(settings, notifiable2);

        // Assert
        settings.Factory.Received(1).Invoke();
        expected.Received(1).Initialize();
        expected.Received().Attach(notifiable1);
        expected.Received().Attach(notifiable2);
        testee.Attached.Should().HaveCount(2);
    }

    [Fact]
    public void Detach_All_ShouldCallInOrder()
    {
        // Arrange
        var testee = this.CreateTestee();
        var expected = Substitute.For<IReceiver>();
        var settings = new TestReceiver.Settings(this.CreateFactory(expected));
        settings.Should().BeSameAs(settings);
        settings.Should().Be(settings);
        var notifiable1 = Substitute.For<ILogMessageNotifiable>();
        var notifiable2 = Substitute.For<ILogMessageNotifiable>();
        expected.Attach(notifiable1).Returns((true, 1), (false, 1));
        expected.Attach(notifiable2).Returns((true, 2));
        testee.Attach(settings, notifiable1);
        testee.Attach(settings, notifiable2);

        expected.Detach(notifiable1).Returns((true, 1), (false, 1));
        expected.Detach(notifiable2).Returns((true, 0));

        // Act
        testee.Detach(settings, notifiable1);
        testee.Detach(settings, notifiable1);
        testee.Detach(settings, notifiable2);

        // Assert
        Received.InOrder(() =>
        {
            expected.Detach(notifiable1);
            expected.Detach(notifiable1);
            expected.Detach(notifiable2);
            expected.Terminate();
        });
        testee.Attached.Should().BeEmpty();
    }

    private Func<IReceiver> CreateFactory(IReceiver expected)
    {
        var factory = Substitute.For<Func<IReceiver>>();
        factory().Returns(expected);
        return factory;
    }

    protected abstract class TestReceiver : BaseReceiver
    {
        public record Settings(Func<IReceiver> Factory) : ReceiverSettings([])
        {
            public override string Key => CreateKey<TestReceiver>();
            public override string DisplayName => nameof(TestReceiver);

            public override ReceiverSettings DeepClone()
            {
                return this with { };
            }

            public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
            {
                return this.Factory();
            }
        }
    }
}
