using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("split_recurring_expenses")]
public class RecurringExpenseDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("paid_by")]
    public string PaidBy { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = "Sonstiges";

    [Column("split_mode")]
    public string SplitMode { get; set; } = Enums.SplitMode.Equal;

    [Column("shares")]
    public List<RecurringShare> Shares { get; set; } = [];

    [Column("repeat_interval")]
    public string RepeatInterval { get; set; } = Enums.RepeatInterval.Monthly;

    [Column("next_date")]
    public DateTime NextDate { get; set; } = DateTime.Today;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_by", ignoreOnUpdate: true)]
    public string? CreatedBy { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}

public class RecurringShare
{
    [JsonProperty("member_id")]
    public string MemberId { get; set; } = string.Empty;

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("weight")]
    public decimal? Weight { get; set; }
}
