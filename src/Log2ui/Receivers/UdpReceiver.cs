using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Log2ui.Collections;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Log2ui.Receivers;

public class UdpReceiver(UdpReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    private static readonly ILogger Logger = Log.ForContext<UdpReceiver>();
    private UdpClient? _udpClient;

    [Browsable(false)]
    public override string SampleClientConfig => """
                                                 Configuration for log4net:
                                                 <appender name="UdpAppender" type="log4net.Appender.UdpAppender">
                                                   <remoteAddress value="localhost" />
                                                   <remotePort value="7071" />
                                                   <layout type="log4net.Layout.XmlLayoutSchemaLog4j" />
                                                 </appender>
                                                 """;

    public override bool IsAlive => this._udpClient?.Client.IsBound is true;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<UdpReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    private async Task StartAsync()
    {
        try
        {
            while (this._udpClient is not null)
            {
                var received = await this._udpClient.ReceiveAsync().AwaitInPool();
                var buffer = received.Buffer;
                var remoteEndPoint = received.RemoteEndPoint;
                var loggingEvent = Encoding.UTF8.GetString(buffer);

                var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "UdpLogger");
                logMsg.RootLoggerName = remoteEndPoint.Address.ToString().Replace(".", "-");
                logMsg.LoggerName = $"{remoteEndPoint.Address.ToString().Replace(".", "-")}_{logMsg.LoggerName}";
                this.Notify(logMsg);
            }
        }
        catch (SocketException)
        {
        }
        catch (Exception e)
        {
            UdpReceiver.Logger.Error(e, e.Message);
        }
    }

    protected override void Initialize()
    {
        if (this._udpClient is not null)
        {
            return;
        }

        // Init connection here, before starting the thread, to know the status now
        this._udpClient = settings.IpV6 ? new UdpClient(settings.Port, AddressFamily.InterNetworkV6) : new UdpClient(settings.Port);
        this._udpClient.Client.ReceiveBufferSize = settings.BufferSize;
        if (!string.IsNullOrEmpty(settings.Address))
        {
            this._udpClient.JoinMulticastGroup(IPAddress.Parse(settings.Address));
        }

        this.StartAsync().FireAndForget();
    }

    protected override void Terminate()
    {
        if (this._udpClient is null)
        {
            return;
        }

        this._udpClient.Close();
        this._udpClient = null;
    }

    [ReceiverSettingsDiscriminator(nameof(UdpReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultProperties)
    {
        private static readonly EquatableArray<LogColumn> DefaultProperties = [];

        public override string Key => ReceiverSettings.CreateKey<UdpReceiver>(this.IpV6 ? "IPv6" : "IPv4", this.Port);
        public override string DisplayName => $"UDP :{this.Port}";
        public override string TypeDisplayName => "UDP";

        [Category("Configuration")]
        [DisplayName("UDP Port Number")]
        [DefaultValue(7071)]
        public int Port
        {
            get;
            set => this.SetField(ref field, value);
        } = 7071;

        [Category("Configuration")]
        [DisplayName("Use IPv6 Addresses")]
        [DefaultValue(false)]
        public bool IpV6
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Configuration")]
        [DisplayName("Multicast Group Address (Optional)")]
        public string? Address
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Configuration")]
        [DisplayName("Receive Buffer Size")]
        [DefaultValue(10000)]
        public int BufferSize
        {
            get;
            set => this.SetField(ref field, value);
        } = 10000;

        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<UdpReceiver>(serviceProvider, this);
        }
    }
}
