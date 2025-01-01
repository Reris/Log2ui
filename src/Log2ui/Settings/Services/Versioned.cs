namespace Log2ui.Settings.Services;

public record Versioned<T>(int Version, T Data);
