using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace LabAPIServer.Api.Auth;

public sealed class JwtValidationOptionsValidator : IValidateOptions<JwtValidationOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtValidationOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("JwtValidation:Issuer is required.");
        }
        else if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) || !string.Equals(issuer.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("JwtValidation:Issuer must be an absolute HTTPS URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("JwtValidation:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningAlgorithm))
        {
            failures.Add("JwtValidation:SigningAlgorithm is required.");
        }

        if (options.ClockSkewSeconds < 0 || options.ClockSkewSeconds > 600)
        {
            failures.Add("JwtValidation:ClockSkewSeconds must be between 0 and 600.");
        }

        var keyMaterial = JwtValidationOptions.LoadPublicKeyPem(options);
        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            failures.Add("JwtValidation:PublicKeyPem or JwtValidation:PublicKeyPath is required.");
        }
        else
        {
            try
            {
                using var rsa = JwtValidationOptions.CreatePublicRsa(keyMaterial);
            }
            catch (CryptographicException)
            {
                failures.Add("JwtValidation public key is not a valid RSA PEM public key.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
