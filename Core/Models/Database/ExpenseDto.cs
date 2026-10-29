using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("expenses")]
public class ExpenseDto : BaseModel
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

    [Column("expense_date")]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Column("created_by", ignoreOnUpdate: true)]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}
