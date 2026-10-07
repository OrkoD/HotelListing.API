namespace HotelListing.Api.Domain.ValueObjects;

public readonly record struct Money : IComparable<Money>
{
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }

    public Money(decimal amount, CurrencyCode currency)
    {
        if (!currency.IsInitialized)
            throw new ArgumentException("Money cannot be created with an uninitialized CurrencyCode.", nameof(currency));


        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(CurrencyCode currency) => new(0m, currency);
    public static Money Usd(decimal amount) => new(amount, CurrencyCode.USD);
    public static Money Eur(decimal amount) => new(amount, CurrencyCode.EUR);
    public static Money Uah(decimal amount) => new(amount, CurrencyCode.UAH);

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    // --- Arithmetic Operators ---
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money money) =>
        new(-money.Amount, money.Currency);

    // Kept unrounded for intermediate precision (decimal 18,4)
    public static Money operator *(Money left, decimal right) =>
        new(left.Amount * right, left.Currency);

    public static Money operator *(decimal multiplier, Money money) =>
        money * multiplier;

    // --- Comparison Operators ---
    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    /// <summary>
    /// Explicitly rounds to standard currency precision (default 2 places) using Banker's Rounding (MidpointRounding.ToEven).
    /// </summary>
    public Money Round(int decimals = 2, MidpointRounding mode = MidpointRounding.ToEven) =>
        new(Math.Round(Amount, decimals, mode), Currency);

    public override string ToString() => $"{Amount:F2} {Currency.Value}";

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Currency mismatch: cannot compare or operate between '{a.Currency.Value}' and '{b.Currency.Value}'.");
    }
}
