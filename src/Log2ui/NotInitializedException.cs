using System;

namespace Log2ui;

public class NotInitializedException(string? name = null)
    : ApplicationException(name is null ? "Access to a value that has not been initialized." : $"'{name} has not been initialized");
