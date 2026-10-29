using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("settlements")]
public class SettlementDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [Column("from_member")]
    public string FromMember { get; set; } = string.Empty;

    [Column("to_member")]
    public string ToMember { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("settled_at")]
    public DateTime SettledAt { get; set; } = DateTime.Today;

    [Column("created_by", ignoreOnUpdate: true)]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}
