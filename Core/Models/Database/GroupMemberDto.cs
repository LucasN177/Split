using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("group_members")]
public class GroupMemberDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Verknüpfter Benutzer – null bei Mitgliedern ohne eigenes Konto.</summary>
    [Column("user_id", NullValueHandling.Include)]
    public string? UserId { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}
