using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using YouTubeVideoUploader.Domain.Interfaces;

namespace YouTubeVideoUploader.Infrastructure.YouTube;

/// <summary>
/// Service managing Google OAuth2 authentication flow.
/// </summary>
public class GoogleAuthService : IAuthenticationService
{
    private static readonly string[] Scopes = { YouTubeService.Scope.YoutubeForceSsl };
    private const string DataStoreFolder = "YouTubeVideoUploader.Auth";
    private UserCredential? _credential;

    /// <summary>
    /// Gets the current authenticated UserCredential.
    /// </summary>
    public UserCredential? Credential => _credential;

    /// <inheritdoc />
    public bool IsAuthenticated => _credential != null;

    /// <inheritdoc />
    public async Task<bool> AuthenticateAsync(string clientSecretPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientSecretPath))
        {
            throw new ArgumentException("Client secret path cannot be null or empty.", nameof(clientSecretPath));
        }

        if (!File.Exists(clientSecretPath))
        {
            throw new FileNotFoundException("Google OAuth Client Secret file not found.", clientSecretPath);
        }

        try
        {
            // Use FileDataStore to store credentials in %APPDATA%/YouTubeVideoUploader.Auth
            var dataStore = new FileDataStore(DataStoreFolder, fullPath: false);

            using var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read);
            var clientSecrets = (await GoogleClientSecrets.FromStreamAsync(stream, ct)).Secrets;

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                ct,
                dataStore
            );

            return _credential != null;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Authentication failed using client secret file {Path}", clientSecretPath);
            _credential = null;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        var dataStore = new FileDataStore(DataStoreFolder, fullPath: false);
        await dataStore.ClearAsync();
        _credential = null;
    }
}
