using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace VenuePlatform.Web.Security;

/// <summary>
/// Handles generation and validation of secure download tokens for invoices.
/// Tokens are signed with HMAC-SHA256 using the JWT secret key.
/// </summary>
public sealed class InvoiceDownloadToken
{
    private readonly byte[] _key;

    public InvoiceDownloadToken(string jwtSecretKey)
    {
        if (string.IsNullOrEmpty(jwtSecretKey))
        {
            throw new ArgumentException("JWT secret key is required.", nameof(jwtSecretKey));
        }
        _key = Encoding.UTF8.GetBytes(jwtSecretKey);
    }

    /// <summary>
    /// Generates a signed token for invoice download.
    /// </summary>
    /// <param name="invoiceId">The invoice ID</param>
    /// <param name="expiresUtc">Token expiration time (UTC)</param>
    /// <returns>Base64Url-encoded signed token</returns>
    public string GenerateToken(Guid invoiceId, DateTime expiresUtc)
    {
        var payload = new TokenPayload(invoiceId, expiresUtc);
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadBase64Url = WebEncoders.Base64UrlEncode(payloadBytes);

        var signature = ComputeSignature(payloadBase64Url);
        var signatureBase64Url = WebEncoders.Base64UrlEncode(signature);

        // Token format: payloadBase64Url.signatureBase64Url
        return $"{payloadBase64Url}.{signatureBase64Url}";
    }

    /// <summary>
    /// Validates a token and extracts the payload if valid.
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <param name="expectedInvoiceId">The expected invoice ID</param>
    /// <param name="payload">The extracted payload if valid</param>
    /// <returns>True if token is valid, not expired, and matches invoice ID</returns>
    public bool TryValidateToken(string token, Guid expectedInvoiceId, out TokenPayload? payload)
    {
        payload = null;

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var payloadBase64Url = parts[0];
        var signatureBase64Url = parts[1];

        // Validate signature using constant-time comparison
        try
        {
            var computedSignature = ComputeSignature(payloadBase64Url);
            var providedSignature = WebEncoders.Base64UrlDecode(signatureBase64Url);

            if (!CryptographicOperations.FixedTimeEquals(computedSignature, providedSignature))
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        // Parse payload safely
        try
        {
            var payloadBytes = WebEncoders.Base64UrlDecode(payloadBase64Url);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            payload = JsonSerializer.Deserialize<TokenPayload>(payloadJson);

            if (payload == null)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        // Validate invoice ID matches
        if (payload.InvoiceId != expectedInvoiceId)
        {
            return false;
        }

        // Validate not expired
        if (payload.ExpiresUtc <= DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    private byte[] ComputeSignature(string payloadBase64Url)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64Url));
    }

    /// <summary>
    /// Token payload structure.
    /// </summary>
    public sealed record TokenPayload(Guid InvoiceId, DateTime ExpiresUtc);
}
