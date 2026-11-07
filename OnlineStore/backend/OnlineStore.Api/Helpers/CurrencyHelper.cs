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

    private static readonly Dictionary<string, string> CountryCurrencySymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Turkey"] = "₺", ["Türkiye"] = "₺", ["TR"] = "₺",
        ["United States"] = "$", ["US"] = "$", ["USA"] = "$",
        ["United Kingdom"] = "£", ["UK"] = "£", ["GB"] = "£",
        ["Germany"] = "€", ["France"] = "€", ["Italy"] = "€", ["Spain"] = "€",
        ["Netherlands"] = "€", ["Belgium"] = "€", ["Austria"] = "€",
        ["Ireland"] = "€", ["Portugal"] = "€", ["Greece"] = "€", ["Finland"] = "€",
        ["EU"] = "€",
        ["Japan"] = "¥", ["JP"] = "¥",
        ["China"] = "¥", ["CN"] = "¥",
        ["India"] = "₹", ["IN"] = "₹",
        ["Russia"] = "₽", ["RU"] = "₽",
        ["Brazil"] = "R$", ["BR"] = "R$",
        ["South Korea"] = "₩", ["KR"] = "₩",
        ["Saudi Arabia"] = "﷼", ["SA"] = "﷼",
        ["UAE"] = "د.إ", ["United Arab Emirates"] = "د.إ",
        ["Egypt"] = "£", ["EG"] = "E£",
        ["Iraq"] = "ع.د", ["IQ"] = "ع.د",
        ["Syria"] = "£", ["SY"] = "S£",
        ["Lebanon"] = "ل.ل", ["LB"] = "ل.ل",
        ["Jordan"] = "د.ا", ["JO"] = "د.ا",
        ["Kuwait"] = "د.ك", ["KW"] = "د.ك",
        ["Qatar"] = "ر.ق", ["QA"] = "ر.ق",
        ["Bahrain"] = "د.ب", ["BH"] = "د.ب",
        ["Oman"] = "ر.ع", ["OM"] = "ر.ع",
        ["Pakistan"] = "₨", ["PK"] = "₨",
        ["Nigeria"] = "₦", ["NG"] = "₦",
        ["South Africa"] = "R", ["ZA"] = "R",
        ["Mexico"] = "$", ["MX"] = "MX$",
        ["Canada"] = "C$", ["CA"] = "C$",
        ["Australia"] = "A$", ["AU"] = "A$",
        ["Switzerland"] = "CHF", ["CH"] = "CHF",
        ["Sweden"] = "kr", ["SE"] = "kr",
        ["Norway"] = "kr", ["NO"] = "kr",
        ["Denmark"] = "kr", ["DK"] = "kr",
        ["Poland"] = "zł", ["PL"] = "zł",
        ["Czech Republic"] = "Kč", ["CZ"] = "Kč",
        ["Hungary"] = "Ft", ["HU"] = "Ft",
        ["Romania"] = "lei", ["RO"] = "lei",
        ["Thailand"] = "฿", ["TH"] = "฿",
        ["Malaysia"] = "RM", ["MY"] = "RM",
        ["Indonesia"] = "Rp", ["ID"] = "Rp",
        ["Philippines"] = "₱", ["PH"] = "₱",
        ["Vietnam"] = "₫", ["VN"] = "₫",
        ["Singapore"] = "S$", ["SG"] = "S$",
        ["Israel"] = "₪", ["IL"] = "₪",
        ["Argentina"] = "AR$", ["AR"] = "AR$",
        ["Colombia"] = "COL$", ["CO"] = "COL$",
        ["Chile"] = "CLP$", ["CL"] = "CLP$",
        ["Peru"] = "S/", ["PE"] = "S/"
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

    /// <summary>
    /// Returns the display currency symbol for a given country name or code.
    /// Defaults to "$" if the country is unknown or null.
    /// </summary>
    public static string GetCurrencySymbol(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return "$";

        return CountryCurrencySymbolMap.TryGetValue(country.Trim(), out var symbol) ? symbol : "$";
    }
}
