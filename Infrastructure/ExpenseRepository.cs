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

    public async Task<IResponse<string>> SaveExpense(ExpenseDto expense, List<ExpenseShareDto> shares)
    {
        try
        {
            // Ausgabe und Aufteilung in einer Datenbank-Transaktion speichern
            var parameters = new Dictionary<string, object>
            {
                ["p_expense"] = new Dictionary<string, object?>
                {
                    ["id"] = expense.Id,
                    ["group_id"] = expense.GroupId,
                    ["title"] = expense.Title,
                    ["amount"] = expense.Amount,
                    ["paid_by"] = expense.PaidBy,
                    ["category"] = expense.Category,
                    ["expense_date"] = expense.ExpenseDate.ToString("yyyy-MM-dd"),
                    ["note"] = expense.Note ?? string.Empty,
                    ["receipt_path"] = expense.ReceiptPath,
                    ["split_mode"] = expense.SplitMode,
                    ["original_amount"] = expense.OriginalAmount,
                    ["original_currency"] = expense.OriginalCurrency,
                    ["exchange_rate"] = expense.ExchangeRate
                },
                ["p_shares"] = shares.Select(s => new Dictionary<string, object?>
                {
                    ["member_id"] = s.MemberId,
                    ["amount"] = s.Amount,
                    ["weight"] = s.Weight
                }).ToList()
            };

            var id = await client.Rpc<string>("split_save_expense", parameters);
            return string.IsNullOrEmpty(id)
                ? Response<string>.Failure("Ausgabe konnte nicht gespeichert werden")
                : Response<string>.Success(id);
        }
        catch (Exception ex)
        {
            return Response<string>.Failure(ex);
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

    public async Task<IResponse> UpdateSettlement(SettlementDto settlement)
    {
        try
        {
            await client.From<SettlementDto>()
                .Where(x => x.Id == settlement.Id)
                .Set(x => x.FromMember, settlement.FromMember)
                .Set(x => x.ToMember, settlement.ToMember)
                .Set(x => x.Amount, settlement.Amount)
                .Set(x => x.Note, settlement.Note)
                .Set(x => x.SettledAt, settlement.SettledAt)
                .Update();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
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

    public async Task<IResponse<List<RecurringExpenseDto>>> GetRecurring(string groupId)
    {
        try
        {
            var result = await client.From<RecurringExpenseDto>()
                .Where(x => x.GroupId == groupId)
                .Order(x => x.NextDate, Ordering.Ascending)
                .Get();
            return Response<List<RecurringExpenseDto>>.Success(result.Models);
        }
        catch (Exception ex)
        {
            return Response<List<RecurringExpenseDto>>.Failure(ex);
        }
    }

    public async Task<IResponse> SaveRecurring(RecurringExpenseDto recurring)
    {
        try
        {
            if (string.IsNullOrEmpty(recurring.Id))
            {
                recurring.CreatedBy = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
                await client.From<RecurringExpenseDto>().Insert(recurring);
            }
            else
            {
                await client.From<RecurringExpenseDto>().Update(recurring);
            }
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> DeleteRecurring(string recurringId)
    {
        try
        {
            await client.From<RecurringExpenseDto>().Where(x => x.Id == recurringId).Delete();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<int>> ProcessRecurring(string groupId)
    {
        try
        {
            var count = await client.Rpc<int>("split_process_recurring", new Dictionary<string, object> { ["p_group_id"] = groupId });
            return Response<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Response<int>.Failure(ex);
        }
    }
}
