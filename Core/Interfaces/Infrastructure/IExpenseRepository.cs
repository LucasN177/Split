using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Infrastructure;

public interface IExpenseRepository
{
    public Task<IResponse<List<ExpenseDto>>> GetExpenses(IReadOnlyCollection<string> groupIds);

    public Task<IResponse<List<ExpenseShareDto>>> GetShares(IReadOnlyCollection<string> expenseIds);

    public Task<IResponse<ExpenseDto>> SaveExpense(ExpenseDto expense, List<ExpenseShareDto> shares);

    public Task<IResponse> DeleteExpense(string expenseId);

    public Task<IResponse<List<SettlementDto>>> GetSettlements(IReadOnlyCollection<string> groupIds);

    public Task<IResponse<SettlementDto>> AddSettlement(SettlementDto settlement);

    public Task<IResponse> DeleteSettlement(string settlementId);
}
