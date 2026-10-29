using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace Split.Services;

/// <summary>
/// Live-Aktualisierungen über Supabase Realtime. Fehler beim Verbinden werden geschluckt –
/// die App funktioniert dann wie bisher, nur ohne Live-Updates.
/// </summary>
public class RealtimeService(Supabase.Client client)
{
    private static readonly string[] GroupTables = ["split_expenses", "split_settlements", "split_group_members", "split_recurring_expenses"];

    private bool _connected;

    /// <summary>
    /// Abonniert Änderungen einer Gruppe. <paramref name="onChange"/> wird (gebündelt) aufgerufen,
    /// wenn ein anderes Mitglied etwas ändert. Das Ergebnis beim Verlassen der Seite freigeben.
    /// </summary>
    public async Task<IAsyncDisposable?> SubscribeToGroup(string groupId, Func<Task> onChange)
    {
        try
        {
            if (!await EnsureConnected())
                return null;

            var channel = client.Realtime.Channel($"split-group-{groupId}");
            foreach (var table in GroupTables)
                channel.Register(new PostgresChangesOptions("public", table, ListenType.All, $"group_id=eq.{groupId}"));

            var debounce = new Debouncer(TimeSpan.FromMilliseconds(600), onChange);
            channel.AddPostgresChangeHandler(ListenType.All, (_, _) => debounce.Trigger());

            await channel.Subscribe();
            return new Subscription(client, channel, debounce);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Realtime nicht verfügbar: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> EnsureConnected()
    {
        var token = client.Auth.CurrentSession?.AccessToken;
        if (string.IsNullOrEmpty(token))
            return false;

        if (!_connected)
        {
            await client.Realtime.ConnectAsync();
            _connected = true;
        }

        // Aktuelles JWT setzen, damit Realtime die RLS-Policies des Benutzers anwendet
        client.Realtime.SetAuth(token);
        return true;
    }

    private sealed class Subscription(Supabase.Client client, RealtimeChannel channel, Debouncer debouncer) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            debouncer.Dispose();
            try
            {
                channel.Unsubscribe();
                client.Realtime.Remove(channel);
            }
            catch (Exception)
            {
                // Verbindung bereits geschlossen
            }
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Bündelt viele Änderungen (z.B. Ausgabe + Anteile) zu einem Neuladen.</summary>
    private sealed class Debouncer(TimeSpan delay, Func<Task> action) : IDisposable
    {
        private CancellationTokenSource? _cts;

        public void Trigger()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Delay(delay, token).ContinueWith(async t =>
            {
                if (!t.IsCanceled) await action();
            }, TaskScheduler.Default);
        }

        public void Dispose() => _cts?.Cancel();
    }
}
