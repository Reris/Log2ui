using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using MSMQ.Messaging;

namespace Log2ui.Receivers;

public class MsmqReceiver(MsmqReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    [NonSerialized]
    private const int QueueCheckTimerDelayAndInterval = 5000;

    private readonly Settings _settings = settings;

    [NonSerialized]
    private MessageQueue? _queue;

    [NonSerialized]
    private Timer? _queueCreationCheckTimer;


    [Browsable(false)]
    public override string SampleClientConfig => "Configuration for NLog:" + Environment.NewLine +
                                                 "<target name=\"queue\" type=\"MSMQ\" layout=\"${log4jxmlevent}\" " + Environment.NewLine +
                                                 "\tqueue = \".\\private$\\nlog\" recoverable= \"true\"" + Environment.NewLine +
                                                 "\tlabel = \"${logger}\" />" + Environment.NewLine +
                                                 Environment.NewLine + Environment.NewLine +
                                                 "Configuration for log4net:" + Environment.NewLine +
                                                 Environment.NewLine +
                                                 "NOTE:  log4net (1.2.10) does not include an MSMQ appender.  The following configuration is based on the MSMQ Appender in '.\\examples\\net\\1.0\\Appenders\\SampleAppendersApp\\cs\\src' that is included in the log4net download." +
                                                 Environment.NewLine + Environment.NewLine +
                                                 "<appender name=\"MsmqAppender\" type=\"SampleAppendersApp.Appender.MsmqAppender, SampleAppendersApp\">" +
                                                 Environment.NewLine +
                                                 "\t<queueName value=\".\\Private$\\log-test\" />" + Environment.NewLine +
                                                 "\t<labelLayout value=\"LOG [%level] %date\" />" + Environment.NewLine +
                                                 "\t<layout type=\"log4net.Layout.XmlLayoutSchemaLog4j\" />" + Environment.NewLine +
                                                 "</appender>";

    public override bool IsAlive => this._queue is not null;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<MsmqReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    protected override void Initialize()
    {
        if (!MessageQueue.Exists(this._settings.QueueName))
        {
            if (this._settings.Create)
            {
                MessageQueue.Create(this._settings.QueueName, this._settings.Transactional);
            }
            else
            {
                /*
                 * Start the queue check timer.  Should the time be configurable?
                 */
                this._queueCreationCheckTimer = new Timer(
                    MsmqReceiver.QueueCreationCheckTimerFunction,
                    this,
                    MsmqReceiver.QueueCheckTimerDelayAndInterval,
                    MsmqReceiver.QueueCheckTimerDelayAndInterval);
                return;
            }
        }

        this.Start();
    }

    private void Start()
    {
        this._queue = new MessageQueue(this._settings.QueueName);

        this._queue.ReceiveCompleted += (source, asyncResult) =>
        {
            try
            {
                // End the asynchronous receive operation.
                var m = ((MessageQueue)source).EndReceive(asyncResult.AsyncResult);
                this.Notify(this.Read(m));


                if (this._settings.BulkProcessBackedUpMessages)
                {
                    var all = ((MessageQueue)source).GetAllMessages();
                    if (all.Length > 0)
                    {
                        var numberofmessages = all.Length > 1000 ? 1000 : all.Length;

                        var logs = new LogMessage[numberofmessages];

                        for (var i = 0; i < numberofmessages; i++)
                        {
                            var thisone = ((MessageQueue)source).Receive();
                            logs[i] = this.Read(thisone);
                        }

                        this.Notify(logs);
                    }
                }

                ((MessageQueue)source).BeginReceive();
            }
            catch (MessageQueueException)
            {
                // Handle sources of MessageQueueException.
            }
        };

        this._queue.BeginReceive();
    }

    protected override void Terminate()
    {
        /*
         * Are we going to have any issues if we are processing a receive complete or will
         * MSMQ protect us?
         */
        if (this._queue is not null)
        {
            this._queue.Close();
            this._queue = null;
        }
    }

    private LogMessage Read(Message m)
    {
        var loggingEvent = Encoding.ASCII.GetString(((MemoryStream)m.BodyStream).ToArray());
        var logMsg = ReceiverUtils.ParseLog4JXmlLogEvent(loggingEvent, "MSMQLogger");
        logMsg.LoggerName = $"{this._settings.QueueName.TrimStart('.')}_{logMsg.LoggerName}";
        logMsg.RootLoggerName = this._settings.QueueName;
        return logMsg;
    }

    private static void QueueCreationCheckTimerFunction(object state)
    {
        //TODO: If this timer gets called then we did not finish the job before the maximum allowable time.
        //_logger.Fatal("JobMaxExecutionTimerFunction");

        if (state is not MsmqReceiver rcv || !MessageQueue.Exists(rcv._settings.QueueName))
        {
            return;
        }

        rcv._queueCreationCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
        rcv._queueCreationCheckTimer.Dispose();
        rcv.Start();
    }

    [ReceiverSettingsDiscriminator(nameof(MsmqReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultMappings)
    {
        private static readonly EquatableArray<FieldMapping> DefaultMappings = [];

        public override string Key => ReceiverSettings.CreateKey("Msmq", this.QueueName);
        public override string DisplayName => $"MSMQ {this.QueueName}";
        public override string TypeDisplayName => "Windows Message Queue (MSMQ)";

        [Category("Configuration")]
        [DisplayName("Queue Name")]
        [Description(@"Name of the queue to create.  I.e. .\private$\log-test")]
        [DefaultValue(@".\private$\log")]
        public string QueueName
        {
            get;
            set => this.SetField(ref field, value);
        } = @".\private$\log";

        [Category("Configuration")]
        [DisplayName("Create Queue")]
        [Description(
            "Determines how to handle queue creation. If true and the queue does not exist it will be created. " +
            "If false and the queue does not exist the receiver will wait for the queue to be created.")]
        public bool Create
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Configuration")]
        public bool Transactional
        {
            get;
            set => this.SetField(ref field, value);
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        [DisplayName("Bulk Process Backed Up Messages")]
        [Description("If true multiple messages in the queue are processed as one update to the log viewer. This improves the performance of the viewer")]
        public bool BulkProcessBackedUpMessages
        {
            get;
            set => this.SetField(ref field, value);
        }


        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<MsmqReceiver>(serviceProvider, this);
        }
    }
}
