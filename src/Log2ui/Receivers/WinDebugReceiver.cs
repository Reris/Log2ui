using System;
using System.ComponentModel;
using System.Diagnostics;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Receivers;

public class WinDebugReceiver(WinDebugReceiver.Settings settings) : BaseReceiver, ISelfRegistering
{
    [Browsable(false)]
    public override string SampleClientConfig => "N/A";

    public override bool IsAlive => true;

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<WinDebugReceiver>();
        ReceiverSettingsDiscriminatorAttribute.Register<Settings>(registry.Collection);
    }

    protected override void Initialize()
    {
        DebugMonitor.OnOutputDebugString += this.DebugMonitor_OnOutputDebugString;
        DebugMonitor.Start();
    }

    protected override void Terminate()
    {
        DebugMonitor.OnOutputDebugString -= this.DebugMonitor_OnOutputDebugString;
        DebugMonitor.Stop();
    }

    private void DebugMonitor_OnOutputDebugString(int pid, string text)
    {
        // Trim ending newline (if any) 
        if (text.EndsWith(Environment.NewLine))
        {
            text = text.Substring(0, text.Length - Environment.NewLine.Length);
        }

        // Replace dots by "middle dots" to preserve Logger namespace
        var processName = WinDebugReceiver.GetProcessName(pid);
        processName = processName.Replace('.', '·');

        var logMsg = new LogMessage
        {
            Message = text,
            LoggerName = $"{processName}.{pid}",
            Level = LogLevel.Debug,
            ThreadName = pid.ToString(),
            TimeStamp = DateTime.Now,
        };
        this.Notify(logMsg);
    }

    private static string GetProcessName(int pid)
    {
        if (pid == -1)
        {
            return Process.GetCurrentProcess().ProcessName;
        }

        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            return "<exited>";
        }
    }

    [ReceiverSettingsDiscriminator(nameof(WinDebugReceiver), 1)]
    public record Settings() : ReceiverSettings(Settings.DefaultProperties)
    {
        private static readonly EquatableArray<LogColumn> DefaultProperties = [];

        public override string Key => ReceiverSettings.CreateKey("WinDebug");
        public override string DisplayName => "WinDebug";
        public override string TypeDisplayName => "WinDebug (OutputDebugString)";


        public override ReceiverSettings DeepClone()
        {
            return this with { };
        }

        public override IReceiver CreateReceiver(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<WinDebugReceiver>(serviceProvider, this);
        }
    }
}
