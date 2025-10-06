using System.Net.Http;
using System.Threading.Tasks;
using BSTheater.Shared.Models.Authentication.Responses;
using BSTheater.Shared.Services;

namespace BeatSaberTheater.Services;

public class AuthTokenService
{
    public static async Task<AuthenticationTokenResponse> RetrieveTestUserToken()
    {
        var service = new BeatSaberTheaterApiService("http://localhost:5252", new HttpClient());
        var token = await service.Authentication.ExchangeSteamToken("UNIT_TEST_USER");

        return token;
    }
}
