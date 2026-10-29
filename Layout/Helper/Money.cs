using System.Globalization;

namespace Split.Layout.Helper;

public static class Money
{
    public static readonly CultureInfo Culture = new("de-DE");

    public static readonly IReadOnlyList<string> Currencies = ["EUR", "USD", "CHF", "GBP"];

    public static readonly IReadOnlyList<string> Categories =
        ["Essen & Trinken", "Einkauf", "Unterkunft", "Transport", "Freizeit", "Haushalt", "Sonstiges"];

    public static string Symbol(string currency) => currency switch
    {
        "EUR" => "€",
        "USD" => "$",
        "GBP" => "£",
        "CHF" => "CHF",
        _ => currency
    };

    public static string Format(decimal amount, string currency = "EUR")
        => $"{amount.ToString("N2", Culture)} {Symbol(currency)}";

    public static string CategoryIcon(string category) => category switch
    {
        "Essen & Trinken" => MudBlazor.Icons.Material.Filled.Restaurant,
        "Einkauf" => MudBlazor.Icons.Material.Filled.ShoppingCart,
        "Unterkunft" => MudBlazor.Icons.Material.Filled.Hotel,
        "Transport" => MudBlazor.Icons.Material.Filled.DirectionsCar,
        "Freizeit" => MudBlazor.Icons.Material.Filled.Celebration,
        "Haushalt" => MudBlazor.Icons.Material.Filled.Home,
        _ => MudBlazor.Icons.Material.Filled.Receipt
    };

    public static string Initials(string? name)
    {
        var parts = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
        };
    }
}
