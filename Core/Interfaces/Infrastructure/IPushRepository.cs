using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Infrastructure;

public interface IPushRepository
{
    public Task<IResponse> SaveSubscription(PushSubscriptionDto subscription);

    public Task<IResponse> DeleteSubscription(string endpoint);
}
