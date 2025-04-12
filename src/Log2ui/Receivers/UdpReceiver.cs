using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Log2ui.Receivers;

[Serializable]
[DisplayName("UDP (IP v4 and v6)")]
public class UdpReceiver : BaseReceiver
{
    private string _address = string.Empty;
    private int _bufferSize = 10000;

    private bool _ipv6;
    private int _port = 7071;

    [NonSerialized]
    private IPEndPoint? _remoteEndPoint;

    [NonSerialized]
    private UdpClient? _udpClient;

    [NonSerialized]
    private Thread? _worker;

    [Category("Configuration")]
    [DisplayName("UDP Port Number")]
    [DefaultValue(7071)]
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
    [DisplayName("Multicast Group Address (Optional)")]
    public string Address
    {
        get => this._address;
        set => this._address = value;
    }

    [Category("Configuration")]
    [DisplayName("Receive Buffer Size")]
    public int BufferSize
    {
        get => this._bufferSize;
        set => this._bufferSize = value;
    }

    [Browsable(false)]
    public override string SampleClientConfig => """
                                                 Configuration for log4net:
                                                 <appender name="UdpAppender" type="log4net.Appender.UdpAppender">
                                                   <remoteAddress value="localhost" />
                                                   <remotePort value="7071" />
                                                   <layout type="log4net.Layout.XmlLayoutSchemaLog4j" />
                                                 </appender>
                                                 """;

    public void Clear()
    {
    }

    private void Start()
    {
        while (this._udpClient != null && this._remoteEndPoint != null)
        {
            try
            {
                var buffer = this._udpClient.Receive(ref this._remoteEndPoint);
                var loggingEvent = Encoding.UTF8.GetString(buffer);

                //Console.WriteLine(loggingEvent);
                //  Console.WriteLine("Count: " + count++);

                var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "UdpLogger");
                logMsg.RootLoggerName = this._remoteEndPoint.Address.ToString().Replace(".", "-");
                logMsg.LoggerName = $"{this._remoteEndPoint.Address.ToString().Replace(".", "-")}_{logMsg.LoggerName}";
                this.Notify(logMsg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }
    }

    protected override void Initialize()
    {
        if (this._worker is { IsAlive: true })
        {
            return;
        }

        // Init connexion here, before starting the thread, to know the status now
        this._remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        this._udpClient = this._ipv6 ? new UdpClient(this._port, AddressFamily.InterNetworkV6) : new UdpClient(this._port);
        this._udpClient.Client.ReceiveBufferSize = this._bufferSize;
        if (!string.IsNullOrEmpty(this._address))
        {
            this._udpClient.JoinMulticastGroup(IPAddress.Parse(this._address));
        }

        // We need a working thread
        this._worker = new Thread(this.Start)
        {
            IsBackground = true,
        };
        this._worker.Start();
    }

    protected override void Terminate()
    {
        if (this._udpClient != null)
        {
            this._udpClient.Close();
            this._udpClient = null;

            this._remoteEndPoint = null;
        }

        if (this._worker != null && this._worker.IsAlive)
        {
            this._worker.Abort();
        }

        this._worker = null;
    }
}
