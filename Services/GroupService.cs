using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Interfaces.Services;
using Split.Core.Models;
using Split.Core.Models.Balances;
using Split.Core.Models.Database;
using Split.Infrastructure;

namespace Split.Services;

public class GroupService : IGroupService
{
    private const string GroupsCacheKey = "cache.groups";
    private const string DashboardCacheKey = "cache.dashboard";
    private static string DetailsCacheKey(string groupId) => $"cache.group.{groupId}";

    private readonly IGroupRepository groupRepository;
    private readonly IExpenseRepository expenseRepository;
    private readonly IAuthService authService;
    private readonly ConnectivityService connectivity;
    private readonly LocalStore store;
    private List<GroupDto> _groups = [];

    public GroupService(
        IGroupRepository groupRepository,
        IExpenseRepository expenseRepository,
        IAuthService authService,
        ConnectivityService connectivity,
        LocalStore store)
    {
        this.groupRepository = groupRepository;
        this.expenseRepository = expenseRepository;
        this.authService = authService;
        this.connectivity = connectivity;
        this.store = store;

        // Gruppen und Cache des vorherigen Benutzers nach dem Abmelden verwerfen
        authService.AuthStateChanged += () =>
        {
            if (authService.IsAuthenticated) return;
            store.ClearCache();
            if (_groups.Count == 0) return;
            _groups = [];
            GroupsChanged?.Invoke();
        };
    }

    public IReadOnlyList<GroupDto> Groups => _groups;

    public event Action? GroupsChanged;

    public async Task<IResponse<List<GroupDto>>> LoadGroups()
    {
        var result = await groupRepository.GetGroups();
        if (result.IsSuccess && result.Data is not null)
        {
            _groups = result.Data;
            store.Set(GroupsCacheKey, _groups);
        }
        else if (!connectivity.IsOnline && store.Get<List<GroupDto>>(GroupsCacheKey) is { } cached)
        {
            _groups = cached;
            result = Response<List<GroupDto>>.Success(cached);
        }

        GroupsChanged?.Invoke();
        return result;
    }

    public async Task<IResponse<GroupDto>> CreateGroup(GroupDto group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
            return Response<GroupDto>.Failure("Bitte einen Gruppennamen angeben.");

        group.Name = group.Name.Trim();
        var result = await groupRepository.CreateGroup(group);
        if (result.IsSuccess)
            await LoadGroups();
        return result;
    }

    public async Task<IResponse> UpdateGroup(GroupDto group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
            return Response.Failure("Bitte einen Gruppennamen angeben.", null);

        group.Name = group.Name.Trim();
        var result = await groupRepository.UpdateGroup(group);
        if (result.IsSuccess)
            await LoadGroups();
        return result;
    }

    public async Task<IResponse> DeleteGroup(string groupId)
    {
        var result = await groupRepository.DeleteGroup(groupId);
        if (result.IsSuccess)
        {
            store.Remove(DetailsCacheKey(groupId));
            await LoadGroups();
        }
        return result;
    }

    public async Task<IResponse<GroupDetails>> GetGroupDetails(string groupId)
    {
        if (!connectivity.IsOnline)
            return FromCache(groupId, "Keine Verbindung");

        var group = await groupRepository.GetGroup(groupId);
        if (!group.IsSuccess || group.Data is null)
        {
            // Netzwerkfehler trotz "online" → auf Cache ausweichen
            return group.Exception is HttpRequestException
                ? FromCache(groupId, group.ErrorMessage)
                : Response<GroupDetails>.Failure(group.ErrorMessage ?? "Gruppe nicht gefunden");
        }

        // Fällige wiederkehrende Ausgaben anlegen, bevor die Ausgaben geladen werden
        await expenseRepository.ProcessRecurring(groupId);

        var membersTask = groupRepository.GetMembers([groupId]);
        var expensesTask = expenseRepository.GetExpenses([groupId]);
        var settlementsTask = expenseRepository.GetSettlements([groupId]);
        var recurringTask = expenseRepository.GetRecurring(groupId);
        await Task.WhenAll(membersTask, expensesTask, settlementsTask, recurringTask);

        var error = new IResponse[] { membersTask.Result, expensesTask.Result, settlementsTask.Result }
            .FirstOrDefault(r => !r.IsSuccess);
        if (error is not null)
            return Response<GroupDetails>.Failure(error.ErrorMessage ?? "Daten konnten nicht geladen werden");

        var members = membersTask.Result.Data ?? [];
        var expenses = expensesTask.Result.Data ?? [];
        var settlements = settlementsTask.Result.Data ?? [];

        var shares = await expenseRepository.GetShares(expenses.Select(e => e.Id).ToList());
        if (!shares.IsSuccess)
            return Response<GroupDetails>.Failure(shares.ErrorMessage ?? "Daten konnten nicht geladen werden");

        var balances = BalanceCalculator.CalculateBalances(members, expenses, shares.Data!, settlements);

        var details = new GroupDetails
        {
            Group = group.Data,
            Members = members,
            Expenses = expenses,
            Shares = shares.Data!,
            Settlements = settlements,
            Recurring = recurringTask.Result.Data ?? [],
            Balances = balances,
            Suggestions = BalanceCalculator.SuggestTransfers(balances),
            CurrentUserId = authService.CurrentUser?.Id
        };

        store.Set(DetailsCacheKey(groupId), details);
        return Response<GroupDetails>.Success(details);
    }

    private IResponse<GroupDetails> FromCache(string groupId, string? error)
    {
        var cached = store.Get<GroupDetails>(DetailsCacheKey(groupId));
        if (cached is null)
            return Response<GroupDetails>.Failure($"Offline und kein gespeicherter Stand vorhanden ({error}).");

        cached.IsFromCache = true;
        return Response<GroupDetails>.Success(cached);
    }

    public async Task<IResponse<GroupMemberDto>> AddMember(string groupId, string name, bool isUsername)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Response<GroupMemberDto>.Failure("Bitte einen Namen angeben.");

        var member = new GroupMemberDto { GroupId = groupId, DisplayName = name.Trim() };

        if (isUsername)
        {
            var user = await groupRepository.FindUserByUsername(name);
            if (!user.IsSuccess || user.Data is null)
                return Response<GroupMemberDto>.Failure(user.ErrorMessage ?? "Benutzer nicht gefunden");

            member.UserId = user.Data.UserId;
            var fullName = $"{user.Data.Vorname} {user.Data.Nachname}".Trim();
            member.DisplayName = string.IsNullOrEmpty(fullName) ? user.Data.Username : fullName;
        }

        var result = await groupRepository.AddMember(member);
        if (!result.IsSuccess && result.ErrorMessage?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
            return Response<GroupMemberDto>.Failure("Dieser Benutzer ist bereits Mitglied der Gruppe.");
        return result;
    }

    public async Task<IResponse> RenameMember(string memberId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Response.Failure("Bitte einen Namen angeben.", null);
        return await groupRepository.RenameMember(memberId, displayName.Trim());
    }

    public async Task<IResponse> RemoveMember(GroupDetails details, string memberId)
    {
        if (!details.IsAdmin)
            return Response.Failure("Nur der Admin der Gruppe kann Mitglieder entfernen.", null);

        var isInvolved = details.Expenses.Any(e => e.PaidBy == memberId)
                         || details.Shares.Any(s => s.MemberId == memberId)
                         || details.Settlements.Any(s => s.FromMember == memberId || s.ToMember == memberId);
        if (isInvolved)
            return Response.Failure("Mitglieder mit Ausgaben oder Zahlungen können nicht entfernt werden.", null);

        return await groupRepository.RemoveMember(memberId);
    }

    public async Task<IResponse> LeaveGroup(string groupId)
    {
        var result = await groupRepository.LeaveGroup(groupId);
        if (result.IsSuccess)
        {
            store.Remove(DetailsCacheKey(groupId));
            await LoadGroups();
        }
        return result;
    }

    public async Task<IResponse<InviteInfo>> GetInvite(string inviteCode)
        => await groupRepository.GetInvite(inviteCode);

    public async Task<IResponse<string>> JoinGroup(string inviteCode, string? memberId)
    {
        var result = await groupRepository.JoinGroup(inviteCode, memberId);
        if (result.IsSuccess)
            await LoadGroups();
        return result;
    }

    public async Task<IResponse<string>> RegenerateInvite(string groupId)
        => await groupRepository.RegenerateInvite(groupId);

    public async Task<IResponse> DeleteMyData()
    {
        var result = await groupRepository.DeleteMyData();
        if (result.IsSuccess)
        {
            store.ClearCache();
            await LoadGroups();
        }
        return result;
    }

    public async Task<IResponse<DashboardOverview>> GetDashboard()
    {
        if (!connectivity.IsOnline)
            return DashboardFromCache();

        var groups = await LoadGroups();
        if (!groups.IsSuccess || groups.Data is null)
            return groups.Exception is HttpRequestException
                ? DashboardFromCache()
                : Response<DashboardOverview>.Failure(groups.ErrorMessage ?? "Gruppen konnten nicht geladen werden");

        var groupIds = groups.Data.Select(g => g.Id).ToList();
        var membersTask = groupRepository.GetMembers(groupIds);
        var expensesTask = expenseRepository.GetExpenses(groupIds);
        var settlementsTask = expenseRepository.GetSettlements(groupIds);
        await Task.WhenAll(membersTask, expensesTask, settlementsTask);

        var members = membersTask.Result.Data ?? [];
        var expenses = expensesTask.Result.Data ?? [];
        var settlements = settlementsTask.Result.Data ?? [];
        var shares = (await expenseRepository.GetShares(expenses.Select(e => e.Id).ToList())).Data ?? [];

        var userId = authService.CurrentUser?.Id;
        var expenseGroup = expenses.ToDictionary(e => e.Id, e => e.GroupId);

        var summaries = groups.Data.Select(group =>
        {
            var groupMembers = members.Where(m => m.GroupId == group.Id).ToList();
            var groupExpenses = expenses.Where(e => e.GroupId == group.Id).ToList();
            var balances = BalanceCalculator.CalculateBalances(
                groupMembers,
                groupExpenses,
                shares.Where(s => expenseGroup.GetValueOrDefault(s.ExpenseId) == group.Id),
                settlements.Where(s => s.GroupId == group.Id));

            var me = groupMembers.FirstOrDefault(m => m.UserId == userId);
            var myBalance = me is null ? 0 : balances.FirstOrDefault(b => b.MemberId == me.Id)?.Balance ?? 0;
            return new GroupSummary(group, groupMembers.Count, groupExpenses.Sum(e => e.Amount), myBalance);
        }).ToList();

        var recent = expenses
            .Take(8)
            .Select(e =>
            {
                var group = groups.Data.First(g => g.Id == e.GroupId);
                var payer = members.FirstOrDefault(m => m.Id == e.PaidBy)?.DisplayName ?? "Unbekannt";
                return new ExpenseListItem(e, group.Name, payer, group.Currency);
            })
            .ToList();

        var overview = new DashboardOverview { Groups = summaries, RecentExpenses = recent };
        store.Set(DashboardCacheKey, overview);
        return Response<DashboardOverview>.Success(overview);
    }

    private IResponse<DashboardOverview> DashboardFromCache()
    {
        var cached = store.Get<DashboardOverview>(DashboardCacheKey);
        if (cached is null)
            return Response<DashboardOverview>.Failure("Offline und kein gespeicherter Stand vorhanden.");

        cached.IsFromCache = true;
        if (store.Get<List<GroupDto>>(GroupsCacheKey) is { } groups)
        {
            _groups = groups;
            GroupsChanged?.Invoke();
        }
        return Response<DashboardOverview>.Success(cached);
    }
}
