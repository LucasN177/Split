using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace Split.Infrastructure;

/// <summary>
/// Einfacher JSON-Speicher im localStorage des Browsers (Offline-Cache und Warteschlange).
/// Fehler (z.B. voller Speicher, privater Modus) werden ignoriert – der Cache ist optional.
/// </summary>
public class LocalStore(IJSRuntime jsRuntime)
{
    private const string Prefix = "split.";

    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private IJSInProcessRuntime Js => (IJSInProcessRuntime)jsRuntime;

    public T? Get<T>(string key)
    {
        try
        {
            var json = Js.Invoke<string?>("localStorage.getItem", Prefix + key);
            return string.IsNullOrEmpty(json) ? default : JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public void Set<T>(string key, T value)
    {
        try
        {
            Js.InvokeVoid("localStorage.setItem", Prefix + key, JsonConvert.SerializeObject(value, Settings));
        }
        catch (Exception)
        {
            // Cache ist optional
        }
    }

    public void Remove(string key)
    {
        try
        {
            Js.InvokeVoid("localStorage.removeItem", Prefix + key);
        }
        catch (Exception)
        {
            // ignorieren
        }
    }

    /// <summary>Entfernt alle Cache-Einträge (z.B. beim Abmelden). Die Session bleibt unberührt.</summary>
    public void ClearCache()
    {
        try
        {
            Js.InvokeVoid("split.clearStorage", Prefix + "cache.");
            Remove("outbox");
        }
        catch (Exception)
        {
            // ignorieren
        }
    }
}
