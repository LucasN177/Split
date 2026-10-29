using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Models;
using Split.Core.Models.Database;

namespace Split.Services;

/// <summary>
/// Web-Push-Benachrichtigungen. Das Gerät wird im Browser abonniert und das Abo in Supabase gespeichert;
/// der Versand erfolgt durch die Edge Function "split-notify" (siehe supabase/functions).
/// </summary>
public class PushService(IJSRuntime jsRuntime, IPushRepository pushRepository, IConfiguration configuration)
{
    private string? VapidPublicKey => configuration["Push:VapidPublicKey"];

    /// <summary>Push wird vom Browser unterstützt und ist in der App konfiguriert.</summary>
    public async Task<bool> IsAvailable()
    {
        if (string.IsNullOrWhiteSpace(VapidPublicKey) || VapidPublicKey.StartsWith("PUSH_"))
            return false;
        try
        {
            return await jsRuntime.InvokeAsync<bool>("split.push.isSupported");
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task<bool> IsSubscribed()
    {
        try
        {
            return await jsRuntime.InvokeAsync<PushSubscriptionInfo?>("split.push.getSubscription") is not null;
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task<IResponse> Subscribe()
    {
        try
        {
            var info = await jsRuntime.InvokeAsync<PushSubscriptionInfo?>("split.push.subscribe", VapidPublicKey);
            if (info is null)
                return Response.Failure("Benachrichtigungen wurden im Browser nicht erlaubt.", null);

            return await pushRepository.SaveSubscription(new PushSubscriptionDto
            {
                Endpoint = info.Endpoint,
                P256dh = info.P256dh,
                Auth = info.Auth
            });
        }
        catch (JSException ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> Unsubscribe()
    {
        try
        {
            var endpoint = await jsRuntime.InvokeAsync<string?>("split.push.unsubscribe");
            return endpoint is null ? Response.Success() : await pushRepository.DeleteSubscription(endpoint);
        }
        catch (JSException ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    private sealed class PushSubscriptionInfo
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}
