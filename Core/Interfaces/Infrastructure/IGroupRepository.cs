using Split.Core.Interfaces.Response;
using Split.Core.Models;
using Split.Core.Models.Database;

namespace Split.Core.Interfaces.Infrastructure;

public interface IGroupRepository
{
    public Task<IResponse<List<GroupDto>>> GetGroups();

    public Task<IResponse<GroupDto>> GetGroup(string groupId);

    public Task<IResponse<GroupDto>> CreateGroup(GroupDto group);

    public Task<IResponse> UpdateGroup(GroupDto group);

    public Task<IResponse> DeleteGroup(string groupId);

    public Task<IResponse<List<GroupMemberDto>>> GetMembers(IReadOnlyCollection<string> groupIds);

    public Task<IResponse<GroupMemberDto>> AddMember(GroupMemberDto member);

    public Task<IResponse> RemoveMember(string memberId);

    public Task<IResponse<UserDto>> FindUserByUsername(string username);

    public Task<IResponse> RenameMember(string memberId, string displayName);

    /// <summary>Verlässt die Gruppe (RPC split_leave_group).</summary>
    public Task<IResponse> LeaveGroup(string groupId);

    public Task<IResponse<InviteInfo>> GetInvite(string inviteCode);

    /// <summary>Tritt einer Gruppe bei, optional als vorhandenes Namens-Mitglied. Gibt die Gruppen-ID zurück.</summary>
    public Task<IResponse<string>> JoinGroup(string inviteCode, string? memberId);

    public Task<IResponse<string>> RegenerateInvite(string groupId);

    /// <summary>Verlässt alle Gruppen und entfernt Push-Abos (RPC split_delete_my_data).</summary>
    public Task<IResponse> DeleteMyData();
}
