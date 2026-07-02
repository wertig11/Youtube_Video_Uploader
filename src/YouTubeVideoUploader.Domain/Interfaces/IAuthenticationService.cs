using System.Threading;
using System.Threading.Tasks;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Service for managing OAuth2 authentication with Google/YouTube.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Gets a value indicating whether the user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Authenticates the user using client secrets, prompting for browser login if necessary.
    /// </summary>
    /// <param name="clientSecretPath">The path to the client_secret.json file.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if authentication succeeded; otherwise, false.</returns>
    Task<bool> AuthenticateAsync(string clientSecretPath, CancellationToken ct);

    /// <summary>
    /// Logs the user out by clearing stored credentials.
    /// </summary>
    Task SignOutAsync();
}
