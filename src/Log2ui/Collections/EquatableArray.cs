using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Log2ui.Collections;

public static class EquatableArray
{
    [OverloadResolutionPriority(10)]
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> items)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(items);
    }

    public static EquatableArray<T> Create<T>(params IEnumerable<T> items)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(items);
    }
}

[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
public readonly struct EquatableArray<T> : IReadOnlyList<T>, IEquatable<EquatableArray<T>>, IEquatableArray
    where T : IEquatable<T>
{
    private readonly T[]? _array;

    public EquatableArray(params IEnumerable<T> items)
        : this(items.ToArray())
    {
    }

    [OverloadResolutionPriority(10)]
    public EquatableArray(ReadOnlySpan<T> items)
        : this(items.ToArray())
    {
    }

    [OverloadResolutionPriority(20)]
    private EquatableArray(T[] array)
        : this()
    {
        this._array = array;
    }

    public bool Equals(EquatableArray<T> other)
    {
        if (this._array is null && other._array is null)
        {
            return true;
        }

        if (this._array is null || other._array is null)
        {
            return false;
        }

        if (this._array.Length != other._array.Length)
        {
            return false;
        }

        return this._array.SequenceEqual(other._array);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>?)this._array)?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj switch
        {
            EquatableArray<T> a => this.Equals(a),
            ImmutableArray<T> a => this.Equals(a),
            _ => false,
        };
    }

    public override int GetHashCode()
    {
        if (this._array is null)
        {
            return 0;
        }

        HashCode hashCode = default;
        foreach (var item in this._array)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this._array?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
    }

    public ImmutableArray<T> AsImmutableArray()
    {
        var array = this._array ?? [];
        var result = Unsafe.As<T[], ImmutableArray<T>>(ref Unsafe.AsRef(in array));
        return result;
    }

    public static EquatableArray<T> FromImmutableArray(ImmutableArray<T> array)
    {
        EquatableArray<T> array2 = default;
        Unsafe.AsRef(in array2._array) = Unsafe.As<ImmutableArray<T>, T[]>(ref array);
        return array2;
    }

    public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
    {
        return EquatableArray<T>.FromImmutableArray(array);
    }

    public static implicit operator EquatableArray<T>(ReadOnlySpan<T> enumerable)
    {
        return new EquatableArray<T>(enumerable);
    }

    public static implicit operator ImmutableArray<T>(EquatableArray<T> array)
    {
        return array.AsImmutableArray();
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    public EquatableArray<T> Append(T item)
    {
        return this.AsImmutableArray().Add(item);
    }

    public int Count => this._array?.Length ?? 0;

    public T this[int index] => this._array![index];
    public T this[Index index] => this._array![index];
    public EquatableArray<T> this[Range range] => new(this._array?[range] ?? []);
}
