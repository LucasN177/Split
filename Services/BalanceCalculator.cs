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
    /// Teilt einen Betrag nach Gewichten auf (Prozent oder Anteile) – Verfahren der größten Reste,
    /// sodass die Summe der Anteile exakt dem Betrag entspricht.
    /// </summary>
    public static Dictionary<string, decimal> SplitByWeights(decimal amount, IReadOnlyList<KeyValuePair<string, decimal>> weights)
    {
        var result = new Dictionary<string, decimal>();
        var relevant = weights.Where(w => w.Value > 0).ToList();
        var totalWeight = relevant.Sum(w => w.Value);
        if (relevant.Count == 0 || totalWeight <= 0)
            return result;

        var totalCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        var exact = relevant.Select(w => (w.Key, Cents: totalCents * w.Value / totalWeight)).ToList();
        var floored = exact.Select(e => (e.Key, Cents: (long)Math.Floor(e.Cents), Remainder: e.Cents - Math.Floor(e.Cents))).ToList();
        var missing = totalCents - floored.Sum(f => f.Cents);

        // Fehlende Cents an die Mitglieder mit den größten Nachkommaresten verteilen (stabil nach Reihenfolge)
        var bonus = floored
            .Select((f, index) => (f.Key, f.Remainder, index))
            .OrderByDescending(f => f.Remainder)
            .ThenBy(f => f.index)
            .Take((int)missing)
            .Select(f => f.Key)
            .ToHashSet();

        foreach (var f in floored)
            result[f.Key] = (f.Cents + (bonus.Contains(f.Key) ? 1 : 0)) / 100m;

        return result;
    }

    /// <summary>
    /// Berechnet die Anteile einer Ausgabe je nach Aufteilungsart.
    /// <paramref name="inputs"/> enthält je teilnehmendem Mitglied den eingegebenen Wert
    /// (Betrag, Prozent oder Anzahl Anteile; bei gleichmäßiger Aufteilung ignoriert).
    /// </summary>
    public static (List<ExpenseShareDto> Shares, string? Error) CalculateShares(
        decimal amount, string mode, IReadOnlyList<KeyValuePair<string, decimal>> inputs)
    {
        var participants = mode == Core.Models.Enums.SplitMode.Equal
            ? inputs
            : inputs.Where(i => i.Value > 0).ToList();

        if (participants.Count == 0)
            return ([], "Bitte mindestens ein Mitglied auswählen.");

        switch (mode)
        {
            case Core.Models.Enums.SplitMode.Amount:
                if (participants.Sum(p => p.Value) != amount)
                    return ([], "Die Summe der Beträge muss dem Gesamtbetrag entsprechen.");
                return (participants.Select(p => new ExpenseShareDto { MemberId = p.Key, Amount = Math.Round(p.Value, 2) }).ToList(), null);

            case Core.Models.Enums.SplitMode.Percent:
                if (participants.Sum(p => p.Value) != 100)
                    return ([], "Die Prozentwerte müssen zusammen 100 % ergeben.");
                return (ToShares(SplitByWeights(amount, participants), participants), null);

            case Core.Models.Enums.SplitMode.Shares:
                return (ToShares(SplitByWeights(amount, participants), participants), null);

            default:
                var even = SplitEvenly(amount, participants.Select(p => p.Key).ToList());
                return (even.Select(e => new ExpenseShareDto { MemberId = e.Key, Amount = e.Value }).ToList(), null);
        }

        static List<ExpenseShareDto> ToShares(Dictionary<string, decimal> amounts, IEnumerable<KeyValuePair<string, decimal>> weights)
            => weights.Select(w => new ExpenseShareDto { MemberId = w.Key, Amount = amounts.GetValueOrDefault(w.Key), Weight = w.Value }).ToList();
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
