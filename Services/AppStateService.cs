using Microsoft.JSInterop;

namespace Split.Services;

public class AppStateService(IJSRuntime jsRuntime)
{
    private const string DarkModeKey = "split.darkmode";

    public bool IsDarkMode { get; private set; }

    public bool IsDrawerOpen { get; set; } = true;

    /// <summary>True, solange die App beim Start Session und Einstellungen lädt.</summary>
    public bool IsInitializing { get; private set; } = true;

    public event Action? OnChange;

    /// <summary>Lädt gespeicherte Einstellungen. Ohne gespeicherten Wert wird die Systemeinstellung verwendet.</summary>
    public async Task Initialize()
    {
        try
        {
            var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", DarkModeKey);
            IsDarkMode = stored is not null
                ? stored == "true"
                : await jsRuntime.InvokeAsync<bool>("split.prefersDarkMode");
        }
        catch (JSException)
        {
            IsDarkMode = false;
        }
    }

    public void FinishInitialization()
    {
        IsInitializing = false;
        NotifyStateChanged();
    }

    public async Task ToggleDarkMode()
    {
        await SetDarkMode(!IsDarkMode);
    }

    public async Task SetDarkMode(bool value)
    {
        IsDarkMode = value;
        NotifyStateChanged();
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", DarkModeKey, value ? "true" : "false");
        }
        catch (JSException)
        {
            // localStorage nicht verfügbar (z.B. privater Modus) – Einstellung gilt nur für diese Sitzung
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void DrawerToggle()
    {
        IsDrawerOpen = !IsDrawerOpen;
        NotifyStateChanged();
    }
}
