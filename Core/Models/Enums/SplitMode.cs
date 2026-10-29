namespace Split.Core.Models.Enums;

/// <summary>Aufteilungsarten einer Ausgabe (so in der Datenbank gespeichert).</summary>
public static class SplitMode
{
    public const string Equal = "equal";
    public const string Amount = "amount";
    public const string Percent = "percent";
    public const string Shares = "shares";

    public static string Label(string mode) => mode switch
    {
        Amount => "Beträge",
        Percent => "Prozent",
        Shares => "Anteile",
        _ => "Gleichmäßig"
    };
}

/// <summary>Wiederholungsintervalle für wiederkehrende Ausgaben.</summary>
public static class RepeatInterval
{
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public const string Yearly = "yearly";

    public static readonly IReadOnlyList<string> All = [Weekly, Monthly, Yearly];

    public static string Label(string interval) => interval switch
    {
        Weekly => "Wöchentlich",
        Yearly => "Jährlich",
        _ => "Monatlich"
    };

    public static DateTime Next(DateTime date, string interval) => interval switch
    {
        Weekly => date.AddDays(7),
        Yearly => date.AddYears(1),
        _ => date.AddMonths(1)
    };
}
