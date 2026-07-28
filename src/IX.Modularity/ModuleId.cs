using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace IX.Modularity;

/// <summary>
/// Identifies a module independently of its display name or version.
/// </summary>
public readonly partial struct ModuleId : IEquatable<ModuleId>
{
    private readonly string? _value;

    /// <summary>
    /// Initializes a new module identifier.
    /// </summary>
    /// <param name="value">A lowercase, dot- or hyphen-separated identifier.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid identifier.</exception>
    public ModuleId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!ModuleIdPattern.IsMatch(value))
        {
            throw new ArgumentException(
                "Module identifiers must be lowercase and contain only letters, digits, dots, and hyphens.",
                nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the validated identifier value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is an uninitialized default instance.</exception>
    public string Value => _value ?? throw new InvalidOperationException("A default ModuleId is not initialized.");

    /// <summary>
    /// Parses a module identifier.
    /// </summary>
    /// <param name="value">The identifier to parse.</param>
    /// <returns>The parsed identifier.</returns>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid identifier.</exception>
    public static ModuleId Parse(string value)
    {
        return !TryParse(value, out ModuleId result) ? throw new FormatException($"'{value}' is not a valid module identifier.") : result;
    }

    /// <summary>
    /// Attempts to parse a module identifier.
    /// </summary>
    /// <param name="value">The identifier to parse.</param>
    /// <param name="result">The parsed identifier when parsing succeeds.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? value,
        out ModuleId result)
    {
        if (value is not null && ModuleIdPattern.IsMatch(value))
        {
            result = new ModuleId(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool Equals(ModuleId other)
    {
        return StringComparer.Ordinal.Equals(_value, other._value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ModuleId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _value is null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether two identifiers are equal.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the identifiers are equal; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(ModuleId left, ModuleId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two identifiers are different.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true"/> when the identifiers are different; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(ModuleId left, ModuleId right)
    {
        return !left.Equals(right);
    }

    [GeneratedRegex(
        "^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex ModuleIdPattern
    {
        get;
    }
}
