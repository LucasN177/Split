using Newtonsoft.Json;

namespace Split.Core.Models;

/// <summary>Antwort der RPC split_get_invite.</summary>
public class InviteInfo
{
    [JsonProperty("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [JsonProperty("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [JsonProperty("is_member")]
    public bool IsMember { get; set; }

    [JsonProperty("open_members")]
    public List<InviteMember> OpenMembers { get; set; } = [];
}

public class InviteMember
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
