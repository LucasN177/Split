using Split.Core.Interfaces.Response;
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
}
