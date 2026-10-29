namespace Split.Core.Models.Balances;

/// <summary>Vorgeschlagene Ausgleichszahlung von <see cref="FromMemberId"/> an <see cref="ToMemberId"/>.</summary>
public record TransferSuggestion(string FromMemberId, string FromName, string ToMemberId, string ToName, decimal Amount);
