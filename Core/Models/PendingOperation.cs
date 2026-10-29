using Split.Core.Models.Database;

namespace Split.Core.Models;

/// <summary>Offline erfasste Änderung, die beim nächsten Online-Gang synchronisiert wird.</summary>
public class PendingOperation
{
    public const string ExpenseType = "expense";
    public const string SettlementType = "settlement";

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = ExpenseType;
    public string GroupId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ExpenseDto? Expense { get; set; }
    public List<ExpenseShareDto>? Shares { get; set; }
    public SettlementDto? Settlement { get; set; }

    /// <summary>Letzte Fehlermeldung, falls die Synchronisation fehlgeschlagen ist.</summary>
    public string? LastError { get; set; }
}
