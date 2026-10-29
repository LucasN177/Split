using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("split_expense_shares")]
public class ExpenseShareDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("expense_id")]
    public string ExpenseId { get; set; } = string.Empty;

    [Column("member_id")]
    public string MemberId { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }
}
