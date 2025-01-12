using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

[DisplayName("TCP (IP v4 and v6)")]
public class TcpReceiver(TcpReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    private Socket? _socket;

    public override string SampleClientConfig => """
                                                 Configuration for NLog:
                                                 <target name="TcpOutlet" xsi:type="NLogViewer" address="tcp://localhost:4505"/>
                                                 """;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<TcpReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    protected override void Initialize()
    {
        if (this._socket != null)
        {
            return;
        }

        this._socket = new Socket(settings.IpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        this._socket.ExclusiveAddressUse = true;
        this._socket.Bind(new IPEndPoint(settings.IpV6 ? IPAddress.IPv6Any : IPAddress.Any, settings.Port));
        this._socket.Listen(100);
        this._socket.ReceiveBufferSize = settings.BufferSize;

        var args = new SocketAsyncEventArgs();
        args.Completed += this.AcceptAsyncCompleted;

        this._socket.AcceptAsync(args);
    }

    private void AcceptAsyncCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (this._socket == null || e.SocketError != SocketError.Success)
        {
            return;
        }

        new Thread(this.Start) { IsBackground = true }.Start(e.AcceptSocket);

        e.AcceptSocket = null;
        this._socket.AcceptAsync(e);
    }

    private void Start(object newSocket)
    {
        try
        {
            using var socket = (Socket)newSocket;
            using var ns = new NetworkStream(socket, FileAccess.Read, false);
            while (this._socket != null)
            {
                var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(ns, "TcpLogger");
                logMsg.RootLoggerName = logMsg.LoggerName;
                logMsg.LoggerName = string.Format(":{1}.{0}", logMsg.LoggerName, settings.Port);
                this.Notify(logMsg);
            }
        }
        catch (IOException)
        {
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override void Terminate()
    {
        if (this._socket == null)
        {
            return;
        }

        this._socket.Close();
        this._socket = null;
    }

    [ReceiverSettingsDiscriminator(nameof(TcpReceiver), 1)]
    public record Settings : ReceiverSettings
    {
        public override string ValueKey => $"{(this.IpV6 ? "IPv6" : "IPv4")}:{this.Port}";

        [Category("Configuration")]
        [DisplayName("TCP Port Number")]
        [DefaultValue(4505)]
        public int Port { get; set; } = 4505;

        [Category("Configuration")]
        [DisplayName("Use IPv6 Addresses")]
        [DefaultValue(false)]
        public bool IpV6 { get; set; }

        [Category("Configuration")]
        [DisplayName("Receive Buffer Size")]
        [DefaultValue(10000)]
        public int BufferSize { get; set; } = 10000;

        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<TcpReceiver>(serviceProvider, this);
        }
    }
}
