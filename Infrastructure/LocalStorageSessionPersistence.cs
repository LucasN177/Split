using Microsoft.JSInterop;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Split.Infrastructure;

/// <summary>
/// Speichert die Supabase-Session im localStorage des Browsers,
/// damit Benutzer nach einem Reload angemeldet bleiben.
/// </summary>
public class LocalStorageSessionPersistence(IJSRuntime jsRuntime) : IGotrueSessionPersistence<Session>
{
    private const string StorageKey = "split.supabase.session";

    private IJSInProcessRuntime Js => (IJSInProcessRuntime)jsRuntime;

    public void SaveSession(Session session)
        => Js.InvokeVoid("localStorage.setItem", StorageKey, JsonConvert.SerializeObject(session));

    public void DestroySession()
        => Js.InvokeVoid("localStorage.removeItem", StorageKey);

    public Session? LoadSession()
    {
        var json = Js.Invoke<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<Session>(json);
        }
        catch (JsonException)
        {
            DestroySession();
            return null;
        }
    }
}
