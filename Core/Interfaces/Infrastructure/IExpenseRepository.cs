using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Infrastructure;

public interface IExpenseRepository
{
    public Task<IResponse<List<ExpenseDto>>> GetExpenses(IReadOnlyCollection<string> groupIds);

    public Task<IResponse<List<ExpenseShareDto>>> GetShares(IReadOnlyCollection<string> expenseIds);

    /// <summary>Speichert Ausgabe und Aufteilung atomar (RPC split_save_expense). Gibt die ID zurück.</summary>
    public Task<IResponse<string>> SaveExpense(ExpenseDto expense, List<ExpenseShareDto> shares);

    public Task<IResponse> DeleteExpense(string expenseId);

    public Task<IResponse<List<SettlementDto>>> GetSettlements(IReadOnlyCollection<string> groupIds);

    public Task<IResponse<SettlementDto>> AddSettlement(SettlementDto settlement);

    public Task<IResponse> UpdateSettlement(SettlementDto settlement);

    public Task<IResponse> DeleteSettlement(string settlementId);

    public Task<IResponse<List<RecurringExpenseDto>>> GetRecurring(string groupId);

    public Task<IResponse> SaveRecurring(RecurringExpenseDto recurring);

    public Task<IResponse> DeleteRecurring(string recurringId);

    /// <summary>Erzeugt fällige wiederkehrende Ausgaben. Gibt die Anzahl neuer Ausgaben zurück.</summary>
    public Task<IResponse<int>> ProcessRecurring(string groupId);
}
