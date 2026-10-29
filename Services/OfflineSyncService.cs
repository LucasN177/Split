using Split.Core.Interfaces.Infrastructure;
using Split.Core.Models;
using Split.Infrastructure;

namespace Split.Services;

/// <summary>
/// Warteschlange für offline erfasste Ausgaben und Zahlungen.
/// Sobald wieder eine Verbindung besteht, werden sie in Erfassungsreihenfolge gespeichert.
/// </summary>
public class OfflineSyncService
{
    private const string OutboxKey = "outbox";

    private readonly LocalStore _store;
    private readonly IExpenseRepository _expenseRepository;
    private readonly ConnectivityService _connectivity;
    private bool _syncing;

    public OfflineSyncService(LocalStore store, IExpenseRepository expenseRepository, ConnectivityService connectivity)
    {
        _store = store;
        _expenseRepository = expenseRepository;
        _connectivity = connectivity;
        _connectivity.StatusChanged += async online =>
        {
            if (online) await Sync();
        };
    }

    public IReadOnlyList<PendingOperation> Pending => _store.Get<List<PendingOperation>>(OutboxKey) ?? [];

    /// <summary>Wird ausgelöst, wenn sich die Warteschlange ändert oder synchronisiert wurde. Parameter: Anzahl synchronisierter Einträge.</summary>
    public event Action<int>? Changed;

    public void Enqueue(PendingOperation operation)
    {
        var outbox = Pending.ToList();
        outbox.Add(operation);
        _store.Set(OutboxKey, outbox);
        Changed?.Invoke(0);
    }

    public void Discard(string operationId)
    {
        _store.Set(OutboxKey, Pending.Where(o => o.Id != operationId).ToList());
        Changed?.Invoke(0);
    }

    /// <summary>Überträgt alle wartenden Änderungen. Gibt die Anzahl erfolgreich übertragener Einträge zurück.</summary>
    public async Task<int> Sync()
    {
        if (_syncing || !_connectivity.IsOnline) return 0;
        var outbox = Pending.ToList();
        if (outbox.Count == 0) return 0;

        _syncing = true;
        var synced = 0;
        try
        {
            foreach (var operation in outbox.ToList())
            {
                string? error = null;
                switch (operation.Type)
                {
                    case PendingOperation.ExpenseType when operation.Expense is not null:
                        var expenseResult = await _expenseRepository.SaveExpense(operation.Expense, operation.Shares ?? []);
                        error = expenseResult.IsSuccess ? null : expenseResult.ErrorMessage;
                        break;
                    case PendingOperation.SettlementType when operation.Settlement is not null:
                        var settlementResult = await _expenseRepository.AddSettlement(operation.Settlement);
                        error = settlementResult.IsSuccess ? null : settlementResult.ErrorMessage;
                        break;
                }

                if (error is null)
                {
                    outbox.Remove(operation);
                    synced++;
                }
                else
                {
                    operation.LastError = error;
                }
            }

            _store.Set(OutboxKey, outbox);
        }
        finally
        {
            _syncing = false;
        }

        Changed?.Invoke(synced);
        return synced;
    }
}
