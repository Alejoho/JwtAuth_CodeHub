using WebApiRefreshTokenDemo.Models;

namespace WebApiRefreshTokenDemo.Services;

public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, IList<string> roles);
    RefreshToken CreateRefreshToken(string ipAddress);
}
