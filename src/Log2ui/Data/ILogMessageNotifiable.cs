namespace Log2ui.Data;

public interface ILogMessageNotifiable
{
    void Notify(LogMessage[] messages);
    void Notify(LogMessage message);
}
