using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Interfaces.Services;
using Split.Core.Models;
using Split.Core.Models.Database;

namespace Split.Services;

public class ExpenseService(
    IExpenseRepository expenseRepository,
    IReceiptRepository receiptRepository,
    OfflineSyncService offlineSync,
    ConnectivityService connectivity) : IExpenseService
{
    private const string ExchangeRateApi = "https://api.frankfurter.app/latest";
    private static readonly HttpClient httpClient = new();

    public async Task<IResponse<SaveResult>> SaveExpense(ExpenseDto expense, IReadOnlyList<KeyValuePair<string, decimal>> inputs)
    {
        if (string.IsNullOrWhiteSpace(expense.Title))
            return Response<SaveResult>.Failure("Bitte einen Titel angeben.");
        if (expense.Amount <= 0)
            return Response<SaveResult>.Failure("Der Betrag muss größer als 0 sein.");
        if (string.IsNullOrEmpty(expense.PaidBy))
            return Response<SaveResult>.Failure("Bitte angeben, wer bezahlt hat.");

        var (shares, error) = BalanceCalculator.CalculateShares(expense.Amount, expense.SplitMode, inputs);
        if (error is not null)
            return Response<SaveResult>.Failure(error);

        expense.Title = expense.Title.Trim();
        expense.Note = expense.Note?.Trim() ?? string.Empty;

        if (!connectivity.IsOnline)
        {
            if (!string.IsNullOrEmpty(expense.Id))
                return Response<SaveResult>.Failure("Bearbeiten ist offline nicht möglich.");

            offlineSync.Enqueue(new PendingOperation
            {
                Type = PendingOperation.ExpenseType,
                GroupId = expense.GroupId,
                Description = $"{expense.Title} ({expense.Amount:N2})",
                Expense = expense,
                Shares = shares
            });
            return Response<SaveResult>.Success(new SaveResult(null, true));
        }

        var result = await expenseRepository.SaveExpense(expense, shares);
        return result.IsSuccess
            ? Response<SaveResult>.Success(new SaveResult(result.Data, false))
            : Response<SaveResult>.Failure(result.ErrorMessage ?? "Ausgabe konnte nicht gespeichert werden");
    }

    public async Task<IResponse> DeleteExpense(ExpenseDto expense)
    {
        var result = await expenseRepository.DeleteExpense(expense.Id);
        if (result.IsSuccess && !string.IsNullOrEmpty(expense.ReceiptPath))
            await receiptRepository.Delete(expense.ReceiptPath);
        return result;
    }

    public async Task<IResponse<SaveResult>> SaveSettlement(SettlementDto settlement)
    {
        if (settlement.Amount <= 0)
            return Response<SaveResult>.Failure("Der Betrag muss größer als 0 sein.");
        if (settlement.FromMember == settlement.ToMember)
            return Response<SaveResult>.Failure("Absender und Empfänger müssen unterschiedlich sein.");

        settlement.Note = settlement.Note.Trim();

        if (!string.IsNullOrEmpty(settlement.Id))
        {
            var update = await expenseRepository.UpdateSettlement(settlement);
            return update.IsSuccess
                ? Response<SaveResult>.Success(new SaveResult(settlement.Id, false))
                : Response<SaveResult>.Failure(update.ErrorMessage ?? "Zahlung konnte nicht gespeichert werden");
        }

        if (!connectivity.IsOnline)
        {
            offlineSync.Enqueue(new PendingOperation
            {
                Type = PendingOperation.SettlementType,
                GroupId = settlement.GroupId,
                Description = $"Zahlung ({settlement.Amount:N2})",
                Settlement = settlement
            });
            return Response<SaveResult>.Success(new SaveResult(null, true));
        }

        var result = await expenseRepository.AddSettlement(settlement);
        return result.IsSuccess
            ? Response<SaveResult>.Success(new SaveResult(result.Data?.Id, false))
            : Response<SaveResult>.Failure(result.ErrorMessage ?? "Zahlung konnte nicht gespeichert werden");
    }

    public async Task<IResponse> DeleteSettlement(string settlementId)
        => await expenseRepository.DeleteSettlement(settlementId);

    public async Task<IResponse> SaveRecurring(RecurringExpenseDto recurring, IReadOnlyList<KeyValuePair<string, decimal>> inputs)
    {
        if (string.IsNullOrWhiteSpace(recurring.Title))
            return Response.Failure("Bitte einen Titel angeben.", null);
        if (recurring.Amount <= 0)
            return Response.Failure("Der Betrag muss größer als 0 sein.", null);

        var (shares, error) = BalanceCalculator.CalculateShares(recurring.Amount, recurring.SplitMode, inputs);
        if (error is not null)
            return Response.Failure(error, null);

        recurring.Title = recurring.Title.Trim();
        recurring.Shares = shares.Select(s => new RecurringShare { MemberId = s.MemberId, Amount = s.Amount, Weight = s.Weight }).ToList();
        return await expenseRepository.SaveRecurring(recurring);
    }

    public async Task<IResponse> SetRecurringActive(RecurringExpenseDto recurring, bool active)
    {
        recurring.Active = active;
        // Beim Reaktivieren keine Ausgaben für die pausierte Zeit nachholen
        if (active && recurring.NextDate < DateTime.Today)
            recurring.NextDate = DateTime.Today;
        return await expenseRepository.SaveRecurring(recurring);
    }

    public async Task<IResponse> DeleteRecurring(string recurringId)
        => await expenseRepository.DeleteRecurring(recurringId);

    public async Task<IResponse<string>> UploadReceipt(string groupId, byte[] content, string fileName, string contentType)
    {
        if (!connectivity.IsOnline)
            return Response<string>.Failure("Belege können nur online hochgeladen werden.");
        return await receiptRepository.Upload(groupId, content, fileName, contentType);
    }

    public async Task<IResponse<string>> GetReceiptUrl(string path)
        => await receiptRepository.GetUrl(path);

    public async Task<IResponse<decimal>> GetExchangeRate(string from, string to)
    {
        if (from == to)
            return Response<decimal>.Success(1);

        try
        {
            var result = await httpClient.GetFromJsonAsync<FrankfurterResponse>($"{ExchangeRateApi}?from={from}&to={to}");
            return result?.Rates is not null && result.Rates.TryGetValue(to, out var rate)
                ? Response<decimal>.Success(rate)
                : Response<decimal>.Failure("Kein Wechselkurs verfügbar");
        }
        catch (Exception ex)
        {
            return Response<decimal>.Failure(ex);
        }
    }

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
