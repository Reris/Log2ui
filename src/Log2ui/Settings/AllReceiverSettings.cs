using Log2ui.Collections;

namespace Log2ui.Settings;

public record AllReceiverSettings
{
    public EquatableArray<ReceiverSettings> Receivers { get; set; }
}
