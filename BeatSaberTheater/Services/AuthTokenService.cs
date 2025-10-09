using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberTheater.Util;
using BS_Utils.Utilities;
using BSTheater.Shared.Models.Authentication.Responses;
using BSTheater.Shared.Services;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Services;

public class AuthTokenService : IInitializable, IDisposable
{
    public string CurrentAuthToken = string.Empty;

    private readonly LoggingService _loggingService;
    private IPlatformUserModel? _platformUserModel;
    private CancellationTokenSource? _refreshTokenCts;
    private DateTime _tokenExpiryUtc = DateTime.MinValue;

    public AuthTokenService(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void Initialize()
    {
        BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
    }

    private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO? scenesTransition)
    {
        _loggingService.Debug("Starting auth token refresh loop");
        _refreshTokenCts?.Cancel();
        _refreshTokenCts = new CancellationTokenSource();

        // Start persistent background loop
        _ = Task.Run(() => AuthTokenRefreshLoop(_refreshTokenCts.Token));
    }

    private async Task AuthTokenRefreshLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // If token is missing or near expiry, refresh it
                    if (string.IsNullOrEmpty(CurrentAuthToken) || DateTime.UtcNow >= _tokenExpiryUtc.AddMinutes(-5))
                    {
                        await RefreshAuthToken();
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error($"Error during token refresh: {ex}");
                }

                // Sleep for 60 seconds between checks
                await Task.Delay(TimeSpan.FromSeconds(60), token);
            }
        }
        catch (TaskCanceledException)
        {
            // Expected during disposal
        }
    }

    private async Task RefreshAuthToken()
    {
        _loggingService.Debug("Attempting rotating auth token");

        if (_platformUserModel is null)
        {
            var platformUserModel = Resources
                .FindObjectsOfTypeAll<PlatformLeaderboardsModel>()
                .Select(l => l._platformUserModel)
                .Last(x => x != null);
            _platformUserModel = platformUserModel;
        }

        var platformUserToken = await _platformUserModel.GetUserAuthToken();
        _loggingService.Debug("PLATFORM USER: " + platformUserToken.validPlatformEnvironment);
        _loggingService.Debug("PLATFORM USER TOKEN: " + platformUserToken.token);
        var token = platformUserToken.token;

        var authTokenResponse = await RetrieveUserToken(token);
        CurrentAuthToken = authTokenResponse.Token;

        // Parse JWT expiration (exp) claim if present
        _tokenExpiryUtc = ParseJwtExpiry(CurrentAuthToken) ?? DateTime.UtcNow.AddMinutes(55); // Fallback to 55 minutes if expiry could not be parsed from JWT

        _loggingService.Debug($"Current auth token has been rotated: {CurrentAuthToken}");
        _loggingService.Debug($"Token expires at {_tokenExpiryUtc:u}");
    }

    private DateTime? ParseJwtExpiry(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '='); // fix base64 padding
            var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var payloadJson = Encoding.UTF8.GetString(jsonBytes);

            var obj = JObject.Parse(payloadJson);
            var expValue = obj["exp"]?.Value<long>();
            if (expValue != null)
                return DateTimeOffset.FromUnixTimeSeconds(expValue.Value).UtcDateTime;
        }
        catch (Exception ex)
        {
            _loggingService.Warn($"Failed parsing expiration from JWT: {ex}");
            _loggingService.Debug($"Received JWT: {jwt}");
        }
        return null;
    }

    public static async Task<AuthenticationTokenResponse> RetrieveUserToken(string platformUserToken)
    {
        var service = new BeatSaberTheaterApiService("http://localhost:5252", new HttpClient());
        var token = await service.Authentication.ExchangeSteamToken(platformUserToken);
        return token;
    }

    public void Dispose()
    {
        _refreshTokenCts?.Cancel();
        _refreshTokenCts?.Dispose();
        BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoadedFresh;
    }
}
