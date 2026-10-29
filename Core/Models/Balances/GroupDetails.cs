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
    public List<RecurringExpenseDto> Recurring { get; init; } = [];
    public List<MemberBalance> Balances { get; init; } = [];
    public List<TransferSuggestion> Suggestions { get; init; } = [];

    /// <summary>ID des angemeldeten Benutzers (für Rechte-Prüfungen in der Oberfläche).</summary>
    public string? CurrentUserId { get; init; }

    /// <summary>True, wenn die Daten aus dem Offline-Cache stammen.</summary>
    public bool IsFromCache { get; set; }

    public DateTime LoadedAt { get; init; } = DateTime.Now;

    public decimal TotalExpenses => Expenses.Sum(e => e.Amount);

    public bool IsAdmin => CurrentUserId is not null && Group.CreatedBy == CurrentUserId;

    public GroupMemberDto? Me => Members.FirstOrDefault(m => m.UserId is not null && m.UserId == CurrentUserId);

    /// <summary>Löschen dürfen der Ersteller des Eintrags und der Admin der Gruppe.</summary>
    public bool CanDelete(string? createdBy) => IsAdmin || (createdBy is not null && createdBy == CurrentUserId);

    public string MemberName(string memberId)
        => Members.FirstOrDefault(m => m.Id == memberId)?.DisplayName ?? "Unbekannt";
}
