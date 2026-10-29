using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Models;
using Split.Core.Models.Database;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace Split.Infrastructure;

public class ExpenseRepository(Supabase.Client client) : IExpenseRepository
{
    private static readonly QueryOptions ReturnRepresentation = new() { Returning = QueryOptions.ReturnType.Representation };

    public async Task<IResponse<List<ExpenseDto>>> GetExpenses(IReadOnlyCollection<string> groupIds)
    {
        if (groupIds.Count == 0)
            return Response<List<ExpenseDto>>.Success([]);

        try
        {
            var result = await client.From<ExpenseDto>()
                .Filter("group_id", Operator.In, groupIds.ToList())
                .Order(x => x.ExpenseDate, Ordering.Descending)
                .Order(x => x.CreatedAt, Ordering.Descending)
                .Get();
            return Response<List<ExpenseDto>>.Success(result.Models);
        }
        catch (Exception ex)
        {
            return Response<List<ExpenseDto>>.Failure(ex);
        }
    }

    public async Task<IResponse<List<ExpenseShareDto>>> GetShares(IReadOnlyCollection<string> expenseIds)
    {
        if (expenseIds.Count == 0)
            return Response<List<ExpenseShareDto>>.Success([]);

        try
        {
            var result = await client.From<ExpenseShareDto>()
                .Filter("expense_id", Operator.In, expenseIds.ToList())
                .Get();
            return Response<List<ExpenseShareDto>>.Success(result.Models);
        }
        catch (Exception ex)
        {
            return Response<List<ExpenseShareDto>>.Failure(ex);
        }
    }

    public async Task<IResponse<ExpenseDto>> SaveExpense(ExpenseDto expense, List<ExpenseShareDto> shares)
    {
        try
        {
            ExpenseDto? saved;
            if (string.IsNullOrEmpty(expense.Id))
            {
                expense.CreatedBy = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
                saved = (await client.From<ExpenseDto>().Insert(expense, ReturnRepresentation)).Model;
            }
            else
            {
                saved = (await client.From<ExpenseDto>()
                    .Where(x => x.Id == expense.Id)
                    .Set(x => x.Title, expense.Title)
                    .Set(x => x.Amount, expense.Amount)
                    .Set(x => x.PaidBy, expense.PaidBy)
                    .Set(x => x.Category, expense.Category)
                    .Set(x => x.ExpenseDate, expense.ExpenseDate)
                    .Update(ReturnRepresentation)).Model;

                // Aufteilung komplett neu schreiben
                await client.From<ExpenseShareDto>().Where(x => x.ExpenseId == expense.Id).Delete();
            }

            if (saved is null)
                return Response<ExpenseDto>.Failure("Ausgabe konnte nicht gespeichert werden");

            foreach (var share in shares)
                share.ExpenseId = saved.Id;

            if (shares.Count > 0)
                await client.From<ExpenseShareDto>().Insert(shares);

            return Response<ExpenseDto>.Success(saved);
        }
        catch (Exception ex)
        {
            return Response<ExpenseDto>.Failure(ex);
        }
    }

    public async Task<IResponse> DeleteExpense(string expenseId)
    {
        try
        {
            await client.From<ExpenseDto>().Where(x => x.Id == expenseId).Delete();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<List<SettlementDto>>> GetSettlements(IReadOnlyCollection<string> groupIds)
    {
        if (groupIds.Count == 0)
            return Response<List<SettlementDto>>.Success([]);

        try
        {
            var result = await client.From<SettlementDto>()
                .Filter("group_id", Operator.In, groupIds.ToList())
                .Order(x => x.SettledAt, Ordering.Descending)
                .Get();
            return Response<List<SettlementDto>>.Success(result.Models);
        }
        catch (Exception ex)
        {
            return Response<List<SettlementDto>>.Failure(ex);
        }
    }

    public async Task<IResponse<SettlementDto>> AddSettlement(SettlementDto settlement)
    {
        try
        {
            settlement.CreatedBy = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
            var result = await client.From<SettlementDto>().Insert(settlement, ReturnRepresentation);
            return result.Model is not null ? Response<SettlementDto>.Success(result.Model) : Response<SettlementDto>.Failure("Zahlung konnte nicht gespeichert werden");
        }
        catch (Exception ex)
        {
            return Response<SettlementDto>.Failure(ex);
        }
    }

    public async Task<IResponse> DeleteSettlement(string settlementId)
    {
        try
        {
            await client.From<SettlementDto>().Where(x => x.Id == settlementId).Delete();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }
}
