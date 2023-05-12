using ChatOnWebApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace ChatOnWebApi.Services
{
    public static class TokenService
    {
        public static string GetName(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // Token'ı doğrulayın
            var key = Encoding.UTF8.GetBytes("my top secret key");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            SecurityToken validatedToken;
            var claimsPrincipal = tokenHandler.ValidateToken(token, tokenValidationParameters, out validatedToken);

            // Claims'leri okuyun ve Authentication işlemini gerçekleştirin
            var nameClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
            if (nameClaim != null)
                return nameClaim.Value;
            return string.Empty;
        }
        public static RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken()
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddDays(14),
                Created = DateTime.Now
            };
            return refreshToken;
        }
    }
}
