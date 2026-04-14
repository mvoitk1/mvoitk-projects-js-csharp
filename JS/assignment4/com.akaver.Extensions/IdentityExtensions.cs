using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace com.akaver.Extensions;

public static class IdentityExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        return new Guid?(principal.GetUserId<Guid>());
    }

    public static T? GetUserId<T>(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));
        string str = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            ?.Value!;
        if (typeof(T) == typeof(string))
            return (T) Convert.ChangeType((object) str, typeof(T));
        if (typeof(T) == typeof(int))
        {
            int result;
            return int.TryParse(str, out result)
                ? (T) Convert.ChangeType((object) result, typeof(T))
                : (T) Convert.ChangeType((object) null!, typeof(T));
        }

        if (typeof(T) == typeof(long))
        {
            long result;
            return long.TryParse(str, out result)
                ? (T) Convert.ChangeType((object) result, typeof(T))
                : (T) Convert.ChangeType((object) null!, typeof(T));
        }

        if (!(typeof(T) == typeof(Guid)))
            throw new Exception("Invalid type provided");
        Guid result1;
        return Guid.TryParse(str, out result1)
            ? (T) Convert.ChangeType((object) result1, typeof(T))
            : (T) Convert.ChangeType((object) null!, typeof(T));
    }

    public static string GenerateJwt(
        IEnumerable<Claim> claims,
        string key,
        string issuer,
        string audience,
        DateTime expirationDateTime)
    {
        SigningCredentials signingCredentials1 =
            new SigningCredentials((SecurityKey) new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), "HS256");
        string issuer1 = issuer;
        string audience1 = audience;
        IEnumerable<Claim> claims1 = claims;
        DateTime? nullable = new DateTime?(expirationDateTime);
        SigningCredentials signingCredentials2 = signingCredentials1;
        DateTime? notBefore = new DateTime?();
        DateTime? expires = nullable;
        SigningCredentials signingCredentials3 = signingCredentials2;
        return new JwtSecurityTokenHandler().WriteToken((SecurityToken) new JwtSecurityToken(issuer1, audience1,
            claims1, notBefore, expires, signingCredentials3));
    }
}