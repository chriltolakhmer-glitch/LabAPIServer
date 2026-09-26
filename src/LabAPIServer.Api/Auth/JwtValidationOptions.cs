using System.Security.Cryptography;

namespace LabAPIServer.Api.Auth;

public sealed class JwtValidationOptions
{
    public const string SectionName = "JwtValidation";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningAlgorithm { get; set; } = "RS256";

    public int ClockSkewSeconds { get; set; } = 300;

    public string? PublicKeyPem { get; set; }

    public string? PublicKeyPath { get; set; }

    public static string? LoadPublicKeyPem(JwtValidationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicKeyPem))
        {
            return options.PublicKeyPem;
        }

        if (string.IsNullOrWhiteSpace(options.PublicKeyPath))
        {
            return null;
        }

        if (!File.Exists(options.PublicKeyPath))
        {
            throw new InvalidOperationException($"JWT public key path '{options.PublicKeyPath}' does not exist.");
        }

        return File.ReadAllText(options.PublicKeyPath);
    }

    public static RSA CreatePublicRsa(string pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}
