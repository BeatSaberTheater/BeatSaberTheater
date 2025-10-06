using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaberTheater.Util;
using BS_Utils.Utilities;
using BSTheater.Shared.Models.Authentication.Responses;
using BSTheater.Shared.Services;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater.Services;

public class AuthTokenService : IInitializable
{
    public string CurrentAuthToken = string.Empty;

    private readonly LoggingService _loggingService;

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

        // TODO: Only use this method on first refresh, use existing token for subsequent refreshes
        // if (string.IsNullOrEmpty(CurrentAuthToken))
        var platformUserModel = Resources
            .FindObjectsOfTypeAll<PlatformLeaderboardsModel>()
            .Select(l => l._platformUserModel)
            .Last(x => x != null);

        var platformUserToken = await platformUserModel.GetUserAuthToken();
        _loggingService.Info("PLATFORM USER: " + platformUserToken.validPlatformEnvironment);
        _loggingService.Info("PLATFORM USER TOKEN: " + platformUserToken.token);

        var authTokenResponse = await RetrieveUserToken(platformUserToken.token);
        CurrentAuthToken = authTokenResponse.Token;

        _loggingService.Debug($"Current auth token has been rotated: {CurrentAuthToken}");
    }

    public static async Task<AuthenticationTokenResponse> RetrieveUserToken(string platformUserToken)
    {
        var service = new BeatSaberTheaterApiService("http://localhost:5252", new HttpClient());
        var token = await service.Authentication.ExchangeSteamToken(platformUserToken);

        return token;
    }
}
