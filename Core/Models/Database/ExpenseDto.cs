using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Split.Core.Models.Database;

[Table("split_expenses")]
public class ExpenseDto : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Betrag in der Währung der Gruppe (Grundlage für alle Salden).</summary>
    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("paid_by")]
    public string PaidBy { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = "Sonstiges";

    [Column("expense_date")]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Column("note")]
    public string? Note { get; set; } = string.Empty;

    /// <summary>Pfad des Belegs im Storage-Bucket "split-receipts".</summary>
    [Column("receipt_path", NullValueHandling.Include)]
    public string? ReceiptPath { get; set; }

    /// <summary>Aufteilungsart, siehe <see cref="Enums.SplitMode"/>.</summary>
    [Column("split_mode")]
    public string SplitMode { get; set; } = Enums.SplitMode.Equal;

    /// <summary>Ursprünglicher Betrag, falls in Fremdwährung bezahlt.</summary>
    [Column("original_amount", NullValueHandling.Include)]
    public decimal? OriginalAmount { get; set; }

    [Column("original_currency", NullValueHandling.Include)]
    public string? OriginalCurrency { get; set; }

    [Column("exchange_rate")]
    public decimal ExchangeRate { get; set; } = 1;

    [Column("recurring_id", NullValueHandling.Include)]
    public string? RecurringId { get; set; }

    [Column("created_by", ignoreOnUpdate: true)]
    public string? CreatedBy { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public bool IsForeignCurrency => !string.IsNullOrEmpty(OriginalCurrency) && OriginalAmount is not null;
}
