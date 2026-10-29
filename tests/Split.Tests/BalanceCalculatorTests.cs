using Split.Core.Models.Database;
using Split.Core.Models.Enums;
using Split.Services;

namespace Split.Tests;

public class BalanceCalculatorTests
{
    private static KeyValuePair<string, decimal> Pair(string key, decimal value) => new(key, value);

    private static GroupMemberDto Member(string id) => new() { Id = id, DisplayName = id.ToUpperInvariant() };

    // ------------------------------------------------------------------ SplitEvenly

    [Fact]
    public void SplitEvenly_DistributesRemainderCentsToFirstMembers()
    {
        var result = BalanceCalculator.SplitEvenly(10m, ["a", "b", "c"]);

        Assert.Equal(3.34m, result["a"]);
        Assert.Equal(3.33m, result["b"]);
        Assert.Equal(3.33m, result["c"]);
        Assert.Equal(10m, result.Values.Sum());
    }

    [Fact]
    public void SplitEvenly_WithoutMembers_ReturnsEmpty()
        => Assert.Empty(BalanceCalculator.SplitEvenly(10m, []));

    [Theory]
    [InlineData(0.01, 3)]
    [InlineData(99.99, 7)]
    [InlineData(1234.56, 9)]
    public void SplitEvenly_SumAlwaysMatchesAmount(double amount, int members)
    {
        var ids = Enumerable.Range(0, members).Select(i => $"m{i}").ToList();
        var result = BalanceCalculator.SplitEvenly((decimal)amount, ids);

        Assert.Equal((decimal)amount, result.Values.Sum());
    }

    // ------------------------------------------------------------------ SplitByWeights

    [Fact]
    public void SplitByWeights_SplitsProportionally()
    {
        var result = BalanceCalculator.SplitByWeights(100m, [Pair("a", 1), Pair("b", 3)]);

        Assert.Equal(25m, result["a"]);
        Assert.Equal(75m, result["b"]);
    }

    [Fact]
    public void SplitByWeights_UsesLargestRemainderForRounding()
    {
        // 10 € auf 3 gleiche Gewichte → 3,34 / 3,33 / 3,33
        var result = BalanceCalculator.SplitByWeights(10m, [Pair("a", 1), Pair("b", 1), Pair("c", 1)]);

        Assert.Equal(10m, result.Values.Sum());
        Assert.Equal(3.34m, result["a"]);
    }

    [Fact]
    public void SplitByWeights_IgnoresZeroWeights()
    {
        var result = BalanceCalculator.SplitByWeights(50m, [Pair("a", 2), Pair("b", 0)]);

        Assert.Single(result);
        Assert.Equal(50m, result["a"]);
    }

    // ------------------------------------------------------------------ CalculateShares

    [Fact]
    public void CalculateShares_Percent_RequiresHundredPercent()
    {
        var (_, error) = BalanceCalculator.CalculateShares(100m, SplitMode.Percent, [Pair("a", 50), Pair("b", 40)]);
        Assert.NotNull(error);
    }

    [Fact]
    public void CalculateShares_Percent_StoresWeights()
    {
        var (shares, error) = BalanceCalculator.CalculateShares(80m, SplitMode.Percent, [Pair("a", 25), Pair("b", 75)]);

        Assert.Null(error);
        Assert.Equal(20m, shares.Single(s => s.MemberId == "a").Amount);
        Assert.Equal(75m, shares.Single(s => s.MemberId == "b").Weight);
    }

    [Fact]
    public void CalculateShares_Amount_RequiresExactSum()
    {
        var (_, error) = BalanceCalculator.CalculateShares(30m, SplitMode.Amount, [Pair("a", 10), Pair("b", 10)]);
        Assert.NotNull(error);

        var (shares, ok) = BalanceCalculator.CalculateShares(30m, SplitMode.Amount, [Pair("a", 10), Pair("b", 20)]);
        Assert.Null(ok);
        Assert.Equal(30m, shares.Sum(s => s.Amount));
    }

    [Fact]
    public void CalculateShares_Shares_SkipsMembersWithoutShares()
    {
        var (shares, error) = BalanceCalculator.CalculateShares(90m, SplitMode.Shares, [Pair("a", 2), Pair("b", 1), Pair("c", 0)]);

        Assert.Null(error);
        Assert.Equal(2, shares.Count);
        Assert.Equal(60m, shares.Single(s => s.MemberId == "a").Amount);
    }

    [Fact]
    public void CalculateShares_WithoutParticipants_ReturnsError()
    {
        var (_, error) = BalanceCalculator.CalculateShares(10m, SplitMode.Equal, []);
        Assert.NotNull(error);
    }

    // ------------------------------------------------------------------ Salden & Ausgleich

    [Fact]
    public void CalculateBalances_MatchesReadmeExample()
    {
        // Anna zahlt 60 €, Ben 30 €, Chris 0 € – alle teilen gleich
        var members = new[] { Member("anna"), Member("ben"), Member("chris") };
        var expenses = new[]
        {
            new ExpenseDto { Id = "e1", PaidBy = "anna", Amount = 60 },
            new ExpenseDto { Id = "e2", PaidBy = "ben", Amount = 30 }
        };
        var shares = expenses.SelectMany(e => members.Select(m => new ExpenseShareDto { ExpenseId = e.Id, MemberId = m.Id, Amount = e.Amount / 3 }));

        var balances = BalanceCalculator.CalculateBalances(members, expenses, shares, []);

        Assert.Equal(30m, balances.Single(b => b.MemberId == "anna").Balance);
        Assert.Equal(0m, balances.Single(b => b.MemberId == "ben").Balance);
        Assert.Equal(-30m, balances.Single(b => b.MemberId == "chris").Balance);

        var transfer = Assert.Single(BalanceCalculator.SuggestTransfers(balances));
        Assert.Equal("chris", transfer.FromMemberId);
        Assert.Equal("anna", transfer.ToMemberId);
        Assert.Equal(30m, transfer.Amount);
    }

    [Fact]
    public void CalculateBalances_SettlementsReduceDebt()
    {
        var members = new[] { Member("a"), Member("b") };
        var expenses = new[] { new ExpenseDto { Id = "e", PaidBy = "a", Amount = 40 } };
        var shares = new[]
        {
            new ExpenseShareDto { ExpenseId = "e", MemberId = "a", Amount = 20 },
            new ExpenseShareDto { ExpenseId = "e", MemberId = "b", Amount = 20 }
        };
        var settlements = new[] { new SettlementDto { FromMember = "b", ToMember = "a", Amount = 20 } };

        var balances = BalanceCalculator.CalculateBalances(members, expenses, shares, settlements);

        Assert.All(balances, b => Assert.Equal(0m, b.Balance));
        Assert.Empty(BalanceCalculator.SuggestTransfers(balances));
    }

    [Fact]
    public void SuggestTransfers_SettlesAllBalancesWithFewTransfers()
    {
        var balances = new[]
        {
            new Core.Models.Balances.MemberBalance("a", "A", 0, 0, 50),
            new Core.Models.Balances.MemberBalance("b", "B", 0, 0, 10),
            new Core.Models.Balances.MemberBalance("c", "C", 0, 0, -35),
            new Core.Models.Balances.MemberBalance("d", "D", 0, 0, -25)
        };

        var transfers = BalanceCalculator.SuggestTransfers(balances);

        Assert.True(transfers.Count <= 3);
        foreach (var balance in balances)
        {
            // Positiver Saldo = bekommt Geld: Summe erhaltener minus gesendeter Zahlungen
            var net = transfers.Where(t => t.ToMemberId == balance.MemberId).Sum(t => t.Amount)
                      - transfers.Where(t => t.FromMemberId == balance.MemberId).Sum(t => t.Amount);
            Assert.Equal(balance.Balance, net);
        }
    }

    // ------------------------------------------------------------------ Wiederholungen

    [Theory]
    [InlineData(RepeatInterval.Weekly, "2026-01-31", "2026-02-07")]
    [InlineData(RepeatInterval.Monthly, "2026-01-31", "2026-02-28")]
    [InlineData(RepeatInterval.Yearly, "2028-02-29", "2029-02-28")]
    public void RepeatInterval_Next_CalculatesFollowingDate(string interval, string start, string expected)
        => Assert.Equal(DateTime.Parse(expected), RepeatInterval.Next(DateTime.Parse(start), interval));
}
