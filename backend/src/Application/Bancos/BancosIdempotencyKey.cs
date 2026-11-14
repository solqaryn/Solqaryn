namespace Solqaryn.Application.Bancos;

/// <summary>
/// Validated idempotency key for banking write operations.
/// </summary>
public sealed record BancosIdempotencyKey
{
    public const int MaxLength = 100;
    public string Value { get; }

    private BancosIdempotencyKey(string value)
    {
        Value = value;
    }

    public static BancosIdempotencyKey Create(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Idempotency key cannot be null, empty, or whitespace.", nameof(key));

        var normalized = key.Trim();
        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Idempotency key exceeds maximum length of {MaxLength}.", nameof(key));

        foreach (var c in normalized)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                throw new ArgumentException($"Idempotency key contains unsafe character: {c}", nameof(key));
        }

        return new BancosIdempotencyKey(normalized);
    }

    public override string ToString() => Value;
}
