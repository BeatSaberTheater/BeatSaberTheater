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
        Task.Run(RefreshAuthToken);
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

        ScheduleNextRefresh();
    }

    private void ScheduleNextRefresh()
    {
        _refreshTokenCts?.Cancel();
        _refreshTokenCts = new CancellationTokenSource();

        var timeUntilExpiry = _tokenExpiryUtc - DateTime.UtcNow;
        var refreshDelay = timeUntilExpiry - TimeSpan.FromMinutes(5); // refresh 5 minutes before expiry
        if (refreshDelay < TimeSpan.Zero)
            refreshDelay = TimeSpan.FromMinutes(1); // refresh soon if token is near-expired

        _ = Task.Run(async () =>
        {
            try
            {
                _loggingService.Debug($"Scheduling next auth token refresh in {refreshDelay.TotalMinutes:F1} minutes");
                await Task.Delay(refreshDelay, _refreshTokenCts.Token);
                await RefreshAuthToken();
            }
            catch (TaskCanceledException)
            {
                // expected when Dispose() or new refresh
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error scheduling next token refresh: {ex}");
            }
        }, _refreshTokenCts.Token);
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
        catch
        {
            // ignored — fallback will handle missing exp
            _loggingService.Debug("Failed parsing expiration from JWT");
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
