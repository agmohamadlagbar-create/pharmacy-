namespace OnlineStore.Api.Helpers;

/// <summary>
/// Maps country names to ISO 4217 currency codes for price display formatting.
/// </summary>
public static class CurrencyHelper
{
    private static readonly Dictionary<string, string> CountryCurrencyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Middle East
        { "Iraq", "IQD" },
        { "Turkey", "TRY" },
        { "Türkiye", "TRY" },
        { "Iran", "IRR" },
        { "Saudi Arabia", "SAR" },
        { "United Arab Emirates", "AED" },
        { "UAE", "AED" },
        { "Kuwait", "KWD" },
        { "Qatar", "QAR" },
        { "Bahrain", "BHD" },
        { "Oman", "OMR" },
        { "Jordan", "JOD" },
        { "Lebanon", "LBP" },
        { "Syria", "SYP" },
        { "Yemen", "YER" },
        { "Palestine", "ILS" },
        { "Israel", "ILS" },
        { "Egypt", "EGP" },

        // Europe
        { "Germany", "EUR" },
        { "France", "EUR" },
        { "Italy", "EUR" },
        { "Spain", "EUR" },
        { "Netherlands", "EUR" },
        { "Belgium", "EUR" },
        { "Austria", "EUR" },
        { "Ireland", "EUR" },
        { "Portugal", "EUR" },
        { "Greece", "EUR" },
        { "Finland", "EUR" },
        { "Luxembourg", "EUR" },
        { "Slovakia", "EUR" },
        { "Slovenia", "EUR" },
        { "Estonia", "EUR" },
        { "Latvia", "EUR" },
        { "Lithuania", "EUR" },
        { "Cyprus", "EUR" },
        { "Malta", "EUR" },
        { "Croatia", "EUR" },
        { "United Kingdom", "GBP" },
        { "UK", "GBP" },
        { "Switzerland", "CHF" },
        { "Sweden", "SEK" },
        { "Norway", "NOK" },
        { "Denmark", "DKK" },
        { "Poland", "PLN" },
        { "Czech Republic", "CZK" },
        { "Hungary", "HUF" },
        { "Romania", "RON" },
        { "Bulgaria", "BGN" },
        { "Russia", "RUB" },
        { "Ukraine", "UAH" },

        // Americas
        { "United States", "USD" },
        { "USA", "USD" },
        { "Canada", "CAD" },
        { "Mexico", "MXN" },
        { "Brazil", "BRL" },
        { "Argentina", "ARS" },
        { "Colombia", "COP" },
        { "Chile", "CLP" },
        { "Peru", "PEN" },

        // Asia
        { "China", "CNY" },
        { "Japan", "JPY" },
        { "South Korea", "KRW" },
        { "India", "INR" },
        { "Pakistan", "PKR" },
        { "Indonesia", "IDR" },
        { "Malaysia", "MYR" },
        { "Thailand", "THB" },
        { "Vietnam", "VND" },
        { "Philippines", "PHP" },
        { "Singapore", "SGD" },
        { "Bangladesh", "BDT" },

        // Africa
        { "South Africa", "ZAR" },
        { "Nigeria", "NGN" },
        { "Kenya", "KES" },
        { "Morocco", "MAD" },
        { "Tunisia", "TND" },
        { "Algeria", "DZD" },
        { "Libya", "LYD" },

        // Oceania
        { "Australia", "AUD" },
        { "New Zealand", "NZD" },
    };

    /// <summary>
    /// Returns the ISO 4217 currency code for a given country name.
    /// Defaults to "USD" if the country is unknown or null.
    /// </summary>
    public static string GetCurrencyCode(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return "USD";

        return CountryCurrencyMap.TryGetValue(country.Trim(), out var code) ? code : "USD";
    }
}
