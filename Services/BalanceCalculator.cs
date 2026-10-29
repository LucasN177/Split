using Split.Core.Models.Balances;
using Split.Core.Models.Database;

namespace Split.Services;

/// <summary>Reine Rechenlogik für Aufteilungen, Salden und Ausgleichszahlungen.</summary>
public static class BalanceCalculator
{
    /// <summary>
    /// Teilt einen Betrag gleichmäßig auf. Rundungscents werden auf die ersten Mitglieder verteilt,
    /// sodass die Summe der Anteile immer exakt dem Betrag entspricht.
    /// </summary>
    public static Dictionary<string, decimal> SplitEvenly(decimal amount, IReadOnlyList<string> memberIds)
    {
        var result = new Dictionary<string, decimal>();
        if (memberIds.Count == 0)
            return result;

        var totalCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        var baseCents = totalCents / memberIds.Count;
        var remainder = totalCents % memberIds.Count;

        for (var i = 0; i < memberIds.Count; i++)
            result[memberIds[i]] = (baseCents + (i < remainder ? 1 : 0)) / 100m;

        return result;
    }

    /// <summary>
    /// Berechnet den Saldo je Mitglied. Positiv = bekommt Geld zurück, negativ = schuldet Geld.
    /// </summary>
    public static List<MemberBalance> CalculateBalances(
        IEnumerable<GroupMemberDto> members,
        IEnumerable<ExpenseDto> expenses,
        IEnumerable<ExpenseShareDto> shares,
        IEnumerable<SettlementDto> settlements)
    {
        var paid = new Dictionary<string, decimal>();
        var owed = new Dictionary<string, decimal>();
        var settled = new Dictionary<string, decimal>();

        foreach (var expense in expenses)
            paid[expense.PaidBy] = paid.GetValueOrDefault(expense.PaidBy) + expense.Amount;

        foreach (var share in shares)
            owed[share.MemberId] = owed.GetValueOrDefault(share.MemberId) + share.Amount;

        foreach (var settlement in settlements)
        {
            settled[settlement.FromMember] = settled.GetValueOrDefault(settlement.FromMember) + settlement.Amount;
            settled[settlement.ToMember] = settled.GetValueOrDefault(settlement.ToMember) - settlement.Amount;
        }

        return members
            .Select(m =>
            {
                var p = paid.GetValueOrDefault(m.Id);
                var o = owed.GetValueOrDefault(m.Id);
                var balance = p - o + settled.GetValueOrDefault(m.Id);
                return new MemberBalance(m.Id, m.DisplayName, p, o, Math.Round(balance, 2));
            })
            .OrderByDescending(b => b.Balance)
            .ToList();
    }

    /// <summary>
    /// Ermittelt mit einem Greedy-Verfahren eine möglichst kleine Anzahl an Überweisungen,
    /// die alle Salden ausgleicht.
    /// </summary>
    public static List<TransferSuggestion> SuggestTransfers(IEnumerable<MemberBalance> balances)
    {
        var creditors = balances.Where(b => b.Balance > 0.004m)
            .Select(b => (b.MemberId, b.DisplayName, Amount: b.Balance))
            .OrderByDescending(b => b.Amount)
            .ToList();
        var debtors = balances.Where(b => b.Balance < -0.004m)
            .Select(b => (b.MemberId, b.DisplayName, Amount: -b.Balance))
            .OrderByDescending(b => b.Amount)
            .ToList();

        var result = new List<TransferSuggestion>();
        int c = 0, d = 0;
        while (c < creditors.Count && d < debtors.Count)
        {
            var amount = Math.Min(creditors[c].Amount, debtors[d].Amount);
            if (amount >= 0.01m)
                result.Add(new TransferSuggestion(debtors[d].MemberId, debtors[d].DisplayName,
                    creditors[c].MemberId, creditors[c].DisplayName, Math.Round(amount, 2)));

            creditors[c] = creditors[c] with { Amount = creditors[c].Amount - amount };
            debtors[d] = debtors[d] with { Amount = debtors[d].Amount - amount };

            if (creditors[c].Amount < 0.01m) c++;
            if (debtors[d].Amount < 0.01m) d++;
        }

        return result;
    }
}
