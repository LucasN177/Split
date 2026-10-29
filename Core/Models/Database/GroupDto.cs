using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("split_groups")]
public class GroupDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; } = string.Empty;

    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    [Column("created_by", ignoreOnUpdate: true)]
    public string? CreatedBy { get; set; }

    [Column("invite_code", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public string? InviteCode { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}
