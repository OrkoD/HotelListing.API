namespace HotelListing.Api.Domain.ValueObjects;

public readonly record struct CurrencyCode
{
    private static readonly HashSet<string> SupportedCodes = new(StringComparer.Ordinal)
    {
        "USD", "EUR", "UAH", "GBP", "PLN"
    };

    public string Value { get; }

    public CurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency code cannot be null or empty.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!SupportedCodes.Contains(normalized))
            throw new ArgumentException($"'{normalized}' is not a recognized or supported currency code.", nameof(value));

        Value = normalized;
    }

    public static readonly CurrencyCode USD = new("USD");
    public static readonly CurrencyCode EUR = new("EUR");
    public static readonly CurrencyCode UAH = new("UAH");
    public static readonly CurrencyCode GBP = new("GBP");
    public static readonly CurrencyCode PLN = new("PLN");

    public bool IsInitialized => Value is not null;

    public override string ToString() => Value ?? string.Empty;
}
