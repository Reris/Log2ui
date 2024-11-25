using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Log2ui.Receivers;

[Serializable]
[DisplayName("TCP (IP v4 and v6)")]
public class TcpReceiver : BaseReceiver
{
    private int _bufferSize = 10000;
    private bool _ipv6;
    private int _port = 4505;

    [NonSerialized]
    private Socket? _socket;

    [Category("Configuration")]
    [DisplayName("TCP Port Number")]
    [DefaultValue(4505)]
    public int Port
    {
        get => this._port;
        set => this._port = value;
    }

    [Category("Configuration")]
    [DisplayName("Use IPv6 Addresses")]
    [DefaultValue(false)]
    public bool IpV6
    {
        get => this._ipv6;
        set => this._ipv6 = value;
    }

    [Category("Configuration")]
    [DisplayName("Receive Buffer Size")]
    [DefaultValue(10000)]
    public int BufferSize
    {
        get => this._bufferSize;
        set => this._bufferSize = value;
    }

    [Browsable(false)]
    public override string SampleClientConfig => """
                                                 Configuration for NLog:
                                                 <target name="TcpOutlet" xsi:type="NLogViewer" address="tcp://localhost:4505"/>
                                                 """;

    public override void Initialize()
    {
        if (this._socket != null)
        {
            return;
        }

        this._socket = new Socket(this._ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        this._socket.ExclusiveAddressUse = true;
        this._socket.Bind(new IPEndPoint(this._ipv6 ? IPAddress.IPv6Any : IPAddress.Any, this._port));
        this._socket.Listen(100);
        this._socket.ReceiveBufferSize = this._bufferSize;

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
                logMsg.LoggerName = string.Format(":{1}.{0}", logMsg.LoggerName, this._port);

                if (this.Notifiable != null)
                {
                    this.Notifiable.Notify(logMsg);
                }
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

    public override void Terminate()
    {
        if (this._socket == null)
        {
            return;
        }

        this._socket.Close();
        this._socket = null;
    }
}
