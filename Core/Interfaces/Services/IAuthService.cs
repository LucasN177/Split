using Split.Core.Interfaces.Response;
using Split.Core.Models.Database;
using Supabase.Gotrue;

namespace Split.Core.Interfaces.Services;

public interface IAuthService
{
    public Session? CurrentSession { get; set; }
    public User? CurrentUser { get; set; }
    public UserDto? CurrentProfile { get; }
    public bool IsAuthenticated { get; }

    /// <summary>Wird ausgelöst, wenn sich der Anmeldestatus oder das Profil ändert.</summary>
    public event Action? AuthStateChanged;

    /// <summary>Stellt eine gespeicherte Session wieder her (beim App-Start).</summary>
    public Task Initialize();

    public Task<IResponse<User?>> GetCurrentUser();

    public Task<IResponse<Session?>> GetCurrentSession();

    public Task<IResponse<Session>> Login(string email, string password);

    public Task<IResponse<Session?>> Register(string email, string password, UserDto metadata);

    public Task<IResponse> Logout();

    public Task<IResponse> ResetPassword(string email);

    public Task<IResponse> InsertUserMetadata(UserDto user);

    public Task<IResponse> UpdateUserMetadata(UserDto user);

    public Task<IResponse<UserDto>> GetUserMetadata(string id);
}
