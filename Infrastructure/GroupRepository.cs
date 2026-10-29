using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Models;
using Split.Core.Models.Database;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace Split.Infrastructure;

public class GroupRepository(Supabase.Client client) : IGroupRepository
{
    private static readonly QueryOptions ReturnRepresentation = new() { Returning = QueryOptions.ReturnType.Representation };

    public async Task<IResponse<List<GroupDto>>> GetGroups()
    {
        try
        {
            var result = await client.From<GroupDto>()
                .Order(x => x.CreatedAt, Ordering.Descending)
                .Get();
            return Response<List<GroupDto>>.Success(result.Models);
        }
        catch (Exception ex)
        {
            return Response<List<GroupDto>>.Failure(ex);
        }
    }

    public async Task<IResponse<GroupDto>> GetGroup(string groupId)
    {
        try
        {
            var result = await client.From<GroupDto>().Where(x => x.Id == groupId).Get();
            var group = result.Models.FirstOrDefault();
            return group is not null ? Response<GroupDto>.Success(group) : Response<GroupDto>.Failure("Gruppe nicht gefunden");
        }
        catch (Exception ex)
        {
            return Response<GroupDto>.Failure(ex);
        }
    }

    public async Task<IResponse<GroupDto>> CreateGroup(GroupDto group)
    {
        try
        {
            group.CreatedBy = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
            var result = await client.From<GroupDto>().Insert(group, ReturnRepresentation);
            return result.Model is not null ? Response<GroupDto>.Success(result.Model) : Response<GroupDto>.Failure("Gruppe konnte nicht erstellt werden");
        }
        catch (Exception ex)
        {
            return Response<GroupDto>.Failure(ex);
        }
    }

    public async Task<IResponse> UpdateGroup(GroupDto group)
    {
        try
        {
            await client.From<GroupDto>()
                .Where(x => x.Id == group.Id)
                .Set(x => x.Name, group.Name)
                .Set(x => x.Description!, group.Description ?? string.Empty)
                .Set(x => x.Currency, group.Currency)
                .Update();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> DeleteGroup(string groupId)
    {
        try
        {
            await client.From<GroupDto>().Where(x => x.Id == groupId).Delete();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<List<GroupMemberDto>>> GetMembers(IReadOnlyCollection<string> groupIds)
    {
        if (groupIds.Count == 0)
            return Response<List<GroupMemberDto>>.Success([]);

        try
        {
            var result = await client.From<GroupMemberDto>()
                .Filter("group_id", Operator.In, groupIds.ToList())
                .Order(x => x.CreatedAt, Ordering.Ascending)
                .Get();
            return Response<List<GroupMemberDto>>.Success(result.Models);
        }
        catch (Exception ex)
        {
            return Response<List<GroupMemberDto>>.Failure(ex);
        }
    }

    public async Task<IResponse<GroupMemberDto>> AddMember(GroupMemberDto member)
    {
        try
        {
            var result = await client.From<GroupMemberDto>().Insert(member, ReturnRepresentation);
            return result.Model is not null ? Response<GroupMemberDto>.Success(result.Model) : Response<GroupMemberDto>.Failure("Mitglied konnte nicht hinzugefügt werden");
        }
        catch (Exception ex)
        {
            return Response<GroupMemberDto>.Failure(ex);
        }
    }

    public async Task<IResponse> RemoveMember(string memberId)
    {
        try
        {
            await client.From<GroupMemberDto>().Where(x => x.Id == memberId).Delete();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<UserDto>> FindUserByUsername(string username)
    {
        try
        {
            // RPC statt direkter Abfrage, damit die Policies der gemeinsam genutzten users-Tabelle unverändert bleiben
            var result = await client.Rpc<List<UserDto>>("split_find_user", new Dictionary<string, object>
            {
                ["p_username"] = username.Trim()
            });
            var user = result?.FirstOrDefault();
            return user is not null ? Response<UserDto>.Success(user) : Response<UserDto>.Failure($"Benutzer \"{username}\" nicht gefunden");
        }
        catch (Exception ex)
        {
            return Response<UserDto>.Failure(ex);
        }
    }

    public async Task<IResponse> RenameMember(string memberId, string displayName)
    {
        try
        {
            await client.From<GroupMemberDto>()
                .Where(x => x.Id == memberId)
                .Set(x => x.DisplayName, displayName)
                .Update();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> LeaveGroup(string groupId)
    {
        try
        {
            await client.Rpc("split_leave_group", new Dictionary<string, object> { ["p_group_id"] = groupId });
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<InviteInfo>> GetInvite(string inviteCode)
    {
        try
        {
            var result = await client.Rpc<InviteInfo?>("split_get_invite", new Dictionary<string, object> { ["p_code"] = inviteCode });
            return result is not null ? Response<InviteInfo>.Success(result) : Response<InviteInfo>.Failure("Einladung ungültig oder abgelaufen");
        }
        catch (Exception ex)
        {
            return Response<InviteInfo>.Failure(ex);
        }
    }

    public async Task<IResponse<string>> JoinGroup(string inviteCode, string? memberId)
    {
        try
        {
            var parameters = new Dictionary<string, object?> { ["p_code"] = inviteCode, ["p_member_id"] = memberId };
            var groupId = await client.Rpc<string>("split_join_group", parameters);
            return string.IsNullOrEmpty(groupId) ? Response<string>.Failure("Beitritt fehlgeschlagen") : Response<string>.Success(groupId);
        }
        catch (Exception ex)
        {
            return Response<string>.Failure(ex);
        }
    }

    public async Task<IResponse<string>> RegenerateInvite(string groupId)
    {
        try
        {
            var code = await client.Rpc<string>("split_regenerate_invite", new Dictionary<string, object> { ["p_group_id"] = groupId });
            return string.IsNullOrEmpty(code) ? Response<string>.Failure("Link konnte nicht erneuert werden") : Response<string>.Success(code);
        }
        catch (Exception ex)
        {
            return Response<string>.Failure(ex);
        }
    }

    public async Task<IResponse> DeleteMyData()
    {
        try
        {
            await client.Rpc("split_delete_my_data", null);
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }
}
