using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;
using Supabase.Gotrue;

namespace Split.Core.Interfaces.Infrastructure;

public interface IAuthRepository
{
    public Task<IResponse> Initialize();

    public Task<IResponse<Session>> Login(string email, string password);

    public Task<IResponse<Session?>> Register(string email, string password, UserDto metadata);

    public Task<IResponse> Logout();

    public Task<IResponse> ResetPassword(string email, string redirectTo);

    public Task<IResponse> UpdatePassword(string newPassword);

    /// <summary>Übernimmt eine Session aus einem Bestätigungs- oder Passwort-Link (Token im URL-Fragment).</summary>
    public Task<IResponse<Session>> SessionFromUrl(Uri uri);

    public Task<IResponse<Session>> GetCurrentSession();

    public Task<IResponse<User>> GetCurrentUser();

    public Task<IResponse> InsertUserMetadata(UserDto user);

    public Task<IResponse> UpdateUserMetadata(UserDto user);

    public Task<IResponse<UserDto>> GetUserMetadata(string id);

}
