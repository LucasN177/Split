using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("split_push_subscriptions")]
public class PushSubscriptionDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [Column("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    [Column("auth")]
    public string Auth { get; set; } = string.Empty;
}
