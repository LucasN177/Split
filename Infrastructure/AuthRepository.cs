using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Models;
using Split.Core.Models.Database;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Task = System.Threading.Tasks.Task;

namespace Split.Infrastructure;

public class AuthRepository(Supabase.Client client): IAuthRepository
{
    public async Task<IResponse> Initialize()
    {
        try
        {
            // Lädt eine gespeicherte Session und erneuert sie bei Bedarf
            await client.InitializeAsync();
            return Response.Success();
        }
        catch (GotrueException ex) when (ex.Reason is FailureHint.Reason.InvalidRefreshToken or FailureHint.Reason.ExpiredRefreshToken)
        {
            // Abgelaufene Session verwerfen, damit der Benutzer sich neu anmelden kann
            await Logout();
            return Response.Failure("Sitzung abgelaufen", ex);
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<Session>> Login(string email, string password)
    {
        try
        {
            var result = await client.Auth.SignInWithPassword(email.Trim(), password);
            return result is not null ? Response<Session>.Success(result) : Response<Session>.Failure("Anmeldung fehlgeschlagen");
        }
        catch (GotrueException ex)
        {
            return Response<Session>.Failure(Translate(ex));
        }
        catch (Exception ex)
        {
            return Response<Session>.Failure(ex);
        }
    }

    public async Task<IResponse<Session?>> Register(string email, string password, UserDto metadata)
    {
        try
        {
            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    ["username"] = metadata.Username,
                    ["vorname"] = metadata.Vorname,
                    ["nachname"] = metadata.Nachname
                }
            };
            // Session ist null, wenn eine E-Mail-Bestätigung erforderlich ist
            var result = await client.Auth.SignUp(email.Trim(), password, options);
            return Response<Session?>.Success(result);
        }
        catch (GotrueException ex)
        {
            return Response<Session?>.Failure(Translate(ex));
        }
        catch (Exception ex)
        {
            return Response<Session?>.Failure(ex);
        }
    }

    public async Task<IResponse> Logout()
    {
        try
        {
            await client.Auth.SignOut();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> ResetPassword(string email)
    {
        try
        {
            await client.Auth.ResetPasswordForEmail(email.Trim());
            return Response.Success();
        }
        catch (GotrueException ex)
        {
            return Response.Failure(Translate(ex), ex);
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public Task<IResponse<Session>> GetCurrentSession()
    {
        try
        {
            return Task.FromResult(client.Auth.CurrentSession is not null ? Response<Session>.Success(client.Auth.CurrentSession) : Response<Session>.Failure("No login available"));
        }
        catch (Exception exception)
        {
            return Task.FromException<IResponse<Session>>(exception);
        }
    }

    public Task<IResponse<User>> GetCurrentUser()
    {
        try
        {
            return Task.FromResult(client.Auth.CurrentUser is not null ? Response<User>.Success(client.Auth.CurrentUser) : Response<User>.Failure("No login available"));
        }
        catch (Exception exception)
        {
            return Task.FromException<IResponse<User>>(exception);
        }
    }

    public async Task<IResponse> InsertUserMetadata(UserDto user)
    {
        try
        {
            user.UserId = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
            await client.From<UserDto>().Upsert(user);
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse> UpdateUserMetadata(UserDto user)
    {
        try
        {
            var userId = client.Auth.CurrentUser?.Id ?? throw new InvalidOperationException("Nicht angemeldet");
            await client.From<UserDto>()
                .Where(x => x.UserId == userId)
                .Set(x => x.Username, user.Username)
                .Set(x => x.Vorname, user.Vorname)
                .Set(x => x.Nachname, user.Nachname)
                .Update();
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }

    public async Task<IResponse<UserDto>> GetUserMetadata(string id)
    {
        try
        {
            var result = await client.From<UserDto>().Where(x => x.UserId == id).Get();
            var user = result.Models.FirstOrDefault();
            return user is not null ? Response<UserDto>.Success(user) : Response<UserDto>.Failure("No user found");
        }
        catch (Exception ex)
        {
            return Response<UserDto>.Failure(ex);
        }
    }

    private static string Translate(GotrueException ex) => ex.Reason switch
    {
        FailureHint.Reason.UserBadLogin or FailureHint.Reason.UserBadPassword or FailureHint.Reason.UserBadMultiple
            => "E-Mail oder Passwort ist falsch.",
        FailureHint.Reason.UserEmailNotConfirmed => "Bitte bestätige zuerst deine E-Mail-Adresse.",
        FailureHint.Reason.UserAlreadyRegistered => "Diese E-Mail-Adresse ist bereits registriert.",
        FailureHint.Reason.UserBadEmailAddress => "Die E-Mail-Adresse ist ungültig.",
        FailureHint.Reason.UserTooManyRequests => "Zu viele Versuche. Bitte warte einen Moment.",
        FailureHint.Reason.Offline => "Keine Verbindung zum Server.",
        _ => "Es ist ein Fehler aufgetreten. Bitte versuche es erneut."
    };
}
