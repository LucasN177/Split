namespace Split.Core.Models.Balances;

/// <summary>Saldo eines Mitglieds: positiv = bekommt Geld, negativ = schuldet Geld.</summary>
public record MemberBalance(string MemberId, string DisplayName, decimal Paid, decimal Owed, decimal Balance);
