using Split.Core.Models.Database;

namespace Split.Core.Models.Balances;

/// <summary>Saldo des angemeldeten Benutzers in einer Gruppe.</summary>
public record GroupSummary(GroupDto Group, int MemberCount, decimal TotalExpenses, decimal MyBalance);

/// <summary>Ausgabe inklusive Gruppen- und Zahlernamen für Listenansichten.</summary>
public record ExpenseListItem(ExpenseDto Expense, string GroupName, string PaidByName, string Currency);

public class DashboardOverview
{
    public List<GroupSummary> Groups { get; init; } = [];
    public List<ExpenseListItem> RecentExpenses { get; init; } = [];

    /// <summary>True, wenn die Daten aus dem Offline-Cache stammen.</summary>
    public bool IsFromCache { get; set; }

    /// <summary>Summe, die der Benutzer insgesamt zurückbekommt.</summary>
    public decimal TotalOwedToMe => Groups.Where(g => g.MyBalance > 0).Sum(g => g.MyBalance);

    /// <summary>Summe, die der Benutzer insgesamt schuldet.</summary>
    public decimal TotalIOwe => Groups.Where(g => g.MyBalance < 0).Sum(g => -g.MyBalance);
}
