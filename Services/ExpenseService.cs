using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Interfaces.Services;
using Split.Core.Models;
using Split.Core.Models.Database;

namespace Split.Services;

public class ExpenseService(IExpenseRepository expenseRepository) : IExpenseService
{
    public async Task<IResponse<ExpenseDto>> SaveExpense(ExpenseDto expense, IReadOnlyList<string> participantIds)
    {
        if (participantIds.Count == 0)
            return Response<ExpenseDto>.Failure("Bitte mindestens ein Mitglied auswählen.");

        return await SaveExpense(expense, BalanceCalculator.SplitEvenly(expense.Amount, participantIds));
    }

    public async Task<IResponse<ExpenseDto>> SaveExpense(ExpenseDto expense, IReadOnlyDictionary<string, decimal> shares)
    {
        if (string.IsNullOrWhiteSpace(expense.Title))
            return Response<ExpenseDto>.Failure("Bitte einen Titel angeben.");
        if (expense.Amount <= 0)
            return Response<ExpenseDto>.Failure("Der Betrag muss größer als 0 sein.");
        if (string.IsNullOrEmpty(expense.PaidBy))
            return Response<ExpenseDto>.Failure("Bitte angeben, wer bezahlt hat.");

        var relevantShares = shares.Where(s => s.Value > 0).ToList();
        if (relevantShares.Count == 0)
            return Response<ExpenseDto>.Failure("Bitte mindestens ein Mitglied auswählen.");
        if (relevantShares.Sum(s => s.Value) != expense.Amount)
            return Response<ExpenseDto>.Failure("Die Summe der Anteile muss dem Gesamtbetrag entsprechen.");

        expense.Title = expense.Title.Trim();
        var shareDtos = relevantShares
            .Select(s => new ExpenseShareDto { MemberId = s.Key, Amount = s.Value })
            .ToList();

        return await expenseRepository.SaveExpense(expense, shareDtos);
    }

    public async Task<IResponse> DeleteExpense(string expenseId)
        => await expenseRepository.DeleteExpense(expenseId);

    public async Task<IResponse<SettlementDto>> AddSettlement(SettlementDto settlement)
    {
        if (settlement.Amount <= 0)
            return Response<SettlementDto>.Failure("Der Betrag muss größer als 0 sein.");
        if (settlement.FromMember == settlement.ToMember)
            return Response<SettlementDto>.Failure("Absender und Empfänger müssen unterschiedlich sein.");

        return await expenseRepository.AddSettlement(settlement);
    }

    public async Task<IResponse> DeleteSettlement(string settlementId)
        => await expenseRepository.DeleteSettlement(settlementId);
}
