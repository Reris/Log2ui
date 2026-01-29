using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Log2ui.Collections;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

public class SilverlightSocketPolicyReceiver(SilverlightSocketPolicyReceiver.Settings settings) : BaseReceiver, ISelfRegistering
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
    private Socket? _socket;

    [Browsable(false)]
    public override string SampleClientConfig => "This receiver allows Silverlight client to use sockets";

    public override bool IsAlive => this._socket?.IsBound is true;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<SilverlightSocketPolicyReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    protected override void Initialize()
    {
        if (this._socket != null)
        {
            return;
        }

        this._policy = Encoding.UTF8.GetBytes(string.Format(SilverlightSocketPolicyReceiver.PolicyTemplate, settings.PortFrom, settings.PortTo));

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

    protected override void Terminate()
    {
        if (this._socket == null)
        {
            return;
        }

        this._socket.Close();
        this._socket = null;
    }

    [ReceiverSettingsDiscriminator(nameof(SilverlightSocketPolicyReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultProperties)
    {
        private static readonly EquatableArray<LogColumn> DefaultProperties = [];

        public override string Key => ReceiverSettings.CreateKey<SilverlightSocketPolicyReceiver>(this.PortFrom, this.PortTo);
        public override string DisplayName => $"Silverlight :{this.PortFrom}-{this.PortTo}";
        public override string TypeDisplayName => "Silverlight Socket Policy";

        [Category("Configuration")]
        [DisplayName("TCP Port From")]
        [DefaultValue(4502)]
        public int PortFrom
        {
            get;
            set => this.SetField(ref field, value);
        } = 4502;

        [Category("Configuration")]
        [DisplayName("TCP Port To")]
        [DefaultValue(4532)]
        public int PortTo
        {
            get;
            set => this.SetField(ref field, value);
        } = 4532;


        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<SilverlightSocketPolicyReceiver>(serviceProvider, this);
        }
    }
}
