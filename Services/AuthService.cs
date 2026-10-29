using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Interfaces.Services;
using Split.Core.Models;
using Split.Core.Models.Database;
using Supabase.Gotrue;
using Task = System.Threading.Tasks.Task;

namespace Split.Services;

public class AuthService(IAuthRepository authRepository) : IAuthService
{
    public Session? CurrentSession { get; set; }
    public User? CurrentUser { get; set; }
    public UserDto? CurrentProfile { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public event Action? AuthStateChanged;

    public async Task Initialize()
    {
        await authRepository.Initialize();
        await RefreshState();
    }

    public async Task<IResponse<User?>> GetCurrentUser()
    {
        var result =  await authRepository.GetCurrentUser();
        if (result.IsSuccess)
        {
            CurrentUser = result.Data;
            return Response<User?>.Success(result.Data);
        }
        return Response<User?>.Failure(result.ErrorMessage ?? "No current user");
    }

    public async Task<IResponse<Session?>> GetCurrentSession()
    {
        var result =  await authRepository.GetCurrentSession();
        if (result.IsSuccess)
        {
            CurrentSession = result.Data;
            return Response<Session?>.Success(result.Data);
        }
        return Response<Session?>.Failure(result.ErrorMessage ?? "No current session");
    }

    public async Task<IResponse<Session>> Login(string email, string password)
    {
        var result =  await authRepository.Login(email, password);
        if (result is not { IsSuccess: true, Data: not null })
            return Response<Session>.Failure(result.ErrorMessage ?? "Login failed");

        await RefreshState();
        return Response<Session>.Success(result.Data);
    }

    public async Task<IResponse<Session?>> Register(string email, string password, UserDto metadata)
    {
        var result =  await authRepository.Register(email, password, metadata);
        if (!result.IsSuccess)
            return Response<Session?>.Failure(result.ErrorMessage ?? "Register failed.");

        if (result.Data is not null)
        {
            // Profilfelder setzen, falls der users-Trigger die Metadaten nicht übernimmt
            await authRepository.UpdateUserMetadata(metadata);
            await RefreshState();
        }
        return Response<Session?>.Success(result.Data);
    }

    public async Task<IResponse> Logout()
    {
        var result = await authRepository.Logout();
        CurrentSession = null;
        CurrentUser = null;
        CurrentProfile = null;
        AuthStateChanged?.Invoke();
        return result;
    }

    public async Task<IResponse> ResetPassword(string email, string redirectTo)
        => await authRepository.ResetPassword(email, redirectTo);

    public async Task<IResponse> UpdatePassword(string newPassword)
    {
        if (newPassword.Length < 8)
            return Response.Failure("Das Passwort muss mindestens 8 Zeichen lang sein.", null);
        return await authRepository.UpdatePassword(newPassword);
    }

    public async Task<IResponse<string?>> HandleAuthRedirect(Uri uri)
    {
        // Supabase hängt Tokens bzw. Fehler als Fragment an: #access_token=…&type=recovery
        var fragment = System.Web.HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'));
        var error = fragment["error_description"];
        if (!string.IsNullOrEmpty(error))
            return Response<string?>.Failure(error.Replace('+', ' '));

        if (string.IsNullOrEmpty(fragment["access_token"]))
            return Response<string?>.Success(null);

        var result = await authRepository.SessionFromUrl(uri);
        if (!result.IsSuccess)
            return Response<string?>.Failure(result.ErrorMessage ?? "Link ungültig");

        await RefreshState();
        return Response<string?>.Success(fragment["type"] ?? "signup");
    }

    public async Task<IResponse> InsertUserMetadata(UserDto user)
    {
        return await authRepository.InsertUserMetadata(user);
    }

    public async Task<IResponse> UpdateUserMetadata(UserDto user)
    {
        var result = await authRepository.UpdateUserMetadata(user);
        if (result.IsSuccess)
        {
            CurrentProfile = user;
            AuthStateChanged?.Invoke();
        }
        return result;
    }

    public async Task<IResponse<UserDto>> GetUserMetadata(string id)
    {
        var result = await authRepository.GetUserMetadata(id);
        return result is { IsSuccess: true, Data: not null } ? Response<UserDto>.Success(result.Data) : Response<UserDto>.Failure(result.ErrorMessage ?? "No metadata found");
    }

    private async Task RefreshState()
    {
        await GetCurrentSession();
        var user = await GetCurrentUser();
        if (!user.IsSuccess)
        {
            CurrentSession = null;
            CurrentUser = null;
        }

        CurrentProfile = CurrentUser is not null ? (await GetUserMetadata(CurrentUser.Id!)).Data : null;
        AuthStateChanged?.Invoke();
    }
}
