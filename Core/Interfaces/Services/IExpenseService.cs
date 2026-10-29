using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Services;

/// <summary>Ergebnis beim Speichern: ID der Ausgabe oder Hinweis, dass sie offline vorgemerkt wurde.</summary>
public record SaveResult(string? Id, bool Queued);

public interface IExpenseService
{
    /// <summary>
    /// Speichert eine Ausgabe. <paramref name="inputs"/> enthält je teilnehmendem Mitglied den eingegebenen Wert
    /// passend zu <see cref="ExpenseDto.SplitMode"/> (Betrag, Prozent, Anteile; bei "equal" ignoriert).
    /// Ohne Verbindung wird eine neue Ausgabe für die spätere Synchronisation vorgemerkt.
    /// </summary>
    public Task<IResponse<SaveResult>> SaveExpense(ExpenseDto expense, IReadOnlyList<KeyValuePair<string, decimal>> inputs);

    public Task<IResponse> DeleteExpense(ExpenseDto expense);

    public Task<IResponse<SaveResult>> SaveSettlement(SettlementDto settlement);

    public Task<IResponse> DeleteSettlement(string settlementId);

    public Task<IResponse> SaveRecurring(RecurringExpenseDto recurring, IReadOnlyList<KeyValuePair<string, decimal>> inputs);

    public Task<IResponse> SetRecurringActive(RecurringExpenseDto recurring, bool active);

    public Task<IResponse> DeleteRecurring(string recurringId);

    public Task<IResponse<string>> UploadReceipt(string groupId, byte[] content, string fileName, string contentType);

    public Task<IResponse<string>> GetReceiptUrl(string path);

    /// <summary>Aktueller Wechselkurs: 1 <paramref name="from"/> = x <paramref name="to"/>.</summary>
    public Task<IResponse<decimal>> GetExchangeRate(string from, string to);
}
