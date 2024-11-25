using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Log2ui.Receivers;

[Serializable]
[DisplayName("Silverlight Socket Policy")]
public class SilverlightSocketPolicyReceiver : BaseReceiver
{
    private const string PolicyRequestString = "<policy-file-request/>";

    private const string PolicyTemplate = """
                                          <?xml version="1.0" encoding="utf-8" ?>
                                          <access-policy>
                                              <cross-domain-access>
                                                  <policy>
                                                    <allow-from><domain uri="*" /></allow-from>
                                                    <grant-to>
                                                        <socket-resource port="{0}-{1}" protocol="tcp" />
                                                    </grant-to>
                                                  </policy>
                                              </cross-domain-access>
                                          </access-policy>
                                          """;

    private byte[]? _policy;

    private int _portFrom = 4502;
    private int _portTo = 4532;

    [NonSerialized]
    private Socket? _socket;

    [Category("Configuration")]
    [DisplayName("TCP Port From")]
    [DefaultValue(4502)]
    public int PortFrom
    {
        get => this._portFrom;
        set => this._portFrom = value;
    }

    [Category("Configuration")]
    [DisplayName("TCP Port To")]
    [DefaultValue(4532)]
    public int PortTo
    {
        get => this._portTo;
        set => this._portTo = value;
    }

    [Browsable(false)]
    public override string SampleClientConfig => "This receiver allows Silverlight client to use sockets";

    public override void Initialize()
    {
        if (this._socket != null)
        {
            return;
        }

        this._policy = Encoding.UTF8.GetBytes(string.Format(SilverlightSocketPolicyReceiver.PolicyTemplate, this._portFrom, this._portTo));

        this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        this._socket.ExclusiveAddressUse = true;
        this._socket.Bind(new IPEndPoint(IPAddress.Any, 943));
        this._socket.Listen(100);

        var args = new SocketAsyncEventArgs();
        args.Completed += this.AcceptAsyncCompleted;

        this._socket.AcceptAsync(args);
    }

    private void AcceptAsyncCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (this._socket == null)
        {
            return;
        }

        var socket = e.AcceptSocket;

        e.AcceptSocket = null;
        this._socket.AcceptAsync(e);

        this.ProcessRequest(socket);
    }

    private void ProcessRequest(Socket socket)
    {
        using var client = new TcpClient();
        client.Client = socket;
        client.ReceiveTimeout = 5000;
        using var s = client.GetStream();
        var buffer = new byte[SilverlightSocketPolicyReceiver.PolicyRequestString.Length];
        s.Read(buffer, 0, buffer.Length);
        s.Write(this._policy, 0, this._policy.Length);
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
