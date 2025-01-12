namespace Log2ui.Collections.Observables;

public class ValueRelay<T>(T value) : IValueRelay<T>
{
    public T Value { get; set; } = value;
}
