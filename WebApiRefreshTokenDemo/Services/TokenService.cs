using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApiRefreshTokenDemo.Models;
using WebApiRefreshTokenDemo.Options;

namespace WebApiRefreshTokenDemo.Services;

public class TokenService(IOptions<JwtSettingsOptions> jwtOptions) : ITokenService
{
    private readonly JwtSettingsOptions _jwtOptions = jwtOptions.Value;

    public string CreateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecurityKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            { JwtRegisteredClaimNames.Sub,user.Id },
            { JwtRegisteredClaimNames.UniqueName,user.UserName ?? "" },
            { JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString() },
            { ClaimTypes.NameIdentifier,user.Id }
        };

        foreach (var role in roles)
        {
            claims.Add(ClaimTypes.Role, role);
        }

        var token = new SecurityTokenDescriptor()
        {
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(token);
    }

    public RefreshToken CreateRefreshToken(string ipAddress)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
}
