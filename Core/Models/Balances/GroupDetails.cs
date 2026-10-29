using Split.Core.Models.Database;

namespace Split.Core.Models.Balances;

/// <summary>Alle Daten einer Gruppe, die für die Detailansicht benötigt werden.</summary>
public class GroupDetails
{
    public required GroupDto Group { get; init; }
    public List<GroupMemberDto> Members { get; init; } = [];
    public List<ExpenseDto> Expenses { get; init; } = [];
    public List<ExpenseShareDto> Shares { get; init; } = [];
    public List<SettlementDto> Settlements { get; init; } = [];
    public List<MemberBalance> Balances { get; init; } = [];
    public List<TransferSuggestion> Suggestions { get; init; } = [];

    public decimal TotalExpenses => Expenses.Sum(e => e.Amount);

    public string MemberName(string memberId)
        => Members.FirstOrDefault(m => m.Id == memberId)?.DisplayName ?? "Unbekannt";
}
