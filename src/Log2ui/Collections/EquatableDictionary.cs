using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Log2ui.Collections;

public static class EquatableDictionary
{
    [OverloadResolutionPriority(20)]
    public static EquatableDictionary<TKey, TValue> Create<TKey, TValue>(ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
        where TKey : IEquatable<TKey>
        where TValue : IEquatable<TValue>
    {
        return new EquatableDictionary<TKey, TValue>(items);
    }

    public static EquatableDictionary<TKey, TValue> Create<TKey, TValue>(params IEnumerable<(TKey Key, TValue Value)> items)
        where TKey : IEquatable<TKey>
        where TValue : IEquatable<TValue>
    {
        return new EquatableDictionary<TKey, TValue>(items);
    }

    [OverloadResolutionPriority(10)]
    public static EquatableDictionary<TKey, TValue> Create<TKey, TValue>(params IEnumerable<KeyValuePair<TKey, TValue>>? items)
        where TKey : IEquatable<TKey>
        where TValue : IEquatable<TValue>
    {
        return new EquatableDictionary<TKey, TValue>(items ?? ImmutableDictionary<TKey, TValue>.Empty);
    }
}

[CollectionBuilder(typeof(EquatableDictionary), nameof(EquatableDictionary.Create))]
public readonly struct EquatableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IEquatable<EquatableDictionary<TKey, TValue>>, IEquatableDictionary
    where TKey : IEquatable<TKey>
    where TValue : IEquatable<TValue>
{
    private readonly ImmutableDictionary<TKey, TValue>? _dictionary;

    public EquatableDictionary(params IEnumerable<(TKey Key, TValue Value)> items)
        : this(items.ToImmutableDictionary(a => a.Key, a => a.Value))
    {
    }

    public EquatableDictionary(params IEnumerable<KeyValuePair<TKey, TValue>> items)
        : this(items.ToImmutableDictionary())
    {
    }

    [OverloadResolutionPriority(10)]
    public EquatableDictionary(ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
        : this(items.ToArray().ToImmutableDictionary())
    {
    }

    [OverloadResolutionPriority(20)]
    private EquatableDictionary(ImmutableDictionary<TKey, TValue> dictionary)
        : this()
    {
        this._dictionary = dictionary;
    }

    public bool Equals(EquatableDictionary<TKey, TValue> other)
    {
        var my = this._dictionary ?? ImmutableDictionary<TKey, TValue>.Empty;
        var oth = other._dictionary ?? ImmutableDictionary<TKey, TValue>.Empty;
        return EquatableDictionary<TKey, TValue>.KeyValueEqual(my, oth);
    }

    private static bool KeyValueEqual(ImmutableDictionary<TKey, TValue> my, ImmutableDictionary<TKey, TValue> oth)
    {
        if (my.Count != oth.Count)
        {
            return false;
        }

        foreach (var (key, value) in my)
        {
            if (!oth.TryGetValue(key, out var otherValue) || !object.Equals(value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<TKey, TValue>>?)this._dictionary)?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj switch
        {
            EquatableDictionary<TKey, TValue> a => this.Equals(a),
            ImmutableDictionary<TKey, TValue> a => this.Equals(a),
            _ => false,
        };
    }

    public override int GetHashCode()
    {
        if (this._dictionary is null)
        {
            return 0;
        }

        HashCode hashCode = default;
        foreach (var item in this._dictionary)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this._dictionary?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
    }

    public ImmutableDictionary<TKey, TValue> AsImmutableDictionary()
    {
        return this._dictionary ?? [];
    }

    public static EquatableDictionary<TKey, TValue> FromImmutableDictionary(ImmutableDictionary<TKey, TValue> dictionary)
    {
        return new EquatableDictionary<TKey, TValue>(dictionary);
    }

    public static implicit operator EquatableDictionary<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary)
    {
        return EquatableDictionary<TKey, TValue>.FromImmutableDictionary(dictionary);
    }

    public static implicit operator ImmutableDictionary<TKey, TValue>(EquatableDictionary<TKey, TValue> array)
    {
        return array.AsImmutableDictionary();
    }

    public static bool operator ==(EquatableDictionary<TKey, TValue> left, EquatableDictionary<TKey, TValue> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableDictionary<TKey, TValue> left, EquatableDictionary<TKey, TValue> right)
    {
        return !left.Equals(right);
    }

    public EquatableDictionary<TKey, TValue> Append(TKey key, TValue value)
    {
        return this.AsImmutableDictionary().Add(key, value);
    }

    public EquatableDictionary<TKey, TValue> Append(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        return this.AsImmutableDictionary().AddRange(items);
    }

    public EquatableDictionary<TKey, TValue> Remove(TKey key)
    {
        return this.AsImmutableDictionary().Remove(key);
    }

    public int Count => this._dictionary?.Count ?? 0;

    public bool ContainsKey(TKey key)
    {
        return this._dictionary?.ContainsKey(key) is true;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        value = default;
        return this._dictionary?.TryGetValue(key, out value) is true;
    }

    public TValue this[TKey key]
    {
        get
        {
            var dic = this._dictionary ?? ImmutableDictionary<TKey, TValue>.Empty;
            return dic[key];
        }
    }

    public IEnumerable<TKey> Keys => this._dictionary?.Keys ?? Enumerable.Empty<TKey>();
    public IEnumerable<TValue> Values => this._dictionary?.Values ?? Enumerable.Empty<TValue>();
}
