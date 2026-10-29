using Microsoft.JSInterop;

namespace Split.Services;

/// <summary>Verfolgt den Online-Status des Browsers (navigator.onLine + online/offline-Events).</summary>
public class ConnectivityService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private DotNetObjectReference<ConnectivityService>? _reference;

    public bool IsOnline { get; private set; } = true;

    /// <summary>Wird ausgelöst, wenn sich der Status ändert. Parameter: neuer Online-Status.</summary>
    public event Func<bool, Task>? StatusChanged;

    public async Task Initialize()
    {
        try
        {
            _reference = DotNetObjectReference.Create(this);
            IsOnline = await jsRuntime.InvokeAsync<bool>("split.connectivity.register", _reference);
        }
        catch (JSException)
        {
            IsOnline = true;
        }
    }

    [JSInvokable]
    public async Task OnStatusChanged(bool isOnline)
    {
        if (IsOnline == isOnline) return;
        IsOnline = isOnline;
        if (StatusChanged is not null)
            await StatusChanged.Invoke(isOnline);
    }

    public ValueTask DisposeAsync()
    {
        _reference?.Dispose();
        return ValueTask.CompletedTask;
    }
}
