namespace SharePointToolbox.Microsoft365;

public sealed class ApplicationAuthorizationSession
{
    public AuthenticationService.CurrentUserProfile? User { get; private set; }
    public bool IsAuthorized => User is not null;

    public void Authorize(AuthenticationService.CurrentUserProfile user) => User = user;
}
