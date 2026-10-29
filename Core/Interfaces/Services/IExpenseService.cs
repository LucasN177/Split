using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Services;

public interface IExpenseService
{
    /// <summary>Speichert eine Ausgabe und verteilt sie gleichmäßig auf die angegebenen Mitglieder.</summary>
    public Task<IResponse<ExpenseDto>> SaveExpense(ExpenseDto expense, IReadOnlyList<string> participantIds);

    /// <summary>Speichert eine Ausgabe mit individuellen Beträgen je Mitglied.</summary>
    public Task<IResponse<ExpenseDto>> SaveExpense(ExpenseDto expense, IReadOnlyDictionary<string, decimal> shares);

    public Task<IResponse> DeleteExpense(string expenseId);

    public Task<IResponse<SettlementDto>> AddSettlement(SettlementDto settlement);

    public Task<IResponse> DeleteSettlement(string settlementId);
}
