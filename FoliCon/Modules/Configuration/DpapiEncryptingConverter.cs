using System.Security.Cryptography;
using STJ = System.Text.Json.Serialization;

#nullable enable

namespace FoliCon.Modules.Configuration;

/// <summary>
/// JSON converter that encrypts string values at rest using Windows DPAPI (CurrentUser scope).
/// On read: decrypts Base64-encoded DPAPI ciphertext back to plaintext.
///   If the value cannot be decrypted (e.g. plaintext from a pre-encryption config),
///   returns the raw value so the app doesn't crash. It will be encrypted on next save.
/// On write: encrypts plaintext via DPAPI and writes as Base64.
/// </summary>
public class DpapiEncryptingConverter : STJ.JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
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
            // Not valid Base64 — treat as plaintext. Will be encrypted on next save.
            return value;
        }
        catch (CryptographicException)
        {
            // Valid Base64 but not DPAPI-encrypted — treat as plaintext. Will be encrypted on next save.
            return value;
        }
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
}
