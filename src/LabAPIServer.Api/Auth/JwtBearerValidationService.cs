using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LabAPIServer.Api.Auth;

public sealed class JwtBearerValidationService
{
    private readonly JwtValidationOptions options;
    private readonly Lazy<RsaSecurityKey> securityKey;

    public JwtBearerValidationService(IOptions<JwtValidationOptions> options)
    {
        this.options = options.Value ?? throw new ArgumentNullException(nameof(options));

        var pem = JwtValidationOptions.LoadPublicKeyPem(this.options)
            ?? throw new InvalidOperationException("JWT public key is missing.");

        securityKey = new Lazy<RsaSecurityKey>(() =>
        {
            using var rsa = JwtValidationOptions.CreatePublicRsa(pem);
            var publicKey = RSA.Create();
            publicKey.ImportParameters(new RSAParameters
            {
                Modulus = rsa.ExportParameters(false).Modulus,
                Exponent = rsa.ExportParameters(false).Exponent
            });
            return new RsaSecurityKey(publicKey);
        });
    }

    public TokenValidationParameters BuildValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
            ValidateIssuerSigningKey = true,
            ValidateActor = false,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ValidAlgorithms = [options.SigningAlgorithm],
            IssuerSigningKey = securityKey.Value
        };
    }

    public ClaimsPrincipal Validate(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            throw new SecurityTokenException("JWT is missing.");
        }

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(jwt, BuildValidationParameters(), out var validatedToken);

        if (validatedToken is not JwtSecurityToken token)
        {
            throw new SecurityTokenException("Token format is invalid.");
        }

        ValidateRequiredClaims(principal);
        return principal;
    }

    private static void ValidateRequiredClaims(ClaimsPrincipal principal)
    {
        var requiredClaims = new[] { "sub", "jti", "iat", "nbf", "exp" };
        foreach (var claimType in requiredClaims)
        {
            if (principal.Claims.Count(c => string.Equals(c.Type, claimType, StringComparison.Ordinal)) != 1)
            {
                throw new SecurityTokenException("JWT required claims are invalid.");
            }
        }

        var roles = principal.Claims.Where(c => string.Equals(c.Type, "role", StringComparison.Ordinal)).Select(c => c.Value).ToArray();
        if (roles.Length != 1 || !new[] { "Reader", "Operator", "Administrator" }.Contains(roles[0], StringComparer.Ordinal))
        {
            throw new SecurityTokenException("JWT role claim is invalid.");
        }
    }
}
