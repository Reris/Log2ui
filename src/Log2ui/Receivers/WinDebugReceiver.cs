using System;
using System.ComponentModel;
using System.Diagnostics;
using Log2ui.Data;

namespace Log2ui.Receivers;

[Serializable]
[DisplayName("WinDebug (OutputDebugString)")]
public class WinDebugReceiver : BaseReceiver
{
    [Browsable(false)]
    public override string SampleClientConfig => "N/A";

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

        var logMsg = new LogMessage();
        logMsg.Message = text;
        logMsg.LoggerName = processName;
        logMsg.LoggerName = $"{processName}.{pid}";
        logMsg.Level = LogLevel.Debug;
        logMsg.ThreadName = pid.ToString();
        logMsg.TimeStamp = DateTime.Now;
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
}
