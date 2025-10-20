namespace Devlooped.Extensions.AI;

/// <summary>
/// An alternative service key that provides more flexible key comparison (case insensitive by default).
/// </summary>
/// <param name="key">The service key for use in the dependency injection container.</param>
/// <param name="comparer">The comparer used for equality comparisons, defaulting to <see cref="StringComparer.OrdinalIgnoreCase"/> if not specified.</param>
public readonly struct ServiceKey(string key, IEqualityComparer<string?>? comparer = default) : IEquatable<ServiceKey>
{
    readonly IEqualityComparer<string?> comparer = comparer ?? StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the original value of the service key.
    /// </summary>
    public string Value => key;

    /// <inheritdoc/>
    public bool Equals(ServiceKey other) => comparer.Equals(Value, other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ServiceKey k && Equals(k);

    /// <inheritdoc/>
    public override int GetHashCode() => comparer.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Compares both keys for equality.</summary>
    public static bool operator ==(ServiceKey left, ServiceKey right) => left.Equals(right);

    /// <summary>Compares both keys for inequality.</summary>
    public static bool operator !=(ServiceKey left, ServiceKey right) => !(left == right);
}