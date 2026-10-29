using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Models;
using Split.Core.Models.Database;
using Supabase.Postgrest;

namespace Split.Infrastructure;

public class PushRepository(Supabase.Client client) : IPushRepository
{
    public async Task<IResponse> SaveSubscription(PushSubscriptionDto subscription)
    {
        try
        {
            subscription.UserId = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
            await client.From<PushSubscriptionDto>().OnConflict("endpoint").Upsert(subscription, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> DeleteSubscription(string endpoint)
    {
        try
        {
            await client.From<PushSubscriptionDto>().Where(x => x.Endpoint == endpoint).Delete();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }
}
