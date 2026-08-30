using System.Security.Cryptography;
using STJ = System.Text.Json.Serialization;

#nullable enable

namespace FoliCon.Modules.Configuration;

/// <summary>
/// Encrypts (on write) and decrypts (on read) secret string values using Windows DPAPI (CurrentUser scope).
/// When applied at property-level via <c>[JsonConverter(typeof(DpapiEncryptingConverter))]</c>,
/// System.Text.Json invokes this converter only for annotated secret properties.
/// </summary>
public sealed class DpapiEncryptingConverter : STJ.JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return TryDecrypt(value);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteStringValue(value);
            return;
        }

        var plainBytes = System.Text.Encoding.UTF8.GetBytes(value);
        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        writer.WriteStringValue(Convert.ToBase64String(encryptedBytes));
    }

    /// <summary>
    /// Attempts to decrypt a DPAPI-encrypted Base64 string.
    /// If decryption fails or the input is plaintext, returns the original value.
    /// </summary>
    public static string? TryDecrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(value);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException)
        {
            // Not valid Base64 — treat as plaintext.
            return value;
        }
        catch (CryptographicException)
        {
            // Valid Base64 but not DPAPI-encrypted — treat as plaintext.
            return value;
        }
    }
}
