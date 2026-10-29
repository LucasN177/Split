using Split.Core.Interfaces.Response;
using Split.Core.Models.Balances;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Services;

public interface IGroupService
{
    /// <summary>Zuletzt geladene Gruppen des Benutzers (z.B. für das Navigationsmenü).</summary>
    public IReadOnlyList<GroupDto> Groups { get; }

    /// <summary>Wird ausgelöst, wenn sich die Gruppenliste geändert hat.</summary>
    public event Action? GroupsChanged;

    public Task<IResponse<List<GroupDto>>> LoadGroups();

    public Task<IResponse<GroupDto>> CreateGroup(GroupDto group);

    public Task<IResponse> UpdateGroup(GroupDto group);

    public Task<IResponse> DeleteGroup(string groupId);

    public Task<IResponse<GroupDetails>> GetGroupDetails(string groupId);

    /// <summary>Fügt ein Mitglied hinzu – per Benutzername (registrierter Benutzer) oder als reiner Name.</summary>
    public Task<IResponse<GroupMemberDto>> AddMember(string groupId, string name, bool isUsername);

    public Task<IResponse> RemoveMember(GroupDetails details, string memberId);

    public Task<IResponse<DashboardOverview>> GetDashboard();
}
